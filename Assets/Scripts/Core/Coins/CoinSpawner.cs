using System.Runtime.CompilerServices;
using Unity.Netcode;
using UnityEngine;
using System.Collections;
using System.Collections.Generic; // For List

public class CoinSpawner : NetworkBehaviour
{
    [SerializeField] private RespawningCoin coinPrefab;
    [SerializeField] private int maxCoins = 50;
    [SerializeField] private int coinValue = 10;
    [SerializeField] private Vector2 xSpawnRange;
    [SerializeField] private Vector2 zSpawnRange;
    [SerializeField] private LayerMask terrainLayerMask;
    [SerializeField] private LayerMask nonTerrainLayerMask;
    [SerializeField] private Transform coinContainer; // Parent transform for coins

    private List<Vector3> raycastHitPoints = new List<Vector3>(); // Store hit points for Gizmos
    private Collider[] colliders = new Collider[1];//neden 1 taneyse? çünkü sadece bir tane collider almak istiyoruz. collider varsa orda coin spawn etmeyelim diye.
    private float coinRadius;//coinin yarıçapı. Çünkü coinin yarıçapı kadar alanı kontrol edeceğiz o alanda collider var mı diye

    public override void OnNetworkSpawn()
    {
        if (!IsServer) { return; }

        StartCoroutine(DelayedSpawnCoins());
    }

    private IEnumerator DelayedSpawnCoins()
    {
        yield return new WaitForSeconds(1f); // Wait for 2 seconds

        coinRadius = coinPrefab.GetComponent<SphereCollider>().radius;

        for (int i = 0; i < maxCoins; i++)
        {
            SpawnCoin();
        }
    }

    private void SpawnCoin()
    {
        Vector3 spawnPoint = GetSpawnPoint();
        Quaternion rotation = Quaternion.identity; // Default rotation

        // Perform a raycast to get the terrain normal
        Ray ray = new Ray(spawnPoint + Vector3.up * 10f, Vector3.down); // Start raycast slightly above the spawn point
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, terrainLayerMask))
        {
            rotation = Quaternion.FromToRotation(Vector3.up, hit.normal); // Align rotation with terrain normal
        }

        RespawningCoin coinInstance = Instantiate(
            coinPrefab,
            spawnPoint,
            rotation);

        coinInstance.SetValue(coinValue);

        coinInstance.GetComponent<NetworkObject>().Spawn();


        coinInstance.transform.SetParent(coinContainer); // Set the parent transform for coins

        coinInstance.OnCollected += HandleCoinCollected;
    }

    private void HandleCoinCollected(RespawningCoin coin) //para toplandığında çalışır
    {
        coin.transform.position = GetSpawnPoint();// yeni bir spawn point belirle ve coin'i oraya taşı.
        coin.Reset();                           //coinin toplandı bool''unu sıfırlar.               
        //bunları yapmamızın nedeni coini başka yere yerleştirip aslında yeni coin oluşmuş gibi göstermek. yeniden spawn etmek yerine yerini değiştiriyoruz ki haritada para azalmasın. 
    }
    private Vector3 GetSpawnPoint()
    {
        float x = 0f;
        float z = 0f;

        while (true)
        {
            x = Random.Range(xSpawnRange.x, xSpawnRange.y); // Random x within range
            z = Random.Range(zSpawnRange.x, zSpawnRange.y); // Random z within range

            Vector3 spawnPoint = new Vector3(x, 30f, z); // Start raycast from a higher y-value
            Ray ray = new Ray(spawnPoint, Vector3.down); // Ray pointing downward
            RaycastHit hit;


            if (Physics.Raycast(ray, out hit, Mathf.Infinity, terrainLayerMask)) // Check for terrain collision
            {

                Debug.Log($"Raycast hit: {hit.collider.name} at {hit.point}"); // Log hit details
                spawnPoint.y = hit.point.y + 0.5f; // Set y to terrain height

                raycastHitPoints.Add(hit.point); // Store the hit point for Gizmos

                int numColliders = Physics.OverlapSphereNonAlloc(
                    spawnPoint,
                    coinRadius,
                    colliders,
                    nonTerrainLayerMask);

                if (numColliders == 0) // Check for colliders in the area
                {
                    return spawnPoint;
                }

            }
            else
            {
                Debug.Log("Raycast did not hit any terrain.");
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red; // Set Gizmos color to red

        foreach (Vector3 hitPoint in raycastHitPoints)
        {
            Gizmos.DrawSphere(hitPoint, 0.5f); // Draw a sphere at each hit point
        }
    }
}
