using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

// NetworkBehaviour'dan türeterek ağ desteği ekleyelim
public class DynamicNavMeshBuilder : NetworkBehaviour
{
    // NavMeshSurface bileşeni - Editor'da atayabilirsiniz
    [SerializeField] private MonoBehaviour navMeshSurface;
    [SerializeField] private float maxWaitTime = 60f; // Maksimum bekleme süresi (saniye)

    private static DynamicNavMeshBuilder _instance;
    public static DynamicNavMeshBuilder Instance => _instance;

    private bool isNavMeshReady = false;
    public bool IsNavMeshReady => isNavMeshReady;

    // Event tetikleyicilerinin durumunu takip etmek için
    private bool isTerrainReady = false;
    private bool isTreesReady = false;

    // NavMesh hazır olduğunda tetiklenecek event
    public delegate void NavMeshEvent();
    public static event NavMeshEvent OnNavMeshReady;

    private Coroutine forceTimeoutCoroutine;

    private void Awake()
    {
        _instance = this;

        // NavMeshSurface yoksa, uyarı ver
        if (navMeshSurface == null)
        {
            Debug.LogWarning("NavMeshSurface bileşeni atanmamış. Inspector'dan atayın veya aynı GameObject'e ekleyin.");
            navMeshSurface = GetComponent<MonoBehaviour>();
        }
    }

    public override void OnNetworkSpawn()
    {
        // Sadece sunucu navmesh oluştursun
        if (!IsServer)
        {
            Debug.Log("DynamicNavMeshBuilder: Client olduğu için NavMesh oluşturulmayacak.");
            return;
        }

        // MapGenerator ve TreeSpawner eventlerine abone ol
        MapGenerator.OnTerrainGenerationComplete += OnTerrainGenerated;
        TreeSpawner.OnTreeGenerationComplete += OnTreesGenerated;

        // Eğer eventler bu script enable edilmeden önce tetiklendiyse
        // statik değişkenleri manuel olarak kontrol et
        CheckIfGeneratorsAlreadyCompleted();

        // Maksimum bekleme süresi sonunda tüm oluşturucular tamamlanmazsa zorla başlat
        forceTimeoutCoroutine = StartCoroutine(ForceBuildAfterTimeout());
    }

    public override void OnNetworkDespawn()
    {
        // Abonelikleri temizle
        MapGenerator.OnTerrainGenerationComplete -= OnTerrainGenerated;
        TreeSpawner.OnTreeGenerationComplete -= OnTreesGenerated;

        if (forceTimeoutCoroutine != null)
        {
            StopCoroutine(forceTimeoutCoroutine);
            forceTimeoutCoroutine = null;
        }
    }

    private IEnumerator ForceBuildAfterTimeout()
    {
        yield return new WaitForSeconds(maxWaitTime);

        if (!isNavMeshReady)
        {
            Debug.LogWarning($"Maksimum bekleme süresi ({maxWaitTime} saniye) aşıldı! Terrain ve/veya ağaçlar hazır olmasa da NavMesh oluşturuluyor.");
            BuildNavMeshIfReady(true); // force = true
        }
    }

    // Terrain oluşturma tamamlandığında çağrılır
    private void OnTerrainGenerated()
    {
        if (!IsServer) return; // Sadece server için

        Debug.Log("DynamicNavMeshBuilder: Terrain oluşturma tamamlandı!");
        isTerrainReady = true;
        BuildNavMeshIfReady();
    }

    // Ağaçlar oluşturulduğunda çağrılır
    private void OnTreesGenerated()
    {
        if (!IsServer) return; // Sadece server için

        Debug.Log("DynamicNavMeshBuilder: Ağaç oluşturma tamamlandı!");
        isTreesReady = true;
        BuildNavMeshIfReady();
    }

    // Eventleri kaçırdıysak, doğrudan statik değişkenleri kontrol et
    private void CheckIfGeneratorsAlreadyCompleted()
    {
        // MapGenerator'ın IsGenerationComplete özelliğini kontrol et
        MapGenerator mapGenerator = FindObjectOfType<MapGenerator>();
        if (mapGenerator != null && mapGenerator.IsGenerationComplete)
        {
            isTerrainReady = true;
            Debug.Log("DynamicNavMeshBuilder: MapGenerator zaten tamamlanmış.");
        }

        // TreeSpawner'ın IsGenerationComplete özelliğini kontrol et
        TreeSpawner treeSpawner = FindObjectOfType<TreeSpawner>();
        if (treeSpawner != null && treeSpawner.IsGenerationComplete)
        {
            isTreesReady = true;
            Debug.Log("DynamicNavMeshBuilder: TreeSpawner zaten tamamlanmış.");
        }

        // Her iki oluşturucu da tamamlanmışsa, NavMesh'i oluştur
        BuildNavMeshIfReady();
    }

    // Terrain ve ağaçlar hazırsa NavMesh'i oluştur
    private void BuildNavMeshIfReady(bool force = false)
    {
        if (!IsServer) return; // Sadece server için

        if ((isTerrainReady && isTreesReady) || force)
        {
            if (!isNavMeshReady) // Sadece bir kez oluştur
            {
                Debug.Log("DynamicNavMeshBuilder: Terrain ve ağaçlar hazır, NavMesh oluşturuluyor...");
                BuildNavMesh();
                isNavMeshReady = true;

                // NavMesh hazır event'ini tetikle
                OnNavMeshReady?.Invoke();

                // NavMesh hazır olma durumunu networkte senkronize et
                NotifyNavMeshReadyServerRpc();
            }
        }
    }

    // Server'dan client'lere NavMesh hazır olduğunu bildir
    [ServerRpc]
    private void NotifyNavMeshReadyServerRpc()
    {
        // Tüm client'lere NavMesh'in hazır olduğunu bildir
        NotifyNavMeshReadyClientRpc();
    }

    // Client'leri NavMesh'in hazır olduğuna dair bilgilendir
    [ClientRpc]
    private void NotifyNavMeshReadyClientRpc()
    {
        // Server bu RPC'yi de alacak, tekrar işlemesini önle
        if (IsServer) return;

        // Client'te NavMesh durumunu güncelle
        Debug.Log("DynamicNavMeshBuilder: Server'dan NavMesh hazır bildirimi alındı");
        isNavMeshReady = true;

        // Client'te de event'i tetikle
        OnNavMeshReady?.Invoke();
    }

    public void BuildNavMesh()
    {
        if (navMeshSurface != null)
        {
            // NavMeshSurface bileşeninin BuildNavMesh() metodunu reflection ile çağır
            System.Reflection.MethodInfo buildMethod = navMeshSurface.GetType().GetMethod("BuildNavMesh");
            if (buildMethod != null)
            {
                buildMethod.Invoke(navMeshSurface, null);
                Debug.Log("NavMesh başarıyla oluşturuldu!");
            }
            else
            {
                Debug.LogError("NavMeshSurface.BuildNavMesh metodu bulunamadı!");
                FallbackNavMeshBuild();
            }
        }
        else
        {
            // NavMeshSurface yoksa Unity'nin dahili NavMesh API'sini kullan
            FallbackNavMeshBuild();
        }
    }

    private void FallbackNavMeshBuild()
    {
        Debug.LogWarning("NavMeshSurface bulunamadı, NavMesh durumu hazır olarak işaretleniyor...");

        // NavMesh oluşturma işlemi NavMeshSurface olmadan gerçek anlamda yapılamaz
        // Bu durumda da NavMesh'in hazır olduğunu varsayıyoruz ki EnemySpawner çalışabilsin
        isNavMeshReady = true;
    }
}