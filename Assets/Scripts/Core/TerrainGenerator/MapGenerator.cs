using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;
using Unity.Netcode;

public class MapGenerator : MonoBehaviour
{
    // Terrain oluşturma tamamlandığında tetiklenecek event
    public static event Action OnTerrainGenerationComplete;

    // Terrain oluşturuldu mu?
    private bool isGenerationComplete = false;
    public bool IsGenerationComplete => isGenerationComplete;

    public enum DrawMode { NoiseMap, ColoredHeightMap, Mesh, FalloffMap };
    public DrawMode drawMode;

    public TerrainData terrainData;
    public NoiseData noiseData;
    public TextureData textureData;

    public Material terrainMaterial;

    [Range(0, 6)]
    public int editorPreviewLOD;

    [Range(0.1f, 20f)]
    public float maxTerrainHeight = 5f;

    public bool autoUpdate;

    float[,] falloffMap;

    Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
    Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

    // Volatile keyword ekleyerek thread'ler arası görünürlüğü sağlıyoruz
    private volatile int currentSeedValue = 12345;
    private volatile bool seedInitialized = false;
    // Thread safety için kullanılacak kilit nesnesi
    private readonly object seedLock = new object();

    // Debug için thread seed değerini takip edecek değişken
    private int lastRequestedSeedForThread = 0;

    void Awake()
    {
        // NetworkTerrainManager veya TerrainSeedManager'dan seed değerini almayı dene
        UpdateCurrentSeedFromNetworkManager();
    }

    void OnValuesUpdated()
    {
        if (!Application.isPlaying)
        {
            DrawMapInEditor();
        }
    }

    void OnTextureValuesUpdated()
    {
        textureData.ApplyToMaterial(terrainMaterial);
    }

    public int mapChunkSize
    {
        get
        {
            if (terrainData.useFlatShading)
            {
                return 95;
            }
            else
            {
                return 239;
            }
        }
    }

    public void DrawMapInEditor()
    {
        MapData mapData = GenerateMapData(Vector2.zero);

        MapDisplay display = FindAnyObjectByType<MapDisplay>();
        if (drawMode == DrawMode.NoiseMap)
        {
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.heightMap));
        }
        else if (drawMode == DrawMode.ColoredHeightMap)
        {
            display.DrawTexture(TextureGenerator.TextureFromColoredHeightMap(mapData.heightMap, maxTerrainHeight));
        }
        else if (drawMode == DrawMode.Mesh)
        {
            display.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, terrainData.meshHeightMultiplier * maxTerrainHeight, terrainData.meshHeightCurve, editorPreviewLOD, terrainData.useFlatShading));
        }
        else if (drawMode == DrawMode.FalloffMap)
        {
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(FalloffGenerator.GenerateFalloffMap(mapChunkSize)));
        }
    }

    public void RequestMapData(Vector2 centre, Action<MapData> callback)
    {
        // Önce güncel seed değerini almayı zorla
        UpdateCurrentSeedFromNetworkManager();

        // Thread başlatmadan önce son seed değerini kopyala
        int seedForThread;
        lock (seedLock)
        {
            seedForThread = currentSeedValue;
            lastRequestedSeedForThread = seedForThread; // debug için kaydet
        }

        // Loglama ekle - ne zaman hangi seed değeriyle thread başlatıldı
        //Debug.Log($"Thread başlatılıyor, seed: {seedForThread}, mevcut seed değeri: {currentSeedValue}");

        ThreadStart threadStart = delegate
        {
            MapDataThread(centre, callback, seedForThread);
        };

        new Thread(threadStart).Start();
    }

    void MapDataThread(Vector2 centre, Action<MapData> callback, int threadSeed)
    {
        MapData mapData = GenerateMapData(centre, threadSeed);
        lock (mapDataThreadInfoQueue)
        {
            mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
        }
    }

    public void RequestMeshData(MapData mapData, int lod, Action<MeshData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MeshDataThread(mapData, lod, callback);
        };

        new Thread(threadStart).Start();
    }

    void MeshDataThread(MapData mapData, int lod, Action<MeshData> callback)
    {
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, terrainData.meshHeightMultiplier * maxTerrainHeight, terrainData.meshHeightCurve, lod, terrainData.useFlatShading);
        lock (meshDataThreadInfoQueue)
        {
            meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
        }
    }

    // Ana thread'de çalışan güncelleme fonksiyonu
    void Update()
    {
        // Yeni thread'ler için seed değerini güncel tut
        UpdateCurrentSeedFromNetworkManager();

        if (mapDataThreadInfoQueue.Count > 0)
        {
            for (int i = 0; i < mapDataThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }

        if (meshDataThreadInfoQueue.Count > 0)
        {
            for (int i = 0; i < meshDataThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
    }

    // Overload - thread'e özgü seed değerini kullanan versiyon
    public MapData GenerateMapData(Vector2 centre, int threadSeedValue)
    {
        // Thread'e özel seed değerini kullan
        int seedToUse = threadSeedValue;

        // Kapsamlı log
        //Debug.Log($"GenerateMapData(thread) - threadSeedValue: {threadSeedValue}, lastRequested: {lastRequestedSeedForThread}, current: {currentSeedValue}");

        // Hata kontrolü
        if (seedToUse <= 0)
        {
            seedToUse = currentSeedValue; // Önce mevcut değeri dene
            if (seedToUse <= 0) // Hala geçersizse varsayılan değeri kullan
            {
                seedToUse = 12345;
                Debug.LogError($"Thread için geçersiz seed değeri! Varsayılan değer kullanılıyor.");
            }
        }

        float[,] noiseMap = Noise.GenerateNoiseMap(mapChunkSize + 2, mapChunkSize + 2, seedToUse, noiseData.noiseScale, noiseData.octaves, noiseData.persistance, noiseData.lacunarity, centre + noiseData.offset, noiseData.normalizeMode);

        if (terrainData.useFalloff)
        {
            if (falloffMap == null)
            {
                falloffMap = FalloffGenerator.GenerateFalloffMap(mapChunkSize + 2);
            }

            for (int y = 0; y < mapChunkSize + 2; y++)
            {
                for (int x = 0; x < mapChunkSize + 2; x++)
                {

                    if (terrainData.useFalloff)
                    {
                        noiseMap[x, y] = Mathf.Clamp01(noiseMap[x, y] - falloffMap[x, y]);
                    }
                }
            }
        }

        return new MapData(noiseMap);
    }

    // Ana thread için kullanılan orjinal metod
    public MapData GenerateMapData(Vector2 centre)
    {
        // Thread başlatmadan önce seed değerini güncelleyelim
        UpdateCurrentSeedFromNetworkManager();

        int seedToUse;
        lock (seedLock)
        {
            seedToUse = currentSeedValue;
        }

        //Debug.Log($"GenerateMapData(main) - currentSeedValue: {currentSeedValue}, TerrainSeedManager: {TerrainSeedManager.CurrentSeed}, NetworkTerrainManager: {NetworkTerrainManager.TerrainSeedValue}");

        if (seedToUse <= 0)
        {
            seedToUse = 12345;
            Debug.LogError($"MapGenerator: Geçersiz seed değeri! Sabit değer kullanılıyor.");
        }

        float[,] noiseMap = Noise.GenerateNoiseMap(mapChunkSize + 2, mapChunkSize + 2, seedToUse, noiseData.noiseScale, noiseData.octaves, noiseData.persistance, noiseData.lacunarity, centre + noiseData.offset, noiseData.normalizeMode);

        if (terrainData.useFalloff)
        {
            if (falloffMap == null)
            {
                falloffMap = FalloffGenerator.GenerateFalloffMap(mapChunkSize + 2);
            }

            for (int y = 0; y < mapChunkSize + 2; y++)
            {
                for (int x = 0; x < mapChunkSize + 2; x++)
                {

                    if (terrainData.useFalloff)
                    {
                        noiseMap[x, y] = Mathf.Clamp01(noiseMap[x, y] - falloffMap[x, y]);
                    }
                }
            }
        }

        return new MapData(noiseMap);
    }

    // OnValidate called when a value is changed
    void OnValidate()
    {
        if (terrainData != null)
        {
            terrainData.OnValuesUpdated -= OnValuesUpdated;
            terrainData.OnValuesUpdated += OnValuesUpdated;
        }
        if (noiseData != null)
        {
            noiseData.OnValuesUpdated -= OnValuesUpdated;
            noiseData.OnValuesUpdated += OnValuesUpdated;
        }
        if (textureData != null)
        {
            textureData.OnValuesUpdated -= OnTextureValuesUpdated;
            textureData.OnValuesUpdated += OnTextureValuesUpdated;
        }
    }

    void Start()
    {
        // Only initialize the falloffMap if needed
        if (terrainData.useFalloff && falloffMap == null)
        {
            falloffMap = FalloffGenerator.GenerateFalloffMap(mapChunkSize + 2);
        }

        // Başlangıçta kullanılacak seed değerini al
        InitializeMapSeed();

        // NetworkManager üzerindeki olaylara abone ol
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }

        // Seed değerini daha sık güncelle
        InvokeRepeating("UpdateCurrentSeedFromNetworkManager", 1f, 1f);
    }

    private void OnClientConnected(ulong clientId)
    {
        // Client bağlandığında seed değerini güncelle
        UpdateCurrentSeedFromNetworkManager();
        //Debug.Log($"Client bağlandı (ID: {clientId}), seed güncellendi: {currentSeedValue}");
    }

    void OnDestroy()
    {
        // Abonelikleri temizle
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }

    // Ana thread'den NetworkTerrainManager'dan seed değerini al
    private void UpdateCurrentSeedFromNetworkManager()
    {
        // Önce networked seed değerlerini direct referencing ile kontrol et - TerrainSeedManager veya NetworkTerrainManager'dan
        int directNetworkSeed = -1;

        // TerrainSeedManager seed değeri kontrolü
        if (TerrainSeedManager.CurrentSeed > 0)
        {
            directNetworkSeed = TerrainSeedManager.CurrentSeed;
            //            Debug.Log($"Direct TerrainSeedManager seed: {directNetworkSeed}");
        }
        // NetworkTerrainManager seed değeri kontrolü
        else if (NetworkTerrainManager.TerrainSeedValue > 0)
        {
            directNetworkSeed = NetworkTerrainManager.TerrainSeedValue;
            //Debug.Log($"Direct NetworkTerrainManager seed: {directNetworkSeed}");
        }

        // NoiseData kontrolü (en az öncelikli)
        int noiseDataSeed = -1;
        if (noiseData != null && noiseData.seed > 0)
        {
            noiseDataSeed = noiseData.seed;
            //            Debug.Log($"NoiseData seed: {noiseDataSeed}");
        }

        // Seed değerini ayarla - öncelik direkt networked değerlerde
        lock (seedLock)
        {
            // Önce networked değeri dene
            if (directNetworkSeed > 0)
            {
                currentSeedValue = directNetworkSeed;
                seedInitialized = true;
                //                Debug.Log($"UpdateCurrentSeedFromNetworkManager - Networked seed kullanıldı: {currentSeedValue}");
            }
            // Sonra NoiseData değerini dene
            else if (noiseDataSeed > 0 && !seedInitialized)
            {
                currentSeedValue = noiseDataSeed;
                seedInitialized = true;
                //Debug.Log($"UpdateCurrentSeedFromNetworkManager - NoiseData seed kullanıldı: {currentSeedValue}");
            }
            // En son varsayılan değer (12345) kullan, ancak uyarı ver
            else if (!seedInitialized)
            {
                //Debug.LogWarning($"Hiçbir seed kaynağı bulunamadı, varsayılan değer kullanılıyor: {currentSeedValue}");
                seedInitialized = true;
            }

            // NoiseData'yı da güncelle
            if (noiseData != null && currentSeedValue > 0)
            {
                noiseData.seed = currentSeedValue;
                //                Debug.Log($"NoiseData seed updated to: {currentSeedValue}");
            }
        }
    }

    // İlk kez seed ayarlama işlemi
    private void InitializeMapSeed()
    {
        // Güncel seed değerini almayı zorla
        UpdateCurrentSeedFromNetworkManager();

        // Ekstra doğrudan veri erişimi için
        var tsm = FindFirstObjectByType<TerrainSeedManager>();
        var ntm = FindFirstObjectByType<NetworkTerrainManager>();

        //Debug.Log($"InitializeMapSeed - currentSeedValue: {currentSeedValue}");
        //Debug.Log($"InitializeMapSeed - TerrainSeedManager.CurrentSeed: {TerrainSeedManager.CurrentSeed}");
        //Debug.Log($"InitializeMapSeed - NetworkTerrainManager.TerrainSeedValue: {NetworkTerrainManager.TerrainSeedValue}");

        if (tsm != null)
            //Debug.Log($"TerrainSeedManager bulundu");

            if (ntm != null)
                //Debug.Log($"NetworkTerrainManager bulundu");

                // NoiseData'ya seed değerini set et
                if (noiseData != null)
                {
                    noiseData.seed = currentSeedValue;
                    //Debug.Log($"NoiseData seed InitializeMapSeed'de güncellendi: {currentSeedValue}");
                }
    }

    // Lobby'den direkt olarak seed değerini almaya çalışan yeni metod
    private int TryGetLobbySeed()
    {
        try
        {
            // TerrainSeedManager'dan seed değerini almayı dene - bu değer genellikle lobby'den gelmiş olacak
            if (TerrainSeedManager.CurrentSeed > 0)
            {
                return TerrainSeedManager.CurrentSeed;
            }

            // NetworkTerrainManager'dan seed değerini almayı dene
            if (NetworkTerrainManager.TerrainSeedValue > 0)
            {
                return NetworkTerrainManager.TerrainSeedValue;
            }
        }
        catch (System.Exception e)
        {
            //Debug.LogWarning($"Lobby seed değeri alınırken hata: {e.Message}");
        }

        return -1;
    }

    // Terrain oluşturulduğunda bu metod çağrılır
    private void TerrainGenerationCompleted()
    {
        isGenerationComplete = true;
        //Debug.Log("MapGenerator: Terrain generation completed");

        // Event'i tetikle
        OnTerrainGenerationComplete?.Invoke();
    }

    // EndlessTerrain'den tüm terrain chunk'ların oluşturulduğunu bildirecek metod
    public void NotifyAllChunksCreated()
    {
        // Eğer daha önce tamamlanmadıysa, terrain oluşturma işleminin tamamlandığını işaretle
        if (!isGenerationComplete)
        {
            TerrainGenerationCompleted();
        }
    }

    struct MapThreadInfo<T>
    {
        public readonly Action<T> callback;
        public readonly T parameter;

        public MapThreadInfo(Action<T> callback, T parameter)
        {
            this.callback = callback;
            this.parameter = parameter;
        }
    }
}

public struct MapData
{
    public readonly float[,] heightMap;

    public MapData(float[,] heightMap)
    {
        this.heightMap = heightMap;
    }
}