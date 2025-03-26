using Unity.Netcode;
using UnityEngine;
using System;
using System.Collections;

public class NetworkTerrainManager : NetworkBehaviour
{
    private MapGenerator mapGenerator;

    // NetworkVariable to sync the terrain seed across clients
    public NetworkVariable<int> TerrainSeed = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private bool isNetworkSeedReady = false;

    // Host tarafından ayarlanabilecek custom seed değeri
    public static int CustomSeed = -1;

    // Rastgele seed üretmek için kullanacağımız sistem zamanı
    private System.DateTime epochStart = new System.DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static int TerrainSeedValue { get; private set; } = -1;

    // Constant for use in lobby data
    public const string TERRAIN_SEED_KEY = "TerrainSeed";

    private void Awake()
    {
        // We'll now find the MapGenerator in OnNetworkSpawn instead of Awake
        // This is because the OutdoorsScene may not be loaded yet when NetworkTerrainManager awakes
    }

    private void FindMapGenerator()
    {
        // Önce sahne yüklenene kadar bekleyelim
        StartCoroutine(WaitForMapGenerator());
    }

    private IEnumerator WaitForMapGenerator()
    {
        int retryCount = 0;
        int maxRetries = 10;

        while (retryCount < maxRetries)
        {
            // Try to find MapGenerator by tag
            GameObject mapGeneratorObj = GameObject.FindGameObjectWithTag("MapGenerator");

            if (mapGeneratorObj != null)
            {
                mapGenerator = mapGeneratorObj.GetComponent<MapGenerator>();
                if (mapGenerator != null)
                {
                    Debug.Log("MapGenerator found by tag");

                    // Seed'i uygula
                    if (IsServer && mapGenerator.noiseData != null)
                    {
                        mapGenerator.noiseData.seed = TerrainSeed.Value;
                        Debug.Log($"Terrain seed applied to MapGenerator: {TerrainSeed.Value}");
                        isNetworkSeedReady = true;
                    }

                    yield break; // Başarıyla bulduk, çık
                }
            }

            // Fallback: try to find by type
            mapGenerator = FindAnyObjectByType<MapGenerator>();
            if (mapGenerator != null)
            {
                Debug.Log("MapGenerator found by type");

                // Seed'i uygula
                if (IsServer && mapGenerator.noiseData != null)
                {
                    mapGenerator.noiseData.seed = TerrainSeed.Value;
                    Debug.Log($"Terrain seed applied to MapGenerator: {TerrainSeed.Value}");
                    isNetworkSeedReady = true;
                }

                yield break; // Başarıyla bulduk, çık
            }

            // Sahne tamamen yüklenmemiş olabilir, biraz daha bekleyelim
            yield return new WaitForSeconds(0.5f);
            retryCount++;
            Debug.Log($"Waiting for MapGenerator... attempt {retryCount}/{maxRetries}");
        }

        Debug.LogError("MapGenerator not found in scene after multiple attempts! Check if MapGenerator exists and is properly tagged.");
    }

    public bool IsNetworkReady()
    {
        return isNetworkSeedReady;
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log("NetworkTerrainManager OnNetworkSpawn triggered");

        if (IsServer)
        {
            // Seed değerini belirle
            int newSeed;

            if (CustomSeed > 0)
            {
                // Eğer özel bir seed değeri ayarlanmışsa onu kullan
                newSeed = CustomSeed;
                Debug.Log($"Host using custom terrain seed: {newSeed}");
            }
            else
            {
                // Eğer özel seed değeri yoksa, sistem zamanına dayalı rastgele bir değer üret
                int timeSeed = (int)(DateTime.UtcNow - epochStart).TotalSeconds;
                UnityEngine.Random.InitState(timeSeed);
                newSeed = UnityEngine.Random.Range(1, 100000);
                Debug.Log($"Host generated random terrain seed: {newSeed}");
            }

            // Değeri ayarla ve yayımla
            TerrainSeed.Value = newSeed;
            TerrainSeedValue = newSeed;

            // TerrainSeedManager'ı güncelle
            TerrainSeedManager.UpdateSeedFromLobby(newSeed);

            // Hemen MapGenerator'ı bulup seed değerini güncellemeyi başlat
            FindMapGenerator();
        }
        else // Client ise
        {
            // TerrainSeedValue değerini TerrainSeed.Value'den al
            TerrainSeedValue = TerrainSeed.Value;
            Debug.Log($"Client received terrain seed: {TerrainSeedValue}");

            // TerrainSeedManager'ı güncelle
            TerrainSeedManager.UpdateSeedFromLobby(TerrainSeedValue);

            // Hemen MapGenerator'ı bulmaya başla
            FindMapGenerator();
        }

        // Subscribe to seed changes
        TerrainSeed.OnValueChanged += OnSeedChanged;
    }

    // Method to generate a seed for a new lobby
    public static int GenerateSeedForNewLobby()
    {
        int newSeed;

        if (CustomSeed > 0)
        {
            // Use custom seed if provided
            newSeed = CustomSeed;
            Debug.Log($"Host using custom terrain seed for lobby: {newSeed}");
        }
        else
        {
            // Generate random seed based on system time
            System.DateTime epochStart = new System.DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            int timeSeed = (int)(DateTime.UtcNow - epochStart).TotalSeconds;
            UnityEngine.Random.InitState(timeSeed);
            newSeed = UnityEngine.Random.Range(0, 100000);
            Debug.Log($"Generated random terrain seed for lobby: {newSeed}");
        }

        return newSeed;
    }

    // Method to set seed from lobby data
    public void SetSeedFromLobby(int lobbySeed)
    {
        if (lobbySeed <= 0)
        {
            Debug.LogWarning($"SetSeedFromLobby: Geçersiz seed değeri ({lobbySeed}), işlem yapılmadı.");
            return;
        }

        Debug.Log($"Setting terrain seed from lobby: {lobbySeed}");

        // Önce static değeri güncelle
        TerrainSeedValue = lobbySeed;

        // Server ise NetworkVariable'ı güncelle
        if (IsServer)
        {
            TerrainSeed.Value = lobbySeed;
        }

        // Tüm MapGenerator örneklerini doğrudan güncelle
        MapGenerator[] mapGenerators = FindObjectsOfType<MapGenerator>(true);
        foreach (var mapGen in mapGenerators)
        {
            if (mapGen != null)
            {
                // Reflection ile private değişkeni güncellemeye çalış
                try
                {
                    var fieldInfo = typeof(MapGenerator).GetField("currentSeedValue",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (fieldInfo != null)
                    {
                        fieldInfo.SetValue(mapGen, lobbySeed);
                        Debug.Log($"MapGenerator currentSeedValue directly updated to {lobbySeed}");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to update MapGenerator seed via reflection: {e.Message}");
                }

                // NoiseData'yı güncelle
                if (mapGen.noiseData != null)
                {
                    mapGen.noiseData.seed = lobbySeed;
                    Debug.Log($"MapGenerator noiseData seed updated to: {lobbySeed}");
                }
            }
        }
    }

    // Update metodu artık gerekli değil çünkü Coroutine kullanıyoruz
    private void Update()
    {
        // Bu metodu tamamen boş bırakabilir veya kaldırabilirsiniz
    }

    public override void OnNetworkDespawn()
    {
        TerrainSeed.OnValueChanged -= OnSeedChanged;
    }

    private void OnSeedChanged(int previousValue, int newValue)
    {
        Debug.Log($"Terrain seed changed from {previousValue} to {newValue}");
        TerrainSeedValue = newValue;

        if (mapGenerator != null && mapGenerator.noiseData != null)
        {
            mapGenerator.noiseData.seed = newValue;

            // Regenerate the map with the new seed
            mapGenerator.DrawMapInEditor();

            // Mark as ready when seed is received
            isNetworkSeedReady = true;
        }
    }

    // GUI veya konsoldan çağrılabilecek statik bir metod
    // Bu, özel bir seed değeri ayarlamak için kullanılır
    public static void SetCustomSeed(int seed)
    {
        if (seed <= 0)
        {
            Debug.LogWarning($"SetCustomSeed: Geçersiz seed değeri ({seed}), varsayılan bir değer kullanılacak.");
            seed = 12345; // Geçersiz değer yerine varsayılan değer kullan
        }

        CustomSeed = seed;
        // Static değeri de güncelle, hemen kullanılabilsin
        TerrainSeedValue = seed;
        Debug.Log($"Custom terrain seed set: {seed}. This will be used next time a host is created.");
    }
}
