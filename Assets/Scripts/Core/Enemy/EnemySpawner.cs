using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpawner : NetworkBehaviour
{
    [SerializeField] private BasicEnemy enemyPrefab;
    [SerializeField] private int maxEnemies = 20; //Maksimum düşman sayısı
    [SerializeField] private Vector2 xSpawnRange = new Vector2(-100f, 100f); //X ekseninde oluşturma aralığı
    [SerializeField] private Vector2 zSpawnRange = new Vector2(-100f, 100f); //Z ekseninde oluşturma aralığı
    [SerializeField] private LayerMask terrainLayerMask; //Zemin katmanı maskesi
    [SerializeField] private LayerMask nonTerrainLayerMask; //Zemin olmayan katman maskesi
    [SerializeField] private Transform enemyContainer; //Düşmanların parent objesi
    [SerializeField] private bool showDebugGizmos = false; //Debug gösterimi açık/kapalı
    [SerializeField] private int maxRaycastPoints = 100; //Maksimum raycast noktaları
    [SerializeField] private float minDistanceFromPlayer = 15f; //Oyunculardan minimum uzaklık
    [SerializeField] private float initialSpawnDelay = 1f; //İlk spawn için gecikme süresi

    private List<Vector3> raycastHitPoints = new List<Vector3>(); //Gizmos için raycast çarpma noktaları
    private Collider[] colliders = new Collider[5]; //Düşman spawn noktasındaki collider'ları kontrol eder
    private float enemyRadius; //Düşmanın yarıçapı
    private bool isSpawning = false; //Spawn işlemi devam ediyor mu
    private List<BasicEnemy> spawnedEnemies = new List<BasicEnemy>(); //Oluşturulan düşmanların listesi
    private bool isNavMeshReady = false; //NavMesh hazır mı?

    private void OnEnable()
    {
        // DynamicNavMeshBuilder'ın event'ine abone ol
        DynamicNavMeshBuilder.OnNavMeshReady += OnNavMeshReady;
    }

    private void OnDisable()
    {
        // Event aboneliğini kaldır
        DynamicNavMeshBuilder.OnNavMeshReady -= OnNavMeshReady;
    }

    // NavMesh hazır olduğunda çağrılan event handler
    private void OnNavMeshReady()
    {
        // Client'lere NavMesh hazır durumu RPC ile bildirilecek
        // Bu event hem server hem client'te tetiklenebilir

        if (!IsServer)
        {
            // Client ise sadece hazır durumunu güncelle, düşman spawn etme
            Debug.Log("EnemySpawner (Client): NavMesh hazır bildirimi alındı, ancak client düşman oluşturmaz");
            isNavMeshReady = true;
            return;
        }

        Debug.Log("EnemySpawner (Server): NavMesh hazır olduğu bildirildi, düşmanlar oluşturulmaya başlanıyor...");
        isNavMeshReady = true;

        // Eğer NetworkBehaviour olarak çoktan spawn olduysa, düşmanları başlat
        if (IsSpawned)
        {
            StartCoroutine(DelayedSpawnEnemies());
        }
    }

    public override void OnNetworkSpawn()
    {
        // Host/Server değilse, düşman oluşturmayı atla
        if (!IsServer)
        {
            Debug.Log("EnemySpawner: Client olduğu için düşmanlar oluşturulmayacak.");
            return;
        }

        // NavMesh hazırsa düşmanları başlat, değilse OnNavMeshReady event'ini bekle
        if (isNavMeshReady)
        {
            StartCoroutine(DelayedSpawnEnemies());
        }
        else
        {
            Debug.Log("EnemySpawner: NavMesh'in hazır olması bekleniyor...");

            // DynamicNavMeshBuilder yoksa veya çalışmıyorsa
            if (DynamicNavMeshBuilder.Instance == null)
            {
                Debug.LogWarning("EnemySpawner: DynamicNavMeshBuilder bulunamadı! Bir süre bekleyip düşmanları oluşturmaya deneyecek.");
                StartCoroutine(FallbackSpawnWithDelay(10f)); // 10 saniye bekle ve düşmanları oluştur
            }
            else if (DynamicNavMeshBuilder.Instance.IsNavMeshReady)
            {
                // DynamicNavMeshBuilder instance'ı event'i tetiklemeden önce hazırsa
                Debug.Log("EnemySpawner: NavMesh zaten hazır, düşmanlar oluşturulmaya başlanıyor...");
                isNavMeshReady = true;
                StartCoroutine(DelayedSpawnEnemies());
            }
        }
    }

    // Event çalışmazsa diye fallback çözüm
    private IEnumerator FallbackSpawnWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (!isSpawning && !isNavMeshReady)
        {
            Debug.LogWarning("EnemySpawner: NavMesh'in hazır olması beklenemedi, düşmanlar yine de oluşturuluyor...");
            isNavMeshReady = true;
            StartCoroutine(DelayedSpawnEnemies());
        }
    }

    private IEnumerator DelayedSpawnEnemies()
    {
        if (isSpawning) yield break; // Zaten oluşturuluyorsa çık

        // İlk spawn için kısa bir gecikme
        yield return new WaitForSeconds(initialSpawnDelay);

        Debug.Log("EnemySpawner: Düşmanlar oluşturuluyor...");

        //Düşmanın yarıçapını belirleme
        if (enemyPrefab.GetComponent<CapsuleCollider>() != null)
        {
            enemyRadius = enemyPrefab.GetComponent<CapsuleCollider>().radius;
        }
        else if (enemyPrefab.GetComponent<SphereCollider>() != null)
        {
            enemyRadius = enemyPrefab.GetComponent<SphereCollider>().radius;
        }
        else
        {
            enemyRadius = 1f; //Collider bulunamazsa varsayılan yarıçap
        }

        isSpawning = true;
        for (int i = 0; i < maxEnemies; i++)
        {
            BasicEnemy enemy = SpawnEnemy();
            if (enemy != null)
            {
                spawnedEnemies.Add(enemy);
            }
            yield return new WaitForSeconds(0.1f); //Performans düşüşü olmaması için ufak gecikme
        }
        isSpawning = false;

        Debug.Log($"EnemySpawner: Toplam {spawnedEnemies.Count} düşman başarıyla oluşturuldu!");
    }

    private BasicEnemy SpawnEnemy()
    {
        Vector3 spawnPoint = GetSpawnPoint();
        Quaternion rotation = Quaternion.identity;

        //Düşmanı zemin normali ile hizala
        Ray ray = new Ray(spawnPoint + Vector3.up * 10f, Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, terrainLayerMask))
        {
            rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
        }

        try
        {
            //Düşmanı oluştur
            BasicEnemy enemyInstance = Instantiate(
                enemyPrefab,
                spawnPoint,
                rotation);

            //NetworkObject kurulumu ve ağda spawn etme
            var networkObject = enemyInstance.GetComponent<NetworkObject>();
            if (networkObject != null)
            {
                networkObject.Spawn();
                Debug.Log($"Düşman ağda oluşturuldu: {spawnPoint}");
            }
            else
            {
                Debug.LogError("Düşman prefabında NetworkObject bileşeni eksik!");
            }

            //Düzenli olması için parent atama
            if (enemyContainer != null)
            {
                enemyInstance.transform.SetParent(enemyContainer);
            }

            //Düşmanın yenilgisini dinle
            enemyInstance.OnEnemyDefeated += HandleEnemyDefeated;

            return enemyInstance;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Düşman oluşturulurken hata: {e.Message}");
            return null;
        }
    }

    private void HandleEnemyDefeated(BasicEnemy enemy)
    {
        StartCoroutine(RespawnEnemyAfterDelay(enemy));
    }

    private IEnumerator RespawnEnemyAfterDelay(BasicEnemy enemy)
    {
        //Yeniden spawn etmeden önce rastgele süre bekle
        yield return new WaitForSeconds(Random.Range(5f, 15f));

        //Yeni spawn noktası alıp düşmanı sıfırla
        Vector3 newSpawnPoint = GetSpawnPoint();
        enemy.Reset(newSpawnPoint);
    }

    private Vector3 GetSpawnPoint()
    {
        float x = 0f;
        float z = 0f;
        int maxAttempts = 100; //Sonsuz döngüyü önlemek için maksimum deneme sayısı
        int attempts = 0;

        while (attempts < maxAttempts)
        {
            attempts++;
            x = Random.Range(xSpawnRange.x, xSpawnRange.y);
            z = Random.Range(zSpawnRange.x, zSpawnRange.y);

            Vector3 spawnPoint = new Vector3(x, 30f, z);
            Ray ray = new Ray(spawnPoint, Vector3.down);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, terrainLayerMask))
            {
                if (showDebugGizmos)
                {
                    if (raycastHitPoints.Count < maxRaycastPoints)
                    {
                        raycastHitPoints.Add(hit.point);
                    }
                }

                spawnPoint.y = hit.point.y + 0.1f; //Zeminin biraz üzerinde

                //Bu konumda başka objeler var mı kontrol et
                int numColliders = Physics.OverlapSphereNonAlloc(
                    spawnPoint,
                    enemyRadius * 2f, //Üst üste gelmesini önlemek için daha büyük yarıçap
                    colliders,
                    nonTerrainLayerMask);

                if (numColliders == 0)
                {
                    //Nokta tüm oyunculardan yeterince uzak mı kontrol et
                    bool isFarEnough = true;
                    GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

                    foreach (GameObject player in players)
                    {
                        if (Vector3.Distance(player.transform.position, spawnPoint) < minDistanceFromPlayer)
                        {
                            isFarEnough = false;
                            break;
                        }
                    }

                    if (isFarEnough)
                    {
                        return spawnPoint;
                    }
                }
            }
        }

        //Uygun konum bulunamazsa varsayılan konum döndür
        return new Vector3(x, 10f, z);
    }

    private void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        Gizmos.color = Color.red;

        foreach (Vector3 point in raycastHitPoints)
        {
            Gizmos.DrawSphere(point, 0.5f);
        }
    }

    //Debug görselleştirme noktalarını temizleme metodu
    public void ClearDebugPoints()
    {
        raycastHitPoints.Clear();
    }
}