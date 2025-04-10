// using System.Runtime.CompilerServices;
// using Unity.Netcode;
// using UnityEngine;

// public class CoinSpawnerOld : NetworkBehaviour
// {
//     [SerializeField] private RespawningCoin coinPrefab;
//     [SerializeField] private int maxCoins = 50;
//     [SerializeField] private int coinValue = 10;
//     [SerializeField] private Vector2 xSpawnRange;
//     [SerializeField] private Vector2 zSpawnRange;
//     [SerializeField] private LayerMask layerMask;


//     private Collider[] colliders = new Collider[1];//neden 1 taneyse? çünkü sadece bir tane collider almak istiyoruz. collider varsa orda coin spawn etmeyelim diye.
//     private float coinRadius;//coinin yarıçapı. Çünkü coinin yarıçapı kadar alanı kontrol edeceğiz o alanda collider var mı diye

//     public override void OnNetworkSpawn()
//     {

//         if (!IsServer) { return; }

//         coinRadius = coinPrefab.GetComponent<SphereCollider>().radius;

//         for (int i = 0; i < maxCoins; i++)
//         {
//             SpawnCoin();
//         }
//     }

//     private void SpawnCoin()
//     {
//         RespawningCoin coinInstance = Instantiate(//coinInstance, coinPrefab'den bir instance oluşturur. ve oyuna spawn eder.
//             coinPrefab,
//             GetSpawnPoint(),
//             Quaternion.identity);

//         coinInstance.SetValue(coinValue);//coinin değerini belirle. 10 coin değeri verildi.

//         coinInstance.GetComponent<NetworkObject>().Spawn();//Prefab'de Network Object olsa da her Network Object'in ayrı bir kimliği olacağı için bu prefabden spawn ettiğimizde onun
//         //bir kimliği olması lazım. prefabdeki id'yi veremeyiz çünkü hepsinin farklı bir id'si olması lazım. bu satırı çalıştırmazsan NetworkObject compoentinde "Spawn" tuşu çıkıyor.
//         //bastığımızda o objeye bir kimlik veriyor. 
//         //ve sunucu olmayan client'lerde oluşmaz. 
//         //Host parayı oluşturduğunda parayı almaya çalıştığımda Collect() çalışıyor. Collect()'de client kısmı çalışıyor yani parayı gizleme kodu. Server için yazılmış kodlar çalışmıyor.
//         //çünkü server id'si olmadığı için görmüyor.

//         coinInstance.OnCollected += HandleCoinCollected; // yeni oluşturulan coini abone ediyoruz ki coin toplandığında ne yapacağını bilsin.

//     }

//     private void HandleCoinCollected(RespawningCoin coin) //para toplandığında çalışır
//     {
//         coin.transform.position = GetSpawnPoint();// yeni bir spawn point belirle ve coin'i oraya taşı.
//         coin.Reset();                           //coinin toplandı bool''unu sıfırlar.               
//         //bunları yapmamızın nedeni coini başka yere yerleştirip aslında yeni coin oluşmuş gibi göstermek. yeniden spawn etmek yerine yerini değiştiriyoruz ki haritada para azalmasın. 
//     }
//     private Vector3 GetSpawnPoint()
//     {

//         float x = 0f;
//         float z = 0f;

//         while (true)
//         {

//             x = Random.Range(xSpawnRange.x, xSpawnRange.y);//xSpawnRange.x ve xSpawnRange.z arasında bir x değeri seç.
//             z = Random.Range(zSpawnRange.x, zSpawnRange.y);//xspawnRange1, xpawnRange2 diye int tanımlayıp da yapabilirdik.

//             Vector3 spawnPoint = new Vector3(x, coinPrefab.transform.position.y, z);

//             int numColliders = Physics.OverlapSphereNonAlloc( //bu metot, belirtilen konumda belirtilen yarıçap içindeki colliderları döndürür. herhangi bir collider varsa orası doludur
//                 spawnPoint,                                   // oraya coin spawn etmeyelim diye. Layermask, belirttiğimiz layerda mı bulduğunu kontrol eder.
//                 coinRadius,
//                 colliders,
//                 layerMask);

//             if (numColliders == 0)//collider bulamazsan bu spawn pointi döndür.
//             {
//                 return spawnPoint;
//             }


//         }

//     }

// }
