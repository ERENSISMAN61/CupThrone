using Unity.Netcode;
using Unity.Services.Matchmaker.Models;
using UnityEngine;

public class ProjectileLauncher : NetworkBehaviour
{
    [SerializeField] private InputReader inputReader;
    [SerializeField] private ArrowWallet wallet;
    [SerializeField] private Transform projectileSpawnPoint;
    [SerializeField] private GameObject ServerProjectile;
    [SerializeField] private GameObject ClientProjectile;

    [Header("Camera Settings")]
    [SerializeField] private Camera playerCamera; // Oyuncu kamerası referansı

    [SerializeField] private float projectileSpeed;

    [SerializeField] private Collider playerCollider;

    [SerializeField] private GameObject muzzleFlashPrefab;
    [SerializeField] private float fireRate;
    [SerializeField] private float muzzleFlashDuration;
    [SerializeField] private int costToFire;

    // Eski shouldFire değişkenini kaldırdık
    private float timer;
    private float muzzleFlashTimer;

    [Header("Charging Settings")]
    [SerializeField] private float chargeSpeed = 5f; // Default 5 units per second
    [SerializeField] private float maxChargeValue = 10f;
    [SerializeField] private float minChargeToFire = 0.5f;// değiştirilip silinebilir belki.

    [SerializeField] private float chargeValue = 0f;
    private bool isCharging = false;

    [SerializeField] private HandItems handItems; // Okun tutulduğu el nesnesi

    private void Awake()
    {
        // Eğer kamera atanmadıysa, ana kamerayı bulmaya çalış
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
    }

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

    private void HandleFire(bool isPressed)
    {
        Debug.Log($"HandleFire: isPressed={isPressed}");

        if (!handItems.GetHasBowInHand()) return;// yay elinde değilse işlem yapma


        if (isPressed)
        {
            StartCharging();
        }
        else
        {
            StopCharging();
        }
    }

    private void StartCharging()
    {
        Debug.Log("StartCharging: Başladı");
        isCharging = true;
    }

    private void StopCharging()
    {
        // Şarj etmiyorsak işlem yapma
        if (!isCharging) return;

        isCharging = false;
        Debug.Log($"StopCharging: Şarj değeri = {chargeValue}");

        // Kameranın baktığı yönü al
        Vector3 cameraDirection = playerCamera != null ? playerCamera.transform.forward : projectileSpawnPoint.forward;

        // Minimum şarj kontrolü
        if (chargeValue >= minChargeToFire && timer <= 0)
        {
            if (wallet.TotalArrows.Value >= costToFire)
            {
                Debug.Log($"Ok atılıyor! Şarj: {chargeValue}, Yön: {cameraDirection}");
                SpawnServerProjectileServerRpc(projectileSpawnPoint.position, cameraDirection);
                SpawnDummyProjectile(projectileSpawnPoint.position, cameraDirection);
                timer = 1 / fireRate;
            }
            else
            {
                Debug.Log("Yeterli ok yok!");
            }
        }
        else
        {
            Debug.Log($"Atış başarısız. Şarj: {chargeValue}, Timer: {timer}");
        }

        // Şarj değerini sıfırla
        chargeValue = 0f;
    }

    private void Update()
    {
        // Muzzle flash işlemi
        if (muzzleFlashTimer > 0)
        {
            muzzleFlashTimer -= Time.deltaTime;

            if (muzzleFlashTimer <= 0)
            {
                muzzleFlashPrefab.SetActive(false);
            }
        }

        if (!IsOwner) { return; }

        // Timer işleme
        if (timer > 0)
        {
            timer -= Time.deltaTime;
        }

        // Şarj mekanizması - doğru yerde çalışması için buraya taşındı
        if (isCharging)
        {
            // Kamerayı kontrol et
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
                if (playerCamera == null)
                {
                    Debug.LogWarning("Kamera bulunamadı!");
                }
            }

            chargeValue += chargeSpeed * Time.deltaTime;
            chargeValue = Mathf.Clamp(chargeValue, 0f, maxChargeValue);

            if (chargeValue % 1f < 0.01f)
            {
                Debug.Log($"Şarj ediliyor: {Mathf.Floor(chargeValue)}/{maxChargeValue}");
            }
        }
    }

    private void SpawnDummyProjectile(Vector3 spawnPosition, Vector3 direction)
    {
        muzzleFlashPrefab.SetActive(true);
        muzzleFlashTimer = muzzleFlashDuration;

        GameObject projectileInstance = Instantiate(ClientProjectile, spawnPosition, Quaternion.identity);
        projectileInstance.transform.forward = direction;
        Physics.IgnoreCollision(playerCollider, projectileInstance.GetComponent<Collider>());

        if (projectileInstance.TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            // Şarj değerine göre hız ayarla (1.5x - 3x arası)
            float speedMultiplier = 1f + (chargeValue / maxChargeValue * 2f);
            rb.linearVelocity = rb.transform.forward * (projectileSpeed * speedMultiplier);
        }
    }

    [ServerRpc]
    private void SpawnServerProjectileServerRpc(Vector3 spawnPosition, Vector3 direction)
    {
        if (wallet.TotalArrows.Value < costToFire) { return; }

        wallet.SpendCoins(costToFire);

        GameObject projectileInstance = Instantiate(ServerProjectile, spawnPosition, Quaternion.identity);
        projectileInstance.transform.forward = direction;
        SpawnServerProjectileClientRpc(spawnPosition, direction);
        Physics.IgnoreCollision(playerCollider, projectileInstance.GetComponent<Collider>());
        projectileInstance.GetComponent<DealDamageOnContact>().SetOwner(OwnerClientId);

        if (projectileInstance.TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            // Şarj değerine göre hız ayarla (1.5x - 3x arası)
            float speedMultiplier = 1f + (chargeValue / maxChargeValue * 2f);
            rb.linearVelocity = rb.transform.forward * (projectileSpeed * speedMultiplier);
        }
    }

    [ClientRpc]
    private void SpawnServerProjectileClientRpc(Vector3 spawnPosition, Vector3 direction)
    {
        if (IsOwner) { return; }

        SpawnDummyProjectile(spawnPosition, direction);
    }
}
