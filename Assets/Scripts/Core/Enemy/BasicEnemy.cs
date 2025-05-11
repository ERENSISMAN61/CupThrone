using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class BasicEnemy : Enemy, IInteractable
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

    [SerializeField] private int attackDamage = 10; // Enemy'nin saldırı hasarı
    [SerializeField] private float attackRange = 2.0f; // Enemy'nin saldırı menzili
    [SerializeField] private float attackCooldown = 1.5f; // Saldırı bekleme süresi

    [SerializeField] private int maxHealth = 100; // Maximum health of the enemy
    [SerializeField] private NetworkVariable<int> currentHealth = new NetworkVariable<int>(100); // Networked health variable
    [SerializeField] private Collider enemyCollider; // Enemy'nin collider bileşeni
    [SerializeField] private float damageDelay = 0.5f; // Hasar verme gecikmesi
    [SerializeField] private float attackthresholdOffset = 0.1f;

    private Dictionary<ulong, float> clientDamageTimestamps = new Dictionary<ulong, float>();
    private float damageTimeout = 0.5f; // Cooldown in seconds

    private float lastAttackTime = 0f; // Son saldırı zamanı

    private NavMeshAgent navMeshAgent; // Düşmanın hareket bileşeni
    private Transform targetPlayer; // Hedef oyuncu transformu
    private bool navMeshInitialized = false; // NavMesh başlatıldı mı
    private Coroutine initNavMeshCoroutine;
    private Coroutine patrolCoroutine;
    private Vector3 spawnPosition; // Düşmanın başlangıç pozisyonu
    private Vector3 currentPatrolTarget; // Şu anki devriye hedefi
    private bool isWaitingAtPatrolPoint = false; // Devriye noktasında bekliyor mu
    private bool isPatrolling = false; // Devriye geziyor mu

    private float timeSinceLastChase = 0f; // Son takipten bu yana geçen süre
    [SerializeField] private float chaseTimeout = 2f; // Takipten sonra devriyeye dönme süresi

    // Animator parametreleri
    private readonly int isIdleParam = Animator.StringToHash("isIdle");
    private readonly int isWalkingParam = Animator.StringToHash("isWalking");
    private readonly int isGettingHitParam = Animator.StringToHash("isGettingHit");
    private readonly int isPunchingParam = Animator.StringToHash("isPunching");

    public event Action<BasicEnemy> OnEnemyDefeated; // Düşman yenildiğinde tetiklenen olay

    private enum EnemyState
    {
        Idle,
        Patrolling,
        Chasing,
        Attacking
    }

    private EnemyState currentState = EnemyState.Idle;
    private bool isGettingHit = false; // Düşmanın hasar alıp almadığını takip etmek için

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

        // Initialize current health
        currentHealth.Value = maxHealth;
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
            //Debug.Log($"BasicEnemy: NavMesh hazır bildirimi alındı, NavMeshAgent başlatılıyor ({gameObject.name})");
            InitializeNavMeshAgent();
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Subscribe to health changes

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
        //Debug.Log($"BasicEnemy: NavMesh'in hazır olması bekleniyor ({gameObject.name})");

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
        //Debug.LogWarning($"BasicEnemy: NavMesh hazır olmasa da NavMeshAgent başlatılmaya çalışılıyor ({gameObject.name})");
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
                //Debug.Log($"BasicEnemy: NavMeshAgent başarıyla başlatıldı ({gameObject.name})");

                // NavMeshAgent başarıyla başlatıldıktan sonra devriye davranışını başlat
                if (enablePatrol && patrolCoroutine == null)
                {
                    //Debug.Log($"BasicEnemy: Devriye davranışı başlatılıyor ({gameObject.name})");
                    patrolCoroutine = StartCoroutine(PatrolBehavior());
                    currentState = EnemyState.Patrolling; // Devriye davranışı başlatıldığında durumu güncelle
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
        //Debug.Log("curr State:" + currentState);
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
            if (animator != null
            && animator.GetBool(isPunchingParam) == false && animator.GetBool(isGettingHitParam) == false)
                SetIdleAnimation();
        }

        if (!IsServer) return;

        // Ensure the enemy always faces the player
        if (targetPlayer != null && currentState != EnemyState.Patrolling)
        {
            Vector3 directionToPlayer = (targetPlayer.position - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(new Vector3(directionToPlayer.x, 0, directionToPlayer.z));
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }

        switch (currentState)
        {
            case EnemyState.Idle:
                HandleIdleState();
                break;
            case EnemyState.Patrolling:
                HandlePatrollingState();
                break;
            case EnemyState.Chasing:
                HandleChasingState();
                break;
            case EnemyState.Attacking:
                HandleAttackingState();
                break;
        }
    }

    private void HandleIdleState()
    {

        if (targetPlayer == null)
        {

            FindNearestPlayer();
        }

        if (targetPlayer != null)
        {

            currentState = EnemyState.Chasing;
        }
        else if (enablePatrol && patrolCoroutine == null)
        {

            currentState = EnemyState.Patrolling;
        }
    }

    private void HandlePatrollingState()
    {
        if (patrolCoroutine == null)
        {
            patrolCoroutine = StartCoroutine(PatrolBehavior());
        }

        if (targetPlayer != null)
        {
            if (patrolCoroutine != null)
            {
                StopCoroutine(patrolCoroutine);
                patrolCoroutine = null;
            }

            currentState = EnemyState.Chasing;
        }
    }

    private void HandleChasingState()
    {
        if (targetPlayer == null || !targetPlayer.gameObject.activeInHierarchy)
        {
            timeSinceLastChase += Time.deltaTime;

            if (timeSinceLastChase >= chaseTimeout)
            {
                currentState = EnemyState.Patrolling;
            }
        }
        else
        {
            timeSinceLastChase = 0f;
            ChasePlayer();

            float distanceToPlayer = Vector3.Distance(transform.position, targetPlayer.position);

            //Debug.Log("distanceToPlayer: " + (distanceToPlayer - 0.1f) + " attackRange: " + attackRange);

            if (distanceToPlayer - attackthresholdOffset <= attackRange)
            {
                currentState = EnemyState.Attacking;
            }
        }
    }

    private void HandleAttackingState()
    {
        if (currentHealth.Value == 0) return;

        // Düşman hasar alıyorsa saldırı yapmasın
        if (isGettingHit) return;

        if (targetPlayer == null || !targetPlayer.gameObject.activeInHierarchy)
        {
            currentState = EnemyState.Idle;
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, targetPlayer.position);
        if (distanceToPlayer - attackthresholdOffset > attackRange)
        {
            currentState = EnemyState.Chasing;
        }
        else if (Time.time >= lastAttackTime + attackCooldown)
        {
            // Ensure the enemy faces the player before attacking
            Vector3 directionToPlayer = (targetPlayer.position - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(new Vector3(directionToPlayer.x, 0, directionToPlayer.z));
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);

            AttackPlayer();
            lastAttackTime = Time.time;
        }
    }

    private void AttackPlayer()
    {
        // Yumruk animasyonunu başlat, hasar Animation Event ile verilecek
        SetPunchAnimation();

        // Not: GiveDamageToPlayer artık Animation Event tarafından çağrılacak
        // Eğer Unity Editor'de Animation Event eklemediyseniz,
        // burada bir güvenlik önlemi olarak bir süre sonra
        // animasyonu sıfırlamak için bir Invoke ekleyebiliriz
        //Invoke(nameof(OnPunchAnimationEnd), 1.0f);
    }

    private void GiveDamageToPlayer()
    {
        if (!IsServer) return;

        if (targetPlayer != null && targetPlayer.parent.TryGetComponent<Health>(out Health playerHealth))
        {
            // Check if the punching animation is active
            // if (animator != null && animator.GetBool(isPunchingParam))
            // {
            playerHealth.TakeDamage(attackDamage);
            // }
            // else
            // {
            //     Debug.LogWarning("[GiveDamageToPlayer] Damage not applied because punching animation is not active.");
            // }
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
        //Debug.Log("Attacking SetPunchAnimation çağrıldı");
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
            isGettingHit = true;
            animator.SetBool(isIdleParam, false);
            animator.SetBool(isGettingHitParam, true);

            // Hit animasyonu genellikle kısa süreli olduğu için bir coroutine ile reset edelim
            // StartCoroutine(ResetHitAnimation());
        }
    }

    // private IEnumerator ResetHitAnimation()
    // {
    //     yield return new WaitForSeconds(0.5f); // Hit animasyon süresine göre ayarlayın

    //     if (animator != null)
    //     {
    //         isGettingHit = false;
    //         animator.SetBool(isGettingHitParam, false);
    //     }
    // }


    private void FindNearestPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        float closestDistance = float.MaxValue;
        Transform closestPlayer = null;

        foreach (GameObject player in players)
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance < closestDistance && distance <= detectionRadius)
            {
                closestDistance = distance;
                closestPlayer = player.transform;
            }
        }

        targetPlayer = closestPlayer;
    }

    private void ChasePlayer()
    {
        if (targetPlayer != null && navMeshAgent != null && navMeshAgent.enabled)
        {
            // Adjust destination to maintain attack range
            Vector3 directionToPlayer = (targetPlayer.position - transform.position).normalized;
            Vector3 adjustedPosition = targetPlayer.position - directionToPlayer * attackRange;

            // Log NavMeshAgent properties for debugging
            //Debug.Log($"[ChasePlayer] isStopped: {navMeshAgent.isStopped}, velocity: {navMeshAgent.velocity}, remainingDistance: {navMeshAgent.remainingDistance}, pathPending: {navMeshAgent.pathPending}");

            // Check if the adjusted position is on the NavMesh
            NavMeshHit hit;
            if (NavMesh.SamplePosition(adjustedPosition, out hit, 1.0f, NavMesh.AllAreas))
            {
                navMeshAgent.SetDestination(hit.position);
            }
            else
            {
                //Debug.LogWarning($"[ChasePlayer] Adjusted position is not on the NavMesh: {adjustedPosition}");
            }

            // Smoothly rotate towards the player while chasing
            Quaternion lookRotation = Quaternion.LookRotation(new Vector3(directionToPlayer.x, 0, directionToPlayer.z));
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }
        else
        {
            //Debug.LogWarning("[ChasePlayer] Target player or NavMeshAgent is null/disabled.");
        }
    }

    // Yeni eklenen devriye davranışı
    private IEnumerator PatrolBehavior()
    {
        isPatrolling = true;
        while (isPatrolling && navMeshAgent != null && navMeshAgent.enabled && !isDead)
        {
            if (!navMeshAgent.isOnNavMesh)
            {
                Debug.LogError($"NavMeshAgent is not on NavMesh during patrol for {gameObject.name}. Exiting patrol.");
                yield break;
            }

            // Eğer hedef oyuncu yoksa devriye gez
            if (targetPlayer == null)
            {
                // Henüz bir devriye noktasına gitmiyorsa veya hedefine ulaştıysa
                if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance <= patrolPointDistance)
                {
                    if (!isWaitingAtPatrolPoint)
                    {
                        isWaitingAtPatrolPoint = true;

                        // Hedefe ulaşıldığında hareketi durdur
                        navMeshAgent.isStopped = true;

                        // 2 saniye Idle state'e geç
                        currentState = EnemyState.Idle;
                        yield return new WaitForSeconds(2f);

                        // 2 saniye sonra tekrar Patrolling state'e geç
                        currentState = EnemyState.Patrolling;
                        isWaitingAtPatrolPoint = false;

                        // Yeni bir devriye noktası belirle
                        Vector3 randomDirection = UnityEngine.Random.insideUnitSphere * patrolRadius;
                        randomDirection.y = 0;
                        Vector3 randomPoint = spawnPosition + randomDirection;

                        NavMeshHit hit;
                        if (NavMesh.SamplePosition(randomPoint, out hit, patrolRadius, NavMesh.AllAreas))
                        {
                            currentPatrolTarget = hit.position;
                            navMeshAgent.SetDestination(currentPatrolTarget);
                        }

                        // Hareketi yeniden başlat
                        navMeshAgent.isStopped = false;
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

    // public new bool TakeDamage(int damageAmount)
    // {
    //     if (isDead) return false;

    //     if (IsServer)
    //     {
    //         currentHealth.Value -= damageAmount;

    //         // Play hit animation
    //         SetHitAnimation();

    //         if (currentHealth.Value <= 0)
    //         {
    //             Debug.LogError("TakeDamage");
    //             isDead = true;
    //             OnDefeated();
    //             return true;
    //         }
    //     }
    //     else
    //     {
    //         TakeDamageServerRpc(damageAmount, NetworkManager.Singleton.LocalClientId);
    //     }

    //     return false;
    // }

    [ServerRpc(RequireOwnership = false)]
    private void TakeDamageServerRpc(int damage, ulong clientId)
    {
        ApplyDamage_Internal(damage, clientId);
    }

    private void ApplyDamage_Internal(int damage, ulong clientId)
    {
        if (!IsServer)
        {
            //Debug.LogWarning("ApplyDamage called on client! This shouldn't happen.");
            return;
        }

        if (clientDamageTimestamps.TryGetValue(clientId, out float lastDamageTime))
        {
            if (Time.time - lastDamageTime < damageTimeout)
            {
                //Debug.Log($"Damage cooldown active for client {clientId}. Ignoring this hit.");
                return;
            }
        }

        clientDamageTimestamps[clientId] = Time.time;

        //Debug.Log($"BEFORE: BasicEnemy health: {currentHealth.Value}");
        currentHealth.Value -= damage;
        //Debug.Log($"AFTER: BasicEnemy took {damage} damage from client {clientId}. Remaining health: {currentHealth.Value}");

        if (animator.GetBool(isPunchingParam) == false) SetHitAnimation();



        if (currentHealth.Value <= 0)
        {
            //Debug.LogError("ApplyDamage_Internal");
            OnDefeated();
        }
    }

    public override void OnDefeated()
    {
        // Enemy defeated
        if (!IsServer) return;
        // Trigger defeated event

        enemyCollider.enabled = false; // Disable collider
        targetPlayer = null; // Clear target player
        // Stop patrol behavior
        isPatrolling = false;
        if (patrolCoroutine != null)
        {
            StopCoroutine(patrolCoroutine);
            patrolCoroutine = null;
        }

        // Disable movement and visuals
        if (navMeshAgent != null && navMeshAgent.enabled)
        {
            //Debug.LogError("navmesh kapatıldı");
            navMeshAgent.isStopped = true;
            navMeshAgent.enabled = false;
        }

        SetVisible(false);
        // Respawn enemy at a new location
        OnEnemyDefeated?.Invoke(this);

    }


    public void Reset(Vector3 newPosition)
    {
        targetPlayer = null; // Clear target player
        //Debug.LogError("BasicEnemy Reset çağrıldı");
        // Reset enemy state
        isDead = false;
        currentHealth.Value = maxHealth; // Reset to full hea

        transform.position = newPosition;
        spawnPosition = newPosition; // Update spawn position
        // Enable visuals and movement
        SetVisible(true);
        enemyCollider.enabled = true; // Enable collider
        // Reset animation state
        SetIdleAnimation();



        navMeshAgent.enabled = true;
        navMeshAgent.isStopped = false;

        // Restart patrol behavior
        if (enablePatrol && patrolCoroutine == null)
        {
            patrolCoroutine = StartCoroutine(PatrolBehavior());
        }


    }

    public void Interact()
    {
        //Debug.Log($"BasicEnemy Interact called. IsServer: {IsServer}, IsClient: {IsClient}, IsHost: {IsHost}");

        // If we're on a dedicated client, we should call the ServerRpc
        if (IsClient && !IsHost)
        {
            //Debug.Log("CLIENT MODE: Sending damage request to server");
            TakeDamageServerRpc(25, NetworkManager.Singleton.LocalClientId);
        }
        // If we're on the server (or host), apply damage directly
        else if (IsServer)
        {
            //Debug.Log("SERVER MODE: Applying damage directly");
            // For server, use server's client ID (usually 0)
            ApplyDamage_Internal(25, NetworkManager.Singleton.LocalClientId);
        }
    }

    // Animation Event'ler için kullanılacak metodlar
    public void OnPunchAnimationEnd()
    {
        // Bu metod animasyonda bir Animation Event olarak çağrılmalı
        if (animator != null)
        {
            // Yumruk animasyonunu sonlandır
            animator.SetBool(isPunchingParam, false);

            // Mevcut duruma göre uygun animasyona geç
            if (currentState == EnemyState.Chasing ||
                (navMeshAgent != null && navMeshAgent.velocity.sqrMagnitude > movementThreshold * movementThreshold))
            {
                SetWalkingAnimation();
            }
            else
            {
                SetIdleAnimation();
            }
        }
    }

    // Saldırı sırasında hasar vermek için Animation Event'ten çağrılacak metod
    public void OnAttackAnimationHit()
    {
        // Bu metod, animasyonda tam yumruk vuruş anında bir Animation Event olarak çağrılmalı
        GiveDamageToPlayer();
    }

    // Get Hit animasyonu bittiğinde çağrılacak metod
    public void OnHitAnimationEnd()
    {
        // Bu metod, animasyonda hasar alma animasyonu bittiğinde bir Animation Event olarak çağrılmalı
        isGettingHit = false;
        animator.SetBool(isGettingHitParam, false);
    }

}