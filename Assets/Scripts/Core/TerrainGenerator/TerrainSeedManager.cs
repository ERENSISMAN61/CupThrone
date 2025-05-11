using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class TerrainSeedManager : MonoBehaviour
{
    [SerializeField] private int customSeed = -1;
    [SerializeField] private bool useCustomSeed = false;
    [SerializeField] private bool showSeedInGame = false;
    [SerializeField] private GUIStyle seedLabelStyle;

    private static TerrainSeedManager instance;

    public static int CurrentSeed { get; private set; } = -1;

    private void Awake()
    {
        // Singleton pattern implementation
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        // Apply seed setting
        if (useCustomSeed && customSeed > 0)
        {
            NetworkTerrainManager.SetCustomSeed(customSeed);
            CurrentSeed = customSeed;
            //Debug.Log($"TerrainSeedManager: Özel seed ayarlandı: {customSeed}");
        }
        else
        {
            // Rastgele seed üret
            int randomSeed = UnityEngine.Random.Range(1, 100000);
            NetworkTerrainManager.SetCustomSeed(randomSeed);
            CurrentSeed = randomSeed;
            //Debug.Log($"TerrainSeedManager: Rastgele seed üretildi: {randomSeed}");
        }
    }

    // Display current seed in game if enabled
    private void OnGUI()
    {
        if (showSeedInGame)
        {
            // Önce NetworkTerrainManager'dan, yoksa kendi değerimizi kullan
            int displaySeed = NetworkTerrainManager.TerrainSeedValue > 0 ?
                NetworkTerrainManager.TerrainSeedValue : CurrentSeed;

            if (displaySeed > 0)
            {
                GUI.Label(new Rect(10, 10, 200, 30), $"Terrain Seed: {displaySeed}", seedLabelStyle);
            }
        }
    }

    // Method to set seed via UI
    public void SetSeed(int newSeed)
    {
        customSeed = newSeed;
        useCustomSeed = true;
        NetworkTerrainManager.SetCustomSeed(newSeed);
        CurrentSeed = newSeed;
        //Debug.Log($"TerrainSeedManager: Seed manuel olarak değiştirildi: {newSeed}");
    }

    // Enable random seed generation
    public void UseRandomSeed()
    {
        useCustomSeed = false;
        int randomSeed = UnityEngine.Random.Range(1, 100000);
        NetworkTerrainManager.SetCustomSeed(randomSeed);
        CurrentSeed = randomSeed;
        // Debug.Log($"TerrainSeedManager: Rastgele seed kullanımına geçildi: {randomSeed}");
    }

    // Bu metod diğer scriptlerden çağrılabilir
    public static void UpdateSeedFromLobby(int lobbySeed)
    {
        if (lobbySeed <= 0)
        {
            Debug.LogWarning($"UpdateSeedFromLobby: Geçersiz seed değeri ({lobbySeed}), işlem yapılmadı.");
            return;
        }

        CurrentSeed = lobbySeed;
        NetworkTerrainManager.SetCustomSeed(lobbySeed);
        //Debug.Log($"TerrainSeedManager: Lobby'den alınan seed değeri ayarlandı: {lobbySeed}");
    }
}

#if UNITY_EDITOR
// Custom editor for the TerrainSeedManager
[CustomEditor(typeof(TerrainSeedManager))]
public class TerrainSeedManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        TerrainSeedManager manager = (TerrainSeedManager)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Terrain Seed Ayarları", EditorStyles.boldLabel);

        if (GUILayout.Button("Yeni Rastgele Seed Üret"))
        {
            SerializedProperty customSeedProp = serializedObject.FindProperty("customSeed");
            SerializedProperty useCustomSeedProp = serializedObject.FindProperty("useCustomSeed");

            customSeedProp.intValue = UnityEngine.Random.Range(0, 100000);
            useCustomSeedProp.boolValue = true;

            serializedObject.ApplyModifiedProperties();
        }

        if (GUILayout.Button("Her Seferinde Rastgele Seed Kullan"))
        {
            SerializedProperty useCustomSeedProp = serializedObject.FindProperty("useCustomSeed");
            useCustomSeedProp.boolValue = false;
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
