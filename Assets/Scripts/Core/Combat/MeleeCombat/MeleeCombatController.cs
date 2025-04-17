using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class MeleeCombatController : MonoBehaviour
{
    Animator animator;
    AudioSource audioSource;



    Vector3 _PlayerVelocity;



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
            //            Debug.LogWarning("Camera found: " + cam.name);
        }
    }
    void Update()
    {
        // Repeat Inputs
        if (Input.GetKeyDown(KeyCode.Mouse0))
        { Attack(); }

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
    public LayerMask attackLayer;

    public GameObject hitEffect;
    public AudioClip swordSwing;
    public AudioClip hitSound;

    bool attacking = false;
    bool readyToAttack = true;
    int attackCount;

    public void Attack()
    {
        if (!readyToAttack || attacking) return;

        readyToAttack = false;
        attacking = true;

        Invoke(nameof(ResetAttack), attackSpeed);
        Invoke(nameof(AttackRaycast), attackDelay);

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

    void ResetAttack()
    {
        attacking = false;
        readyToAttack = true;
    }

    void AttackRaycast()
    {
        if (Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, attackDistance, attackLayer))
        {
            HitTarget(hit.point);
        }
    }

    void HitTarget(Vector3 pos)
    {
        audioSource.pitch = 1;
        audioSource.PlayOneShot(hitSound);

        GameObject GO = Instantiate(hitEffect, pos, Quaternion.identity);
        Destroy(GO, 20);
    }
}