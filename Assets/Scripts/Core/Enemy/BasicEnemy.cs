using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class BasicEnemy : Enemy
{
    [SerializeField] private float moveSpeed = 3.0f; // Düşmanın hareket hızı
    [SerializeField] private float detectionRadius = 10.0f; // Düşmanın oyuncuyu algılama yarıçapı
    [SerializeField] private Transform enemyModel; // Düşmanın 3D modeli

    [Header("Patrol Ayarları")]
    [SerializeField] private bool enablePatrol = true; // Devriye davranışını etkinleştir/devre dışı bırak
    [SerializeField] private float patrolRadius = 20f; // Devriye gezme yarıçapı
    [SerializeField] private float minPatrolWaitTime = 2f; // Devriye noktaları arasında minimum bekleme süresi
    [SerializeField] private float maxPatrolWaitTime = 5f; // Devriye noktaları arasında maksimum bekleme süresi
    [SerializeField] private float patrolPointDistance = 1f; // Devriye noktasına ne kadar yaklaşılınca "ulaşılmış" sayılır

    [Header("Animasyon Kontrolleri")]
    [SerializeField] private Animator animator; // Animator bileşeni
    [SerializeField] private float movementThreshold = 0.2f; // Hareket algılama eşiği
    [SerializeField] float actualSpeed = 0f;

    private NavMeshAgent navMeshAgent; // Düşmanın hareket bileşeni
    private Transform targetPlayer; // Hedef oyuncu transformu
    private bool navMeshInitialized = false; // NavMesh başlatıldı mı
    private Coroutine initNavMeshCoroutine;
    private Coroutine patrolCoroutine;
    private Vector3 spawnPosition; // Düşmanın başlangıç pozisyonu
    private Vector3 currentPatrolTarget; // Şu anki devriye hedefi
    private bool isWaitingAtPatrolPoint = false; // Devriye noktasında bekliyor mu
    private bool isPatrolling = false; // Devriye geziyor mu

    // Animator parametreleri
    private readonly int isIdleParam = Animator.StringToHash("isIdle");
    private readonly int isWalkingParam = Animator.StringToHash("isWalking");
    private readonly int isGettingHitParam = Animator.StringToHash("isGettingHit");
    private readonly int isPunchingParam = Animator.StringToHash("isPunching");

    public event Action<BasicEnemy> OnEnemyDefeated; // Düşman yenildiğinde tetiklenen olay

    private void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        if (navMeshAgent != null)
        {
            // NavMesh hazır olana kadar devre dışı bırak
            navMeshAgent.enabled = false;
        }

        // Animator referansını otomatik olarak almayı dene
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null && enemyModel != null)
            {
                animator = enemyModel.GetComponent<Animator>();
            }
        }

        // Spawn pozisyonunu kaydet
        spawnPosition = transform.position;

        // Başlangıçta Idle animasyonu ayarla
        SetIdleAnimation();
    }

    private void OnEnable()
    {
        // NavMesh hazır olduğunda bildirim almak için event'e abone ol
        DynamicNavMeshBuilder.OnNavMeshReady += OnNavMeshReady;
    }

    private void OnDisable()
    {
        // Aboneliği kaldır
        DynamicNavMeshBuilder.OnNavMeshReady -= OnNavMeshReady;

        // Coroutine varsa durdur
        if (initNavMeshCoroutine != null)
        {
            StopCoroutine(initNavMeshCoroutine);
            initNavMeshCoroutine = null;
        }
    }

    // NavMesh hazır olduğunda çağrılır
    private void OnNavMeshReady()
    {
        // Client'ler de bu event'i alacak ancak NavMesh oluşturmayacak
        // Sadece isNavMeshReady durumunu güncelleyecek

        // NavMesh hazır olduğu bildirildiğinde NavMeshAgent'ı başlat
        if (navMeshAgent != null && !navMeshAgent.enabled)
        {
            Debug.Log($"BasicEnemy: NavMesh hazır bildirimi alındı, NavMeshAgent başlatılıyor ({gameObject.name})");
            InitializeNavMeshAgent();
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // NavMesh ve NavMeshAgent'ı başlat
        if (initNavMeshCoroutine != null)
            StopCoroutine(initNavMeshCoroutine);

        // DynamicNavMeshBuilder'ı kontrol et - belki NavMesh zaten hazırdır
        if (DynamicNavMeshBuilder.Instance != null && DynamicNavMeshBuilder.Instance.IsNavMeshReady)
        {
            InitializeNavMeshAgent();
        }
        else
        {
            // NavMesh hazır değilse, hazır olup olmadığını düzenli olarak kontrol et
            initNavMeshCoroutine = StartCoroutine(WaitForNavMeshAndInitialize());
        }

        // Sadece sunucu tarafında mantık başlat
        if (IsServer)
        {
            InvokeRepeating(nameof(FindNearestPlayer), 1.0f, 1.0f);
        }
    }

    private IEnumerator WaitForNavMeshAndInitialize()
    {
        Debug.Log($"BasicEnemy: NavMesh'in hazır olması bekleniyor ({gameObject.name})");

        // DynamicNavMeshBuilder'ın NavMesh'i oluşturmasını bekle
        int attempts = 0;
        int maxAttempts = 20; // Maksimum bekleme denemesi

        while (attempts < maxAttempts)
        {
            attempts++;

            if (DynamicNavMeshBuilder.Instance != null && DynamicNavMeshBuilder.Instance.IsNavMeshReady)
            {
                InitializeNavMeshAgent();
                yield break;
            }

            yield return new WaitForSeconds(0.5f);
        }

        // Maksimum deneme sayısına ulaşıldı, yine de başlatmaya çalış
        Debug.LogWarning($"BasicEnemy: NavMesh hazır olmasa da NavMeshAgent başlatılmaya çalışılıyor ({gameObject.name})");
        InitializeNavMeshAgent();
    }

    private void InitializeNavMeshAgent()
    {
        // NavMesh hazır olduğunda NavMeshAgent'ı etkinleştir
        if (navMeshAgent != null)
        {
            try
            {
                navMeshAgent.enabled = true;
                navMeshAgent.speed = moveSpeed;
                navMeshInitialized = true;
                Debug.Log($"BasicEnemy: NavMeshAgent başarıyla başlatıldı ({gameObject.name})");

                // NavMeshAgent başarıyla başlatıldıktan sonra devriye davranışını başlat
                if (enablePatrol && patrolCoroutine == null)
                {
                    patrolCoroutine = StartCoroutine(PatrolBehavior());
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"NavMeshAgent başlatılamadı: {e.Message}");
                navMeshInitialized = false;
            }
        }
    }

    private void Update()
    {
        // Server olmayan client'lerde hareket kontrollerini yapma
        // Client'lerde düşmanlar server tarafından kontrol ediliyor
        if (isDead) return;

        // Hareket hızını kontrol ederek animasyonları ayarla
        actualSpeed = new Vector2(navMeshAgent.velocity.x, navMeshAgent.velocity.z).magnitude;
        if (actualSpeed > movementThreshold)
        {
            SetWalkingAnimation();
        }
        else
        {
            SetIdleAnimation();
        }

        if (!IsServer) return;

        if (targetPlayer != null)
        {
            // Oyuncu algılandığında devriye davranışını durdur
            if (patrolCoroutine != null && isPatrolling)
            {
                isPatrolling = false;
                isWaitingAtPatrolPoint = false;
            }

            // NavMeshAgent başarıyla başlatıldıysa kullan
            if (navMeshAgent != null && navMeshAgent.enabled && navMeshInitialized)
            {
                try
                {
                    navMeshAgent.SetDestination(targetPlayer.position);
                }
                catch (Exception e)
                {
                    // Hata durumunda alternatif hareket kullan
                    Debug.LogWarning($"NavMeshAgent hatası: {e.Message}");
                    MoveWithoutNavMesh();
                }
            }
            // NavMeshAgent yoksa veya başlatılamadıysa basit hareket kullan
            else
            {
                MoveWithoutNavMesh();
            }
        }
        else if (enablePatrol && navMeshInitialized && navMeshAgent != null && navMeshAgent.enabled)
        {
            // Oyuncu yoksa ve devriye davranışı etkinse
            if (patrolCoroutine == null)
            {
                patrolCoroutine = StartCoroutine(PatrolBehavior());
            }
        }
    }

    // Animasyon kontrollerini basitleştirmek için yardımcı metodlar
    private void SetIdleAnimation()
    {
        if (animator != null)
        {
            animator.SetBool(isIdleParam, true);
            animator.SetBool(isWalkingParam, false);
            animator.SetBool(isPunchingParam, false);
        }
    }

    private void SetWalkingAnimation()
    {
        if (animator != null)
        {
            animator.SetBool(isIdleParam, false);
            animator.SetBool(isWalkingParam, true);
            animator.SetBool(isPunchingParam, false);
        }
    }

    private void SetPunchAnimation()
    {
        if (animator != null)
        {
            animator.SetBool(isIdleParam, false);
            animator.SetBool(isWalkingParam, false);
            animator.SetBool(isPunchingParam, true);
        }
    }

    private void SetHitAnimation()
    {
        if (animator != null)
        {
            animator.SetBool(isGettingHitParam, true);

            // Hit animasyonu genellikle kısa süreli olduğu için bir coroutine ile reset edelim
            StartCoroutine(ResetHitAnimation());
        }
    }

    private IEnumerator ResetHitAnimation()
    {
        yield return new WaitForSeconds(0.5f); // Hit animasyon süresine göre ayarlayın

        if (animator != null)
        {
            animator.SetBool(isGettingHitParam, false);
        }
    }

    private void MoveWithoutNavMesh()
    {
        if (targetPlayer == null) return;

        // NavMesh olmadan basit hareket
        Vector3 direction = (targetPlayer.position - transform.position).normalized;
        direction.y = 0; // Y ekseninde hareket etmesini engelle
        transform.position += direction * moveSpeed * Time.deltaTime;

        // Hareket ediyor, yürüme animasyonunu ayarla
        SetWalkingAnimation();

        // Hedefe doğru dön
        if (enemyModel != null)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            enemyModel.rotation = Quaternion.Slerp(enemyModel.rotation, lookRotation, Time.deltaTime * 5f);
        }
    }

    private void FindNearestPlayer()
    {
        // Tüm oyuncu objelerini bul (Player tag'i ile işaretlenmiş olmalılar)
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        float closestDistance = float.MaxValue;
        Transform closestPlayer = null;

        foreach (GameObject player in players)
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance < closestDistance && distance < detectionRadius)
            {
                closestDistance = distance;
                closestPlayer = player.transform;
            }
        }

        targetPlayer = closestPlayer;
    }

    // Yeni eklenen devriye davranışı
    private IEnumerator PatrolBehavior()
    {
        isPatrolling = true;

        while (isPatrolling && navMeshAgent != null && navMeshAgent.enabled && !isDead)
        {
            // Eğer hedef oyuncu yoksa devriye gez
            if (targetPlayer == null)
            {
                // Henüz bir devriye noktasına gitmiyorsa veya hedefine ulaştıysa
                if (!navMeshAgent.hasPath || navMeshAgent.remainingDistance <= patrolPointDistance)
                {
                    if (!isWaitingAtPatrolPoint)
                    {
                        // Bir noktada bekle
                        isWaitingAtPatrolPoint = true;
                        float waitTime = UnityEngine.Random.Range(minPatrolWaitTime, maxPatrolWaitTime);

                        // Beklerken idle animasyonu
                        SetIdleAnimation();

                        yield return new WaitForSeconds(waitTime);
                        isWaitingAtPatrolPoint = false;
                    }

                    // Yeni bir devriye noktası belirle
                    Vector3 randomDirection = UnityEngine.Random.insideUnitSphere * patrolRadius;
                    randomDirection.y = 0;
                    Vector3 randomPoint = spawnPosition + randomDirection;

                    // NavMesh üzerinde erişilebilir bir nokta bul
                    NavMeshHit hit;
                    if (NavMesh.SamplePosition(randomPoint, out hit, patrolRadius, NavMesh.AllAreas))
                    {
                        currentPatrolTarget = hit.position;
                        navMeshAgent.SetDestination(currentPatrolTarget);
                        Debug.Log("Yeni devriye noktası belirlendi:"); //Debug.Log($"Yeni devriye noktası belirlendi: {currentPatrolTarget}");

                        // Hareket başlıyor, yürüme animasyonu
                        SetWalkingAnimation();
                    }
                }

                // Gerçek hareket hızını sürekli kontrol et
                float actualSpeed = new Vector2(navMeshAgent.velocity.x, navMeshAgent.velocity.z).magnitude;
                if (actualSpeed <= movementThreshold && !isWaitingAtPatrolPoint)
                {
                    SetIdleAnimation();
                }
            }
            else
            {
                // Oyuncu algılandığında devriyeyi durdur
                isPatrolling = false;
                yield break;
            }

            yield return new WaitForSeconds(0.1f);
        }
    }

    public bool TakeDamage(int damageAmount)
    {
        if (isDead) return false;

        health -= damageAmount;

        // Hasar aldığında hit animasyonunu oynat
        SetHitAnimation();

        if (health <= 0)
        {
            isDead = true;
            OnDefeated();
            return true;
        }

        return false;
    }

    public override void OnDefeated()
    {
        // Düşman yenildi
        if (!IsServer) return;

        // Spawner'a haber vermek için olayı tetikle
        OnEnemyDefeated?.Invoke(this);

        // Devriye davranışını durdur
        isPatrolling = false;
        if (patrolCoroutine != null)
        {
            StopCoroutine(patrolCoroutine);
            patrolCoroutine = null;
        }

        // Hareket ve görselliği devre dışı bırak
        if (navMeshAgent != null && navMeshAgent.enabled)
        {
            navMeshAgent.isStopped = true;
            navMeshAgent.enabled = false;
        }

        SetVisible(false);
    }

    public void Reset(Vector3 newPosition)
    {
        // Düşman durumunu sıfırla
        isDead = false;
        health = 100; // Tam sağlığa reset

        // Pozisyonu sıfırla
        transform.position = newPosition;
        spawnPosition = newPosition; // Yeni spawn pozisyonunu kaydet

        // Görselleri ve hareketi aktifleştir
        SetVisible(true);

        // Animasyon durumunu sıfırla
        SetIdleAnimation();

        // NavMeshAgent'ı başlatma
        if (navMeshAgent != null && !navMeshAgent.enabled && navMeshInitialized)
        {
            navMeshAgent.enabled = true;
            navMeshAgent.isStopped = false;

            // Devriye davranışını yeniden başlat
            if (enablePatrol && patrolCoroutine == null)
            {
                patrolCoroutine = StartCoroutine(PatrolBehavior());
            }
        }
        else if (!navMeshInitialized)
        {
            // NavMeshAgent zaten başlatılmamışsa, başlatmayı dene
            if (DynamicNavMeshBuilder.Instance != null && DynamicNavMeshBuilder.Instance.IsNavMeshReady)
            {
                InitializeNavMeshAgent();
            }
        }
    }
}