using System.Runtime.CompilerServices;
using Unity.Netcode;
using UnityEngine;
using System.Collections;
using System.Collections.Generic; // For List

public class FoodSpawner : NetworkBehaviour
{
    [SerializeField] private RespawningFood foodPrefab;
    [SerializeField] private int maxFoods = 50;
    [SerializeField] private int foodValue = 10;
    [SerializeField] private Vector2 xSpawnRange;
    [SerializeField] private Vector2 zSpawnRange;
    [SerializeField] private LayerMask terrainLayerMask;
    [SerializeField] private LayerMask nonTerrainLayerMask;
    [SerializeField] private Transform foodContainer; // Parent transform for foods
    [SerializeField] private bool showDebugGizmos = false; // Add debug toggle
    [SerializeField] private int maxRaycastPoints = 100; // Limit stored points

    private List<Vector3> raycastHitPoints = new List<Vector3>(); // Store hit points for Gizmos
    private Collider[] colliders = new Collider[1];//neden 1 taneyse? çünkü sadece bir tane collider almak istiyoruz. collider varsa orda food spawn etmeyelim diye.
    private float foodRadius;//foodin yarıçapı. Çünkü foodin yarıçapı kadar alanı kontrol edeceğiz o alanda collider var mı diye

    public override void OnNetworkSpawn()
    {

        if (!IsServer) { return; }

        StartCoroutine(DelayedSpawnFoods());
    }

    private IEnumerator DelayedSpawnFoods()
    {
        yield return new WaitForSeconds(1f); // Wait for 2 seconds

        foodRadius = foodPrefab.GetComponent<SphereCollider>().radius;

        for (int i = 0; i < maxFoods; i++)
        {
            SpawnFood();
        }
    }

    private void SpawnFood()
    {
        Vector3 spawnPoint = GetSpawnPoint();
        Quaternion rotation = Quaternion.identity; // Default rotation

        // Perform a raycast to get the terrain normal
        Ray ray = new Ray(spawnPoint + Vector3.up * 10f, Vector3.down); // Start raycast slightly above the spawn point
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, terrainLayerMask))
        {
            Debug.Log($"Food normal raycast hit:"); //Debug.Log($"Food normal raycast hit: {hit.collider.name} at {hit.point}");
            rotation = Quaternion.FromToRotation(Vector3.up, hit.normal); // Align rotation with terrain normal
        }

        RespawningFood foodInstance = Instantiate(
            foodPrefab,
            spawnPoint,
            rotation);

        foodInstance.SetValue(foodValue);

        foodInstance.GetComponent<NetworkObject>().Spawn();


        foodInstance.transform.SetParent(foodContainer); // Set the parent transform for foods

        foodInstance.OnCollected += HandleFoodCollected;
    }

    private void HandleFoodCollected(RespawningFood food) //para toplandığında çalışır
    {
        food.transform.position = GetSpawnPoint();// yeni bir spawn point belirle ve food'i oraya taşı.
        food.Reset();                           //foodin toplandı bool''unu sıfırlar.               
        //bunları yapmamızın nedeni foodi başka yere yerleştirip aslında yeni food oluşmuş gibi göstermek. yeniden spawn etmek yerine yerini değiştiriyoruz ki haritada para azalmasın. 
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
            x = Random.Range(xSpawnRange.x, xSpawnRange.y); // Random x within range
            z = Random.Range(zSpawnRange.x, zSpawnRange.y); // Random z within range

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

                spawnPoint.y = hit.point.y + 0.5f; // Set y to terrain height

                int numColliders = Physics.OverlapSphereNonAlloc(
                    spawnPoint,
                    foodRadius,
                    colliders,
                    nonTerrainLayerMask);

                if (numColliders == 0) // Check for colliders in the area
                {
                    return spawnPoint;
                }

            }
            else
            {
                Debug.Log("Food raycast did not hit any terrain.");
            }
        }

        // Fallback if we couldn't find a spot after max attempts
        return new Vector3(x, 10f, z);
    }

    private void OnDrawGizmos()
    {
        // Only draw gizmos if debugging is enabled
        if (!showDebugGizmos) return;

        Gizmos.color = Color.red; // Set Gizmos color to red

        foreach (Vector3 hitPoint in raycastHitPoints)
        {
            Gizmos.DrawSphere(hitPoint, 0.5f); // Draw a sphere at each hit point
        }
    }

    // Add method to clear debug points
    public void ClearDebugPoints()
    {
        raycastHitPoints.Clear();
    }
}
