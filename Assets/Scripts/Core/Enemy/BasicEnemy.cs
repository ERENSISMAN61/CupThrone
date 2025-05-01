using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class BasicEnemy : Enemy
{
    [SerializeField] private float moveSpeed = 3.0f; // Düşmanın hareket hızı
    [SerializeField] private float detectionRadius = 10.0f; // Düşmanın oyuncuyu algılama yarıçapı
    [SerializeField] private Transform enemyModel; // Düşmanın 3D modeli

    private NavMeshAgent navMeshAgent; // Düşmanın hareket bileşeni
    private Transform targetPlayer; // Hedef oyuncu transformu
    private bool navMeshInitialized = false; // NavMesh başlatıldı mı
    private Coroutine initNavMeshCoroutine;

    public event Action<BasicEnemy> OnEnemyDefeated; // Düşman yenildiğinde tetiklenen olay

    private void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        if (navMeshAgent != null)
        {
            // NavMesh hazır olana kadar devre dışı bırak
            navMeshAgent.enabled = false;
        }
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

        if (!IsServer) return;

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
        InvokeRepeating(nameof(FindNearestPlayer), 1.0f, 1.0f);
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
        if (!IsServer) return;

        if (targetPlayer != null && !isDead)
        {
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
    }

    private void MoveWithoutNavMesh()
    {
        if (targetPlayer == null) return;

        // NavMesh olmadan basit hareket
        Vector3 direction = (targetPlayer.position - transform.position).normalized;
        direction.y = 0; // Y ekseninde hareket etmesini engelle
        transform.position += direction * moveSpeed * Time.deltaTime;

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

    public override void OnDefeated()
    {
        // Düşman yenildi
        if (!IsServer) return;

        // Spawner'a haber vermek için olayı tetikle
        OnEnemyDefeated?.Invoke(this);

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

        // Görselleri ve hareketi aktifleştir
        SetVisible(true);

        // NavMeshAgent'ı başlatma
        if (navMeshAgent != null && !navMeshAgent.enabled && navMeshInitialized)
        {
            navMeshAgent.enabled = true;
            navMeshAgent.isStopped = false;
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