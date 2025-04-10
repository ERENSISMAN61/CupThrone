using UnityEngine;
using TMPro;

public class SeedInput : MonoBehaviour
{
    [SerializeField] private TMP_InputField seedCodeField;
    [SerializeField] private TerrainSeedManager terrainSeedManager;

    public void EnterSeed()
    {
        if (int.TryParse(seedCodeField.text, out int seed) && seed > 0)
        {
            terrainSeedManager.SetSeed(seed);
            Debug.Log($"SeedInput: Özel seed ayarlandı: {seed}");
        }
        else
        {
            Debug.LogWarning("SeedInput: Geçersiz seed girişi. Lütfen pozitif bir tam sayı girin.");
        }
    }
}
