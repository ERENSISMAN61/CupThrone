using Unity.Netcode;
using UnityEngine;

public class PlayerMovementNew : NetworkBehaviour
{
    [SerializeField] private InputReader inputReader;
    [SerializeField] private Transform bodyTransform;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Animator animator; // Animator bileşeni

    [SerializeField] private float groundDrag; // Zemin kontrolü için transform
    [SerializeField] private float movementSpeed;
    [SerializeField] private float sprintSpeedMultiplier = 1.5f; // Sprint hız çarpanı
    [SerializeField] private float turnSpeed = 50f;
    [SerializeField] private float jumpForce; // Zıplama kuvveti
    [SerializeField] private LayerMask groundLayer; // Zemin kontrolü için layer

    [SerializeField] private float jumpCooldown; // Zıplama süresi
    [SerializeField] private float airMultiplier; // Hava kontrolü için çarpan

    private bool readyToJump = true; // Zıplama hazır mı değil mi

    private Vector2 movementInput;
    private bool isGrounded;
    private bool isJumping = false; // Track if player is in jump state
    private bool isSprinting = false; // Track if player is sprinting

    // NetworkVariables for animation parameters
    private NetworkVariable<float> networkSpeed = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<float> networkDirection = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<bool> networkIsJumping = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<bool> networkIsSprinting = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            inputReader.MoveEvent += HandleMove;
            inputReader.JumpEvent += HandleJump; // Zıplama olayını dinle
            inputReader.SprintEvent += HandleSprint; // Sprint olayını dinle
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        {
            inputReader.MoveEvent -= HandleMove;
            inputReader.JumpEvent -= HandleJump; // Zıplama olayını kaldır
            inputReader.SprintEvent -= HandleSprint; // Sprint olayını kaldır
        }
    }

    private void HandleMove(Vector2 movement)
    {
        movementInput = movement;
    }

    private void HandleJump()
    {
        if (readyToJump && isGrounded)
        {
            isJumping = true;
            networkIsJumping.Value = true;           
            Debug.Log("Jumping - setting IsJumping to true");

            Jump(); // Zıplama fonksiyonunu çağır                        
            Debug.Log("Jump function called");

            readyToJump = false; // Zıplama hazır değil
            Invoke(nameof(ResetJump), jumpCooldown); // Zıplama süresini sıfırla
        }
    }

    private void HandleSprint(bool sprintState)
    {
        isSprinting = sprintState;
        networkIsSprinting.Value = sprintState;
    }

    private void FixedUpdate()
    {
        // Only process movement for the owner
        if (!IsOwner) { return; }

        // Check if grounded
        
        isGrounded = Physics.Raycast(bodyTransform.position, Vector3.down, 0.2f, groundLayer);

        if (isGrounded)
        {
            Debug.Log("Player is grounded - applying ground drag");
            rb.linearDamping = groundDrag;

        }
        else
        {
            rb.linearDamping = 0;
        }

        MovePlayer();

        // Rotate character based on horizontal input
        if (movementInput.x != 0)
        {
            float yRotation = movementInput.x * turnSpeed * Time.fixedDeltaTime;
            bodyTransform.Rotate(0, yRotation, 0);
        }

        SpeedControl();

        // Calculate forward velocity component for animation
        Vector3 horizontalVel = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        
        // Project velocity onto forward direction to get only forward/backward component
        float forwardSpeed = Vector3.Dot(horizontalVel, bodyTransform.forward);
        
        // Update network variables for animation
        networkSpeed.Value = forwardSpeed;
        networkDirection.Value = movementInput.x;

        // Update animator if present
        if (animator != null)
        {
            animator.SetFloat("Speed", forwardSpeed);
            animator.SetFloat("Direction", movementInput.x);
            animator.SetBool("IsGrounded", isGrounded);
            animator.SetBool("IsJumping", isJumping);
            animator.SetBool("IsSprinting", isSprinting);
            
            // Log animation parameters
            if (isJumping)
            {
                Debug.Log($"Animation parameters: IsJumping={isJumping}, IsGrounded={isGrounded}");
            }
        }
    }

    private void MovePlayer()
    {
        // Calculate movement direction
        Vector3 movementDirection = bodyTransform.forward * movementInput.y + bodyTransform.right * movementInput.x;

        // Calculate current speed based on sprint state
        float currentSpeed = isSprinting ? movementSpeed * sprintSpeedMultiplier : movementSpeed;

        if (isGrounded)
            // Apply stronger force for ground movement with ForceMode.Force for better acceleration
            rb.AddForce(movementDirection.normalized * currentSpeed * 10f, ForceMode.Force);
        else if (!isGrounded)
            // Apply gentler force for air control without the excessive multiplier
            rb.AddForce(movementDirection.normalized * currentSpeed * 10f * airMultiplier, ForceMode.Force);
    }

    private void SpeedControl()
    {
        Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z); // Y eksenindeki hızı sıfırla
        
        // Apply appropriate speed limit based on sprint state
        float maxSpeed = isSprinting ? movementSpeed * sprintSpeedMultiplier : movementSpeed;
        
        if (flatVel.magnitude > maxSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * maxSpeed;
            rb.linearVelocity = new Vector3(limitedVel.x, rb.linearVelocity.y, limitedVel.z); // Y eksenindeki hızı koru
        }
    }

    private void Jump()
    {
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z); // Y eksenindeki hızı sıfırla

        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse); // Zıplama kuvvetini uygula
        
        // Trigger jump animation
        if (animator != null)
        {
            animator.SetTrigger("Jump");
            Debug.Log("Jump animation triggered");

            animator.SetBool("IsJumping", true);  // Also set the bool parameter for consistency
            Debug.Log("Animation parameter set: IsJumping = true");
        }
        else
        {
            Debug.LogError("Animator reference is null!");
        }
    }

    private void ResetJump()
    {
        readyToJump = true; // Zıplama hazır
        Debug.Log("Jump cooldown finished - readyToJump set to true");
    }
}