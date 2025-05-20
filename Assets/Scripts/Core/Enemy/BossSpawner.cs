using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class BossSpawner : NetworkBehaviour
{
    [SerializeField] private BossEnemy bossPrefab;
    [SerializeField] private Vector2 xSpawnRange = new Vector2(-100f, 100f);
    [SerializeField] private Vector2 zSpawnRange = new Vector2(-100f, 100f);
    [SerializeField] private LayerMask terrainLayerMask;
    [SerializeField] private LayerMask nonTerrainLayerMask;
    [SerializeField] private Transform bossContainer;
    [SerializeField] private bool showDebugGizmos = false;
    [SerializeField] private int maxRaycastPoints = 100;
    [SerializeField] private float minDistanceFromPlayer = 15f;
    [SerializeField] private float initialSpawnDelay = 1f;

    private List<Vector3> raycastHitPoints = new List<Vector3>();
    private Collider[] colliders = new Collider[5];
    private float bossRadius;
    private bool isSpawning = false;
    private BossEnemy spawnedBoss = null;
    private bool isNavMeshReady = false;

    private void OnEnable()
    {
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
            //Debug.Log("EnemySpawner (Client): NavMesh hazır bildirimi alındı, ancak client düşman oluşturmaz");
            isNavMeshReady = true;
            return;
        }

        //Debug.Log("EnemySpawner (Server): NavMesh hazır olduğu bildirildi, düşmanlar oluşturulmaya başlanıyor...");
        isNavMeshReady = true;

        // Eğer NetworkBehaviour olarak çoktan spawn olduysa, düşmanları başlat
        if (IsSpawned)
        {
            StartCoroutine(DelayedSpawnBoss());
        }
    }

    public override void OnNetworkSpawn()
    {
        // Host/Server değilse, düşman oluşturmayı atla
        if (!IsServer)
        {
            //Debug.Log("EnemySpawner: Client olduğu için düşmanlar oluşturulmayacak.");
            return;
        }

        // NavMesh hazırsa düşmanları başlat, değilse OnNavMeshReady event'ini bekle
        if (isNavMeshReady)
        {
            StartCoroutine(DelayedSpawnBoss());
        }
        else
        {
            //Debug.Log("EnemySpawner: NavMesh'in hazır olması bekleniyor...");

            // DynamicNavMeshBuilder yoksa veya çalışmıyorsa
            if (DynamicNavMeshBuilder.Instance == null)
            {
                //Debug.LogWarning("EnemySpawner: DynamicNavMeshBuilder bulunamadı! Bir süre bekleyip düşmanları oluşturmaya deneyecek.");
                StartCoroutine(FallbackSpawnWithDelay(10f)); // 10 saniye bekle ve düşmanları oluştur
            }
            else if (DynamicNavMeshBuilder.Instance.IsNavMeshReady)
            {
                // DynamicNavMeshBuilder instance'ı event'i tetiklemeden önce hazırsa
                //Debug.Log("EnemySpawner: NavMesh zaten hazır, düşmanlar oluşturulmaya başlanıyor...");
                isNavMeshReady = true;
                StartCoroutine(DelayedSpawnBoss());
            }
        }
    }

    // Event çalışmazsa diye fallback çözüm
    private IEnumerator FallbackSpawnWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (!isSpawning && !isNavMeshReady)
        {
            //Debug.LogWarning("EnemySpawner: NavMesh'in hazır olması beklenemedi, düşmanlar yine de oluşturuluyor...");
            isNavMeshReady = true;
            StartCoroutine(DelayedSpawnBoss());
        }
    }

    private IEnumerator DelayedSpawnBoss()
    {
        if (isSpawning || spawnedBoss != null) yield break;
        yield return new WaitForSeconds(initialSpawnDelay);
        if (bossPrefab.GetComponent<CapsuleCollider>() != null)
        {
            bossRadius = bossPrefab.GetComponent<CapsuleCollider>().radius;
        }
        else if (bossPrefab.GetComponent<SphereCollider>() != null)
        {
            bossRadius = bossPrefab.GetComponent<SphereCollider>().radius;
        }
        else
        {
            bossRadius = 1f;
        }
        isSpawning = true;
        spawnedBoss = SpawnBoss();
        isSpawning = false;
    }

    private BossEnemy SpawnBoss()
    {
        Vector3 spawnPoint = GetSpawnPoint();
        Quaternion rotation = Quaternion.identity;
        try
        {
            BossEnemy bossInstance = Instantiate(
                bossPrefab,
                spawnPoint,
                rotation);
            var networkObject = bossInstance.GetComponent<NetworkObject>();
            if (networkObject != null)
            {
                networkObject.Spawn();
            }
            else
            {
                Debug.LogError("Boss prefabında NetworkObject bileşeni eksik!");
            }
            if (bossContainer != null)
            {
                bossInstance.transform.SetParent(bossContainer);
            }
            bossInstance.OnEnemyDefeated += HandleBossDefeated;
            return bossInstance;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Boss oluşturulurken hata: {e.Message}");
            return null;
        }
    }

    private void HandleBossDefeated(BossEnemy boss)
    {
        StartCoroutine(RespawnBossAfterDelay(boss));
    }

    private IEnumerator RespawnBossAfterDelay(BossEnemy boss)
    {
        yield return new WaitForSeconds(Random.Range(10f, 30f));
        Vector3 newSpawnPoint = GetSpawnPoint();
        boss.Reset(newSpawnPoint);
    }

    private Vector3 GetSpawnPoint()
    {
        float x = 0f;
        float z = 0f;
        int maxAttempts = 100;
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
                spawnPoint.y = hit.point.y + 0.1f;
                int numColliders = Physics.OverlapSphereNonAlloc(
                    spawnPoint,
                    bossRadius * 2f,
                    colliders,
                    nonTerrainLayerMask);
                if (numColliders == 0)
                {
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

    public void ClearDebugPoints()
    {
        raycastHitPoints.Clear();
    }
}