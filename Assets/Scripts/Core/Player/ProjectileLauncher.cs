using Unity.Netcode;
using Unity.Services.Matchmaker.Models;
using UnityEngine;

public class ProjectileLauncher : NetworkBehaviour
{
    [SerializeField] private InputReader inputReader;
    [SerializeField] private FoodWallet wallet;
    [SerializeField] private Transform projectileSpawnPoint;
    [SerializeField] private GameObject ServerProjectile;
    [SerializeField] private GameObject ClientProjectile;

    [SerializeField] private float projectileSpeed;

    [SerializeField] private Collider playerCollider;

    [SerializeField] private GameObject muzzleFlashPrefab;
    [SerializeField] private float fireRate;
    [SerializeField] private float muzzleFlashDuration;
    [SerializeField] private int costToFire;


    private bool shouldFire;
    private float timer;
    private float muzzleFlashTimer;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) { return; }

        inputReader.PrimaryFireEvent += HandleFire;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) { return; }

        inputReader.PrimaryFireEvent -= HandleFire;
    }

    private void HandleFire(bool shouldFire)
    {
        this.shouldFire = shouldFire;

    }

    private void Update()
    {
        if (muzzleFlashTimer > 0)  //Counter for muzzle flash light
        {
            muzzleFlashTimer -= Time.deltaTime;

            if (muzzleFlashTimer <= 0)
            {
                muzzleFlashPrefab.SetActive(false);
            }

        }

        if (!IsOwner) { return; }

        if (timer > 0)
        {
            timer -= Time.deltaTime;
        }

        if (!shouldFire) { return; }

        if (timer > 0) { return; }// timer 0'dan büyükse atış yapmasın.

        if (wallet.TotalFoods.Value < costToFire) { return; } //para yoksa atış gerçekleşmesin.

        SpawnServerProjectileServerRpc(projectileSpawnPoint.position, projectileSpawnPoint.forward);
        SpawnDummyProjectile(projectileSpawnPoint.position, projectileSpawnPoint.forward);

        timer = 1 / fireRate;//fire rate'i saniyeye çevirir. 1 saniyede kaç mermi atılacağını belirler.
        //timer 0 olduğunda spawn metodları çalıştırılıyor ve tekrar timera bir değer atanıyor. Bu sayede atışlar belirli aralıklarla yapılıyor.
    }

    private void SpawnDummyProjectile(Vector3 spawnPosition, Vector3 direction) //Sahte mermi oluşturur. Kullanıcı attığı merminin anlık tepkisini görmesi için. mermiyi alacak kullanıcı da dummy oluşturur.
    {
        muzzleFlashPrefab.SetActive(true);       // muzzle flash light
        muzzleFlashTimer = muzzleFlashDuration;

        GameObject projectileInstance = Instantiate(ClientProjectile, spawnPosition, Quaternion.identity);

        projectileInstance.transform.forward = direction;


        Physics.IgnoreCollision(playerCollider, projectileInstance.GetComponent<Collider>());

        if (projectileInstance.TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            rb.linearVelocity = rb.transform.forward * projectileSpeed;
        }

    }

    [ServerRpc] //istemciden sunucuya çağrı göndermek için kullanılır.
    private void SpawnServerProjectileServerRpc(Vector3 spawnPosition, Vector3 direction) //
    {
        if (wallet.TotalFoods.Value < costToFire) { return; } //para yoksa atış gerçekleşmesin.

        wallet.SpendCoins(costToFire); //parayı harca. sadece server harcayabilir.


        GameObject projectileInstance = Instantiate(ServerProjectile, spawnPosition, Quaternion.identity); //   Quaternion.identity = 0,0,0,1

        projectileInstance.transform.forward = direction; // merminin spawnlandığında bakacağı yön

        SpawnServerProjectileClientRpc(spawnPosition, direction);

        Physics.IgnoreCollision(playerCollider, projectileInstance.GetComponent<Collider>()); // projectile kedi player colliderına çarpmasın diye


        projectileInstance.GetComponent<DealDamageOnContact>().SetOwner(OwnerClientId); // projectile'daki DealDamageOnContact scriptine bu kodun sahibinin id'sini veriyoruz ki bizi vuramasın.

        if (projectileInstance.TryGetComponent<Rigidbody>(out Rigidbody rb))//bir nesnenin bir bileşeni var mı yok mu kontrol eder. Varsa bileşeni döndürür, yoksa null döndürür.
        {
            rb.linearVelocity = rb.transform.forward * projectileSpeed;
        }
    }

    [ClientRpc] //sunucudan istemciye çağrı göndermek için kullanılır.
    private void SpawnServerProjectileClientRpc(Vector3 spawnPosition, Vector3 direction)
    {
        if (IsOwner) { return; }

        SpawnDummyProjectile(spawnPosition, direction);

    }
}
