using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using System;

public class TreeSpawner : NetworkBehaviour
{
    // Ağaç oluşturma tamamlandığında tetiklenecek event
    public static event Action OnTreeGenerationComplete;

    // Ağaçlar oluşturuldu mu?
    private bool isGenerationComplete = false;
    public bool IsGenerationComplete => isGenerationComplete;

    [SerializeField] private GameObject treePrefab; // Tree prefab to spawn
    [SerializeField] private int maxTrees = 100; // Maximum number of trees to spawn
    [SerializeField] private LayerMask terrainLayerMask; // Layer mask for terrain
    [SerializeField] private LayerMask nonTerrainLayerMask; // Layer mask for terrain
    [SerializeField] private Transform treeContainer; // Parent transform for spawned trees
    [SerializeField] private bool showDebugGizmos = false; // Toggle for debug gizmos
    [SerializeField] private int maxRaycastPoints = 100; // Limit stored points
    [SerializeField] private Vector2 xSpawnRange; // X-axis spawn range
    [SerializeField] private Vector2 zSpawnRange; // Z-axis spawn range

    private List<Vector3> raycastHitPoints = new List<Vector3>(); // Store hit points for Gizmos
    private Collider[] colliders = new Collider[1];
    private float treeRadius;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        StartCoroutine(DelayedSpawnTrees());
    }

    private IEnumerator DelayedSpawnTrees()
    {
        yield return new WaitForSeconds(1f); // Wait for 1 second

        treeRadius = treePrefab.GetComponent<CapsuleCollider>().radius; // Get the radius of the tree collider

        for (int i = 0; i < maxTrees; i++)
        {
            SpawnTree();
        }

        // Tüm ağaçlar oluşturuldu, event'i tetikle
        isGenerationComplete = true;
        Debug.Log("TreeSpawner: All trees spawned successfully!");
        OnTreeGenerationComplete?.Invoke();
    }

    private void SpawnTree()
    {
        Vector3 spawnPoint = GetSpawnPoint();
        Quaternion rotation = Quaternion.identity;

        // Align tree with terrain normal
        Ray ray = new Ray(spawnPoint + Vector3.up * 10f, Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, terrainLayerMask))
        {
            rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
        }

        GameObject treeInstance = Instantiate(treePrefab, spawnPoint, rotation);

        var networkObject = treeInstance.GetComponent<NetworkObject>();
        if (networkObject != null)
        {
            networkObject.Spawn(true); // Ensure the tree is spawned on the network
            Debug.Log($"Tree spawned on network at"); //Debug.Log($"Tree spawned on network at {spawnPoint}");
        }
        else
        {
            Debug.LogError("Tree prefab is missing a NetworkObject component!");
        }

        treeInstance.transform.SetParent(treeContainer);

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
                Debug.Log($"Food spawn raycast hit: "); //Debug.Log($"Food spawn raycast hit: {hit.collider.name} at {hit.point}");

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
                Debug.Log("Tree raycast did not hit any terrain.");
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


