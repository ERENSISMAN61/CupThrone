using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode; // Change to Unity Netcode namespace

// Change to use Unity's Netcode NetworkBehaviour
public class MeleeCombatController : NetworkBehaviour
{
    Animator animator;
    AudioSource audioSource;

    [Header("Camera")]
    public Camera cam;

    void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        audioSource = GetComponent<AudioSource>();

        // Cursor.lockState = CursorLockMode.Locked;
        // Cursor.visible = false;
    }

    void Start()
    {
        TryFindCamera();

        // Her frame kamerayı kontrol et (eğer kamera yoksa veya değişirse)
        StartCoroutine(CheckCameraRoutine());
    }

    private IEnumerator CheckCameraRoutine()
    {
        while (true)
        {
            // Eğer kamera yok veya yok edilmişse, tekrar bulmayı dene
            if (cam == null)
            {
                TryFindCamera();
            }
            yield return new WaitForSeconds(1f); // Her saniye kontrol et
        }
    }

    private void TryFindCamera()
    {
        cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("No main camera found. MeleeCombat will not work correctly.");
        }
        else
        {
            // Debug.LogWarning("Camera found: " + cam.name);
        }
    }
    void Update()
    {

        // Only trigger attack once per click, not continuously
        if (Input.GetKey(KeyCode.Mouse0))
        {
            Attack();
        }

        SetAnimations();
    }

    // ---------- //
    // ANIMATIONS //
    // ---------- //

    public const string IDLE = "Idle";
    public const string WALK = "Walk";
    public const string RUN = "Run";
    public const string ATTACK1 = "Attack 1";
    public const string ATTACK2 = "Attack 2";

    string currentAnimationState;

    public void ChangeAnimationState(string newState)
    {
        // STOP THE SAME ANIMATION FROM INTERRUPTING WITH ITSELF //
        if (currentAnimationState == newState) return;

        // PLAY THE ANIMATION //
        currentAnimationState = newState;
        animator.CrossFadeInFixedTime(currentAnimationState, 0.2f);
    }

    void SetAnimations()
    {
        // If player is not attacking
        if (!attacking)
        {
            if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.W))
            { ChangeAnimationState(IDLE); }
            else if (Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.W))
            { ChangeAnimationState(RUN); }
            else if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D))
            { ChangeAnimationState(WALK); }
            else
            {
                ChangeAnimationState(IDLE);
            }

        }
    }

    // ------------------- //
    // ATTACKING BEHAVIOUR //
    // ------------------- //

    [Header("Attacking")]
    public float attackDistance = 3f;
    public float attackDelay = 0.4f;
    public float attackSpeed = 1f;
    public int meleeDamage = 25; // Add damage amount for melee attacks
    public LayerMask attackLayer;
    public GameObject hitEffect;
    public AudioClip swordSwing;
    public AudioClip hitSound;

    bool attacking = false;
    bool readyToAttack = true;
    int attackCount;

    //[SerializeField] private Collider playerCollider;

    public void Attack()
    {

        if (!readyToAttack || attacking) return;

        readyToAttack = false;
        attacking = true;

        // Delay the interactable attack to match animation timing
        Invoke(nameof(ResetAttack), attackSpeed);

        Invoke(nameof(AttackInteractable), attackDelay);

        audioSource.pitch = Random.Range(0.9f, 1.1f);
        audioSource.PlayOneShot(swordSwing);

        if (attackCount == 0)
        {

            ChangeAnimationState(ATTACK1);
            attackCount++;
        }
        else
        {

            ChangeAnimationState(ATTACK2);
            attackCount = 0;
        }
    }


    // ETKİLEŞİME GEÇİLEBİLEN OBJELERE HASAR VERMEK İÇİN YAZILAN ATTACK,
    // INTERACT() ETKİLEŞİME GEÇİLECEK OLAN OBJE VE O OBJENİN HEALTH SCRIPTİNİN 
    // İÇİNDEKİ INTERACT FONKSİYONUNDA VERİLEN HASAR DEĞERİ AYARLANIYOR (ÖR. TREEHEALTH.CS'YE BAK)
    // ÖR. AĞACA HASAR VERİLECEĞİ ZAMAN AĞAÇ PREFABINDE HEALTH SCRIPTI, ONUN DA İÇİNDE INTERACT FONKSİYONU YAZILIYOR
    void AttackInteractable()
    {
        if (!IsOwner) return;
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit[] hits = Physics.RaycastAll(ray, attackDistance, attackLayer);

        // Tüm hit'leri mesafeye göre sırala (en yakından en uzağa)
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            // Skip if we hit ourselves
            if (hit.collider.transform.IsChildOf(transform) || hit.collider.transform == transform)
            {
                continue;
            }

            // Check if we hit another player with a Health component
            Health targetHealth = hit.collider.GetComponentInParent<Health>();
            if (targetHealth != null && targetHealth.NetworkObject != null)
            {
                // Make sure we're not hitting ourselves
                if (targetHealth.NetworkObject.OwnerClientId != OwnerClientId)
                {
                    // Call the ServerRpc to apply damage
                    DealDamageServerRpc(targetHealth.NetworkObject.NetworkObjectId, meleeDamage);

                    // Show hit effect
                    HitTarget(hit.point);
                    return; // Exit after handling player damage
                }
            }

            // Handle other interactable objects
            if (hit.collider.TryGetComponent(out IInteractable interactable))
            {
                interactable.Interact();
                HitTarget(hit.point);
                return; // Exit after handling interaction
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void DealDamageServerRpc(ulong targetNetworkId, int damageAmount)
    {
        // Find the target NetworkObject by its ID
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetworkId, out NetworkObject targetObject))
        {
            // Get the Health component from the target
            if (targetObject.TryGetComponent<Health>(out Health targetHealth))
            {
                // Apply damage to the target
                targetHealth.TakeDamage(damageAmount);

                // Log the damage (optional)
                Debug.Log($"Player {OwnerClientId} dealt {damageAmount} damage to player {targetObject.OwnerClientId}");
            }
        }
    }

    void ResetAttack()
    {
        attacking = false;
        readyToAttack = true;
    }

    void HitTarget(Vector3 pos)
    {
        audioSource.pitch = 1;
        audioSource.PlayOneShot(hitSound);

        GameObject GO = Instantiate(hitEffect, pos, Quaternion.identity);
        Destroy(GO, 20);
    }
}