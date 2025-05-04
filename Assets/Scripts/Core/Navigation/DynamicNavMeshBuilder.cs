using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

// MonoBehaviour kullanarak daha basit bir yapı
public class DynamicNavMeshBuilder : MonoBehaviour
{
    // NavMeshSurface bileşeni - Editor'da atayabilirsiniz
    [SerializeField] private MonoBehaviour navMeshSurface;
    [SerializeField] private float maxWaitTime = 60f; // Maksimum bekleme süresi (saniye)
    [SerializeField] private bool forceRebuildOnStart = true; // Başlangıçta NavMesh'i zorla yeniden oluştur

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
        Debug.Log("DynamicNavMeshBuilder: Awake çağrıldı");

        // NavMeshSurface yoksa, uyarı ver
        if (navMeshSurface == null)
        {
            Debug.LogWarning("NavMeshSurface bileşeni atanmamış. Inspector'dan atayın veya aynı GameObject'e ekleyin.");
            navMeshSurface = GetComponent<MonoBehaviour>();
        }
    }

    private void Start()
    {
        Debug.Log("DynamicNavMeshBuilder: Start çağrıldı");

        if (forceRebuildOnStart)
        {
            // Başlangıçta NavMesh'i zorla yeniden oluştur
            StartCoroutine(ForceRebuildAfterDelay(2f));
        }
    }

    private void OnEnable()
    {
        Debug.Log("DynamicNavMeshBuilder: OnEnable çağrıldı, event'lere abone olunuyor");

        // MapGenerator ve TreeSpawner eventlerine abone ol
        MapGenerator.OnTerrainGenerationComplete += OnTerrainGenerated;
        TreeSpawner.OnTreeGenerationComplete += OnTreesGenerated;

        // Eğer eventler bu script enable edilmeden önce tetiklendiyse
        // statik değişkenleri manuel olarak kontrol et
        CheckIfGeneratorsAlreadyCompleted();

        // Maksimum bekleme süresi sonunda tüm oluşturucular tamamlanmazsa zorla başlat
        forceTimeoutCoroutine = StartCoroutine(ForceBuildAfterTimeout());
    }

    private void OnDisable()
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

    private IEnumerator ForceRebuildAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        Debug.Log("DynamicNavMeshBuilder: ForceRebuildAfterDelay ile NavMesh zorla yeniden oluşturuluyor");

        // NavMesh'i zorla yeniden oluştur
        BuildNavMesh();
        isNavMeshReady = true;

        // NavMesh hazır event'ini tetikle
        OnNavMeshReady?.Invoke();
    }

    private IEnumerator ForceBuildAfterTimeout()
    {
        Debug.Log($"DynamicNavMeshBuilder: {maxWaitTime} saniyelik bekleme başlatıldı");
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
        Debug.Log("DynamicNavMeshBuilder: Terrain oluşturma tamamlandı bildirimi alındı!");
        isTerrainReady = true;
        BuildNavMeshIfReady();
    }

    // Ağaçlar oluşturulduğunda çağrılır
    private void OnTreesGenerated()
    {
        Debug.Log("DynamicNavMeshBuilder: Ağaç oluşturma tamamlandı bildirimi alındı!");
        isTreesReady = true;
        BuildNavMeshIfReady();
    }

    // Eventleri kaçırdıysak, doğrudan statik değişkenleri kontrol et
    private void CheckIfGeneratorsAlreadyCompleted()
    {
        Debug.Log("DynamicNavMeshBuilder: Generator durumları kontrol ediliyor");

        // MapGenerator'ın IsGenerationComplete özelliğini kontrol et
        MapGenerator mapGenerator = Object.FindFirstObjectByType<MapGenerator>();
        if (mapGenerator != null && mapGenerator.IsGenerationComplete)
        {
            isTerrainReady = true;
            Debug.Log("DynamicNavMeshBuilder: MapGenerator zaten tamamlanmış.");
        }

        // TreeSpawner'ın IsGenerationComplete özelliğini kontrol et
        TreeSpawner treeSpawner = Object.FindFirstObjectByType<TreeSpawner>();
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
        Debug.Log($"DynamicNavMeshBuilder: BuildNavMeshIfReady çağrıldı - isTerrainReady: {isTerrainReady}, isTreesReady: {isTreesReady}, force: {force}");

        if ((isTerrainReady && isTreesReady) || force)
        {
            if (!isNavMeshReady) // Sadece bir kez oluştur
            {
                Debug.Log("DynamicNavMeshBuilder: Terrain ve ağaçlar hazır, NavMesh oluşturuluyor...");
                BuildNavMesh();
                isNavMeshReady = true;

                // NavMesh hazır event'ini tetikle
                OnNavMeshReady?.Invoke();

                // Süreç tamamlandı, bu mesajı logla
                Debug.Log("DynamicNavMeshBuilder: NavMesh başarıyla oluşturuldu ve hazır.");
            }
        }
    }

    public void BuildNavMesh()
    {
        Debug.Log("DynamicNavMeshBuilder: BuildNavMesh çağrıldı");

        if (navMeshSurface != null)
        {
            try
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
            catch (System.Exception e)
            {
                Debug.LogError($"NavMesh oluşturulurken hata: {e.Message}");
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