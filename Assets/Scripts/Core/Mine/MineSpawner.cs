using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using System;

public class MineSpawner : NetworkBehaviour
{
    // Ağaç oluşturma tamamlandığında tetiklenecek event
    public static event Action OnMineGenerationComplete;

    // Ağaçlar oluşturuldu mu?
    private bool isGenerationComplete = false;
    public bool IsGenerationComplete => isGenerationComplete;

    [SerializeField] private GameObject StoneMinePrefab; // Stone mine prefab (40% chance)
    [SerializeField] private GameObject IronMinePrefab; // Iron mine prefab (30% chance)
    [SerializeField] private GameObject GoldMinePrefab; // Gold mine prefab (20% chance)
    [SerializeField] private GameObject DiamondMinePrefab; // Diamond mine prefab (10% chance)
    [SerializeField] private int maxMines = 100; // Maximum number of Mines to spawn
    [SerializeField] private LayerMask terrainLayerMask; // Layer mask for terrain
    [SerializeField] private LayerMask nonTerrainLayerMask; // Layer mask for terrain
    [SerializeField] private Transform MineContainer; // Parent transform for spawned Mines
    [SerializeField] private bool showDebugGizmos = false; // Toggle for debug gizmos
    [SerializeField] private int maxRaycastPoints = 100; // Limit stored points
    [SerializeField] private Vector2 xSpawnRange; // X-axis spawn range
    [SerializeField] private Vector2 zSpawnRange; // Z-axis spawn range

    private List<Vector3> raycastHitPoints = new List<Vector3>(); // Store hit points for Gizmos
    private Collider[] colliders = new Collider[1];
    private float MineRadius;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        StartCoroutine(DelayedSpawnMines());
    }

    private IEnumerator DelayedSpawnMines()
    {
        yield return new WaitForSeconds(1.5f); // Wait for 1 second

        // Use StoneMinePrefab as reference for radius
        MineRadius = StoneMinePrefab.GetComponent<SphereCollider>().radius; 

        for (int i = 0; i < maxMines; i++)
        {
            SpawnMine();
        }

        // Tüm ağaçlar oluşturuldu, event'i tetikle
        isGenerationComplete = true;
        //Debug.Log("MineSpawner: All Mines spawned successfully!");
        OnMineGenerationComplete?.Invoke();
    }

    private void SpawnMine()
    {
        Vector3 spawnPoint = GetSpawnPoint();
        Quaternion rotation = Quaternion.identity; // Default rotation

        // Align Mine with terrain normal
        Ray ray = new Ray(spawnPoint + Vector3.up * 10f, Vector3.down); 
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, terrainLayerMask))
        {
            rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
        }

        // Select a random mine prefab based on probability
        GameObject selectedPrefab = GetRandomMinePrefab();
        GameObject MineInstance = Instantiate(selectedPrefab, spawnPoint, rotation);

        var networkObject = MineInstance.GetComponent<NetworkObject>();
        if (networkObject != null)
        {
            networkObject.Spawn(true); // Ensure the Mine is spawned on the network
            //Debug.Log($"Mine spawned on network at"); //Debug.Log($"Mine spawned on network at {spawnPoint}");
        }
        else
        {
            Debug.LogError("Mine prefab is missing a NetworkObject component!");
        }

        MineInstance.transform.SetParent(MineContainer);

    }

    // New method to select a mine prefab based on probability
    private GameObject GetRandomMinePrefab()
    {
        float random = UnityEngine.Random.Range(0f, 1f);
        
        if (random <= 0.4f) // 40% chance for Stone
            return StoneMinePrefab;
        else if (random <= 0.7f) // 30% chance for Iron (0.4 + 0.3 = 0.7)
            return IronMinePrefab;
        else if (random <= 0.9f) // 20% chance for Gold (0.7 + 0.2 = 0.9)
            return GoldMinePrefab;
        else // 10% chance for Diamond
            return DiamondMinePrefab;
    }


    private Vector3 GetSpawnPoint()
    {
        float x = 0f;
        float z = 0f;
        int maxAttempts = 100; // Add a limit to prevent infinite loops
        int attempts = 0;

        while (attempts < maxAttempts)
        {
            attempts++;
            x = UnityEngine.Random.Range(xSpawnRange.x, xSpawnRange.y); // Random x within range
            z = UnityEngine.Random.Range(zSpawnRange.x, zSpawnRange.y); // Random z within range

            Vector3 spawnPoint = new Vector3(x, 30f, z); // Start raycast from a higher y-value
            Ray ray = new Ray(spawnPoint, Vector3.down); // Ray pointing downward
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, terrainLayerMask)) // Check for terrain collision
            {
                // Always log raycast hits regardless of debug settings
                //Debug.Log($"Food spawn raycast hit: "); //Debug.Log($"Food spawn raycast hit: {hit.collider.name} at {hit.point}");

                if (showDebugGizmos)
                {
                    // Limit the size of the debug points list
                    if (raycastHitPoints.Count < maxRaycastPoints)
                    {
                        raycastHitPoints.Add(hit.point);
                    }
                }

                spawnPoint.y = hit.point.y; // Set y to terrain height

                int numColliders = Physics.OverlapSphereNonAlloc(
                    spawnPoint,
                    0.5f, // Adjust radius as needed
                    colliders,
                    nonTerrainLayerMask);

                if (numColliders == 0) // Check for colliders in the area
                {
                    return spawnPoint;
                }

            }
            else
            {
                //Debug.Log("Mine raycast did not hit any terrain.");
            }
        }

        // Fallback if we couldn't find a spot after max attempts
        return new Vector3(x, 10f, z);
    }

    private void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        Gizmos.color = Color.green;

        foreach (Vector3 point in raycastHitPoints)
        {
            Gizmos.DrawSphere(point, 0.5f);
        }
    }

    // Add method to clear debug points
    public void ClearDebugPoints()
    {
        raycastHitPoints.Clear();
    }
}


