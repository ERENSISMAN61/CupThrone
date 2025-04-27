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

    //[SerializeField] private GameObject muzzleFlashPrefab;
    [SerializeField] private float muzzleFlashDuration;
    [SerializeField] private int costToFire;

    // Eski shouldFire değişkenini kaldırdık
    private bool canFire = true;
    private float muzzleFlashTimer;

    [Header("Charging Settings")]
    [SerializeField] private float chargeSpeed = 5f; // Default 5 units per second
    [SerializeField] private float maxChargeValue = 10f;
    [SerializeField] private float minChargeToFire = 0.5f;// değiştirilip silinebilir belki.

    [SerializeField] private float chargeValue = 0f;
    private bool isCharging = false;

    [SerializeField] private HandItems handItems; // Okun tutulduğu el nesnesi

    [Header("Bow Scale Settings")]
    [SerializeField] private Transform bowTransform; // Yayın transform bileşeni
    [SerializeField] private float maxBowScaleY = 1.5f; // Şarj max olduğunda yayın Y ölçeği
    [SerializeField] private float bowResetDuration = 0.3f; // Yayın normal haline dönme süresi (saniye)
    private Vector3 originalBowScale; // Yayın orijinal ölçeğini saklamak için
    private float bowResetTimer = 0f; // Yayın normale dönüş sayacı
    private bool isResetting = false; // Yayın normale dönüş durumu
    private Vector3 bowScaleAtRelease; // Ok bırakıldığında yayın ölçeği

    // Yeni değişken: Sıfırlama sırasında buton basılı tutuluyorsa takip eder
    private bool isFireButtonHeldDuringReset = false;

    private void Awake()
    {
        // Eğer kamera atanmadıysa, ana kamerayı bulmaya çalış
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        // Yayın orijinal ölçeğini sakla
        if (bowTransform != null)
        {
            originalBowScale = bowTransform.localScale;
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
            // Ateş düğmesi basılıysa işaretle
            if (isResetting)
            {
                isFireButtonHeldDuringReset = true;
                Debug.Log("Ateş düğmesi reset sırasında basılı tutuluyor");
            }
            StartCharging();
        }
        else
        {
            // Ateş düğmesi bırakıldığında işareti kaldır
            isFireButtonHeldDuringReset = false;
            StopCharging();
        }
    }

    private void StartCharging()
    {
        // Reset animasyonu sırasında şarj etmeyi engelle ama basılı tutulduğunu kaydet
        if (isResetting)
        {
            Debug.Log("Yay resetleniyor, şarj başlatılamaz!");
            return;
        }
        
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

        // Minimum şarj kontrolü ve atış yapabilme durumu
        if (chargeValue >= minChargeToFire && canFire)
        {
            if (wallet.TotalArrows.Value >= costToFire)
            {
                Debug.Log($"Ok atılıyor! Şarj: {chargeValue}, Yön: {cameraDirection}");
                SpawnServerProjectileServerRpc(projectileSpawnPoint.position, cameraDirection);
                SpawnDummyProjectile(projectileSpawnPoint.position, cameraDirection);

                // Yeni ok atışından sonra, yay resetlenene kadar ateş etmeyi devre dışı bırak
                canFire = false;
            }
            else
            {
                Debug.Log("Yeterli ok yok!");
            }
        }
        else if (!canFire)
        {
            Debug.Log("Yay henüz hazır değil!");
        }
        else
        {
            Debug.Log($"Atış başarısız. Şarj: {chargeValue}, CanFire: {canFire}");
        }

        // Şarj değerini sıfırla
        chargeValue = 0f;

        // Yayı orijinal ölçeğine geri döndür
        ResetBowScale();
    }

    private void Update()
    {
        // Muzzle flash işlemi
        if (muzzleFlashTimer > 0)
        {
            muzzleFlashTimer -= Time.deltaTime;

            if (muzzleFlashTimer <= 0)
            {
                //muzzleFlashPrefab.SetActive(false);
            }
        }

        if (!IsOwner) { return; }

        // Yay animasyonu - reset durumunda yumuşak geçiş
        if (isResetting)
        {
            bowResetTimer += Time.deltaTime;
            float t = Mathf.Clamp01(bowResetTimer / bowResetDuration);

            // Easing function - daha doğal bir animasyon için
            t = 1f - Mathf.Pow(1f - t, 2f); // Quadratic ease-out

            if (bowTransform != null)
            {
                bowTransform.localScale = Vector3.Lerp(bowScaleAtRelease, originalBowScale, t);
            }

            if (bowResetTimer >= bowResetDuration)
            {
                isResetting = false;
                bowTransform.localScale = originalBowScale;

                // Yay reset animasyonu tamamlandığında ateş etmeyi tekrar aktif et
                canFire = true;
                Debug.Log("Yay hazır! Tekrar ateş edilebilir.");

                // Reset tamamlandığında düğme hala basılı tutuluyorsa şarj etmeye başla
                if (isFireButtonHeldDuringReset)
                {
                    Debug.Log("Düğme hala basılı, şarj başlatılıyor!");
                    isCharging = true;
                    isFireButtonHeldDuringReset = false; // İşlemi gerçekleştirdikten sonra bayrağı sıfırla
                }
            }
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

            // Yayın Y ölçeğini şarj değerine göre ayarla
            UpdateBowScale();

            if (chargeValue % 1f < 0.01f)
            {
                Debug.Log($"Şarj ediliyor: {Mathf.Floor(chargeValue)}/{maxChargeValue}");
            }
        }
    }

    private void SpawnDummyProjectile(Vector3 spawnPosition, Vector3 direction)
    {
        //muzzleFlashPrefab.SetActive(true);
        muzzleFlashTimer = muzzleFlashDuration;

        // Yön vektörüne dayalı bir rotasyon hesaplıyoruz
        Quaternion rotation = Quaternion.LookRotation(direction);

        // Rotation parametresi olarak hesapladığımız rotasyonu kullanıyoruz
        GameObject projectileInstance = Instantiate(ClientProjectile, spawnPosition, rotation);

        // Yönü tekrar ayarlamaya gerek yok, rotasyon ile birlikte forward zaten ayarlanacak
        // projectileInstance.transform.forward = direction;

        Physics.IgnoreCollision(playerCollider, projectileInstance.GetComponent<Collider>());

        if (projectileInstance.TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            // Şarj değerine göre hız ayarla (1.5x - 3x arası)
            float speedMultiplier = 1f + (chargeValue / maxChargeValue * 2f);
            rb.linearVelocity = direction * (projectileSpeed * speedMultiplier);
        }
    }

    [ServerRpc]
    private void SpawnServerProjectileServerRpc(Vector3 spawnPosition, Vector3 direction)
    {
        if (wallet.TotalArrows.Value < costToFire) { return; }

        wallet.SpendCoins(costToFire);

        // Yön vektörüne dayalı bir rotasyon hesaplıyoruz
        Quaternion rotation = Quaternion.LookRotation(direction);

        // Rotation parametresi olarak hesapladığımız rotasyonu kullanıyoruz
        GameObject projectileInstance = Instantiate(ServerProjectile, spawnPosition, rotation);

        // Yönü tekrar ayarlamaya gerek yok
        // projectileInstance.transform.forward = direction;

        SpawnServerProjectileClientRpc(spawnPosition, direction);
        Physics.IgnoreCollision(playerCollider, projectileInstance.GetComponent<Collider>());
        projectileInstance.GetComponent<DealDamageOnContact>().SetOwner(OwnerClientId);

        if (projectileInstance.TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            // Şarj değerine göre hız ayarla (1.5x - 3x arası)
            float speedMultiplier = 1f + (chargeValue / maxChargeValue * 2f);
            rb.linearVelocity = direction * (projectileSpeed * speedMultiplier);
        }
    }

    [ClientRpc]
    private void SpawnServerProjectileClientRpc(Vector3 spawnPosition, Vector3 direction)
    {
        if (IsOwner) { return; }

        SpawnDummyProjectile(spawnPosition, direction);
    }

    // Yayın ölçeğini güncelleyen metod
    private void UpdateBowScale()
    {
        if (bowTransform == null) return;

        // Şarj değerine göre 1.0 ile maxBowScaleY arasında bir değer hesapla
        float currentScaleY = Mathf.Lerp(1.0f, maxBowScaleY, chargeValue / maxChargeValue);

        // Sadece Y ölçeğini değiştir, X ve Z aynı kalsın
        Vector3 newScale = new Vector3(
            originalBowScale.x,
            originalBowScale.y * currentScaleY,
            originalBowScale.z
        );

        bowTransform.localScale = newScale;
    }

    // Yayın ölçeğini orijinal haline döndüren metod
    private void ResetBowScale()
    {
        if (bowTransform == null) return;

        // Ani değişim yerine yumuşak geçiş için hazırlık
        bowResetTimer = 0f;
        isResetting = true;
        bowScaleAtRelease = bowTransform.localScale;
    }
}
