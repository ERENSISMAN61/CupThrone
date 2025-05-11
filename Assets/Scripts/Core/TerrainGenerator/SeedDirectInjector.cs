using UnityEngine;

/// <summary>
/// Seed değerinin doğru bir şekilde MapGenerator'a iletilmesini sağlayan yardımcı sınıf.
/// </summary>
[DefaultExecutionOrder(-1)] // Diğer scriptlerden önce çalışması için
public class SeedDirectInjector : MonoBehaviour
{
    [SerializeField] private float checkInterval = 0.5f;
    private float lastCheckTime = 0f;

    private void Start()
    {
        ApplySeedToAllMapGenerators();
    }

    private void Update()
    {
        // Belirli aralıklarla seed değerlerini kontrol et ve güncelle
        if (Time.time >= lastCheckTime + checkInterval)
        {
            ApplySeedToAllMapGenerators();
            lastCheckTime = Time.time;
        }
    }

    private void ApplySeedToAllMapGenerators()
    {
        // Seed değerini belirle
        int seedValue = GetCurrentSeedValue();

        // MapGenerator örneklerini bul ve güncelle
        if (seedValue > 0)
        {
            MapGenerator[] mapGenerators = FindObjectsOfType<MapGenerator>(true);
            foreach (var mapGen in mapGenerators)
            {
                if (mapGen != null && mapGen.noiseData != null)
                {
                    mapGen.noiseData.seed = seedValue;

                    // MapGenerator'a özel fonksiyon varsa çağır
                    var initMethod = mapGen.GetType().GetMethod("InitializeMapSeed",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (initMethod != null)
                    {
                        initMethod.Invoke(mapGen, null);
                        //Debug.Log($"Called InitializeMapSeed on MapGenerator");
                    }
                }
            }

            //Debug.Log($"SeedDirectInjector: Tüm MapGenerator'lar güncellendi, seed: {seedValue}");
        }
    }

    private int GetCurrentSeedValue()
    {
        // Öncelikle TerrainSeedManager'dan kontrolü dene
        if (TerrainSeedManager.CurrentSeed > 0)
            return TerrainSeedManager.CurrentSeed;

        // Sonra NetworkTerrainManager'ı kontrol et
        if (NetworkTerrainManager.TerrainSeedValue > 0)
            return NetworkTerrainManager.TerrainSeedValue;

        // Hiçbirinden alamadık, -1 döndür (geçersiz seed)
        return -1;
    }
}
