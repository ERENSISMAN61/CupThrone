using Unity.Netcode;
using UnityEngine;

public class PlayerMovementNew : NetworkBehaviour
{
    [SerializeField] private InputReader inputReader;
    [SerializeField] private Transform bodyTransform;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Animator animator;

    [SerializeField] private float groundDrag;
    [SerializeField] private float movementSpeed;
    [SerializeField] private float sprintSpeedMultiplier = 1.5f;
    [SerializeField] private float turnSpeed = 50f;
    [SerializeField] private float jumpForce;
    [SerializeField] private LayerMask groundLayer;

    [SerializeField] private float airMultiplier;

    private Vector2 movementInput;
    private bool isGrounded;
    private bool isJumping = false;
    private bool isSprinting = false;

    // NetworkVariables for animation parameters
    private NetworkVariable<float> networkSpeed = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<float> networkDirection = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<bool> networkIsJumping = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<bool> networkIsSprinting = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<bool> networkIsGrounded = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            inputReader.MoveEvent += HandleMove;
            inputReader.JumpEvent += HandleJump;
            inputReader.SprintEvent += HandleSprint;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        {
            inputReader.MoveEvent -= HandleMove;
            inputReader.JumpEvent -= HandleJump;
            inputReader.SprintEvent -= HandleSprint;
        }
    }

    private void HandleMove(Vector2 movement)
    {
        movementInput = movement;
    }

    private void HandleJump()
    {
        if (isGrounded && !isJumping)
        {
            isJumping = true;
            networkIsJumping.Value = true;           
            Debug.Log("Jumping - setting IsJumping to true");

            Jump();                      
            Debug.Log("Jump function called");
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
        if (IsOwner)
        {
            // Check if grounded
            isGrounded = Physics.Raycast(bodyTransform.position, Vector3.down, 0.2f, groundLayer);
            networkIsGrounded.Value = isGrounded;

            // Only reset jump state when we've actually landed after being in the air
            if (isGrounded && networkIsJumping.Value && rb.linearVelocity.y <= 0.1f)
            {
                // Wait a bit before resetting jump state to allow animation to complete
                Invoke("ResetJumpState", 0.1f);
            }

            if (isGrounded)
            {
//                Debug.Log("Player is grounded - applying ground drag");
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
            networkIsJumping.Value = isJumping;
            networkIsSprinting.Value = isSprinting;

            // Update animator if present
            UpdateAnimatorParameters();
        }
        else
        {
            // Non-owner code: we don't control movement, just update animations from network values
            UpdateAnimatorParameters();
        }
    }

    private void ResetJumpState()
    {
        networkIsJumping.Value = false;
        isJumping = false;
        Debug.Log("Reset jump state");
    }

    private void UpdateAnimatorParameters()
    {
        if (animator != null)
        {
            animator.SetFloat("Speed", networkSpeed.Value);
            animator.SetFloat("Direction", networkDirection.Value);
            animator.SetBool("IsGrounded", networkIsGrounded.Value);
            animator.SetBool("IsJumping", networkIsJumping.Value);
            animator.SetBool("IsSprinting", networkIsSprinting.Value);
            
            // Log animation parameters
            if (networkIsJumping.Value)
            {
                Debug.Log($"Animation parameters: IsJumping={networkIsJumping.Value}, IsGrounded={networkIsGrounded.Value}");
            }
        }
    }

    private void MovePlayer()
    {
        // Calculate movement direction
        Vector3 movementDirection = bodyTransform.forward * movementInput.y + bodyTransform.right * movementInput.x;

        // Only apply sprint multiplier when moving forward
        bool canSprint = movementInput.y > 0;
        float currentSpeed = (isSprinting && canSprint) ? movementSpeed * sprintSpeedMultiplier : movementSpeed;

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
        
        // Only apply sprint speed limit when moving forward
        bool canSprint = movementInput.y > 0;
        float maxSpeed = (isSprinting && canSprint) ? movementSpeed * sprintSpeedMultiplier : movementSpeed;
        
        if (flatVel.magnitude > maxSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * maxSpeed;
            rb.linearVelocity = new Vector3(limitedVel.x, rb.linearVelocity.y, limitedVel.z); // Y eksenindeki hızı koru
        }
    }

    private void Jump()
    {
        // Reset vertical velocity before applying jump force
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);

        // Apply jump force
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        
        // Ensure the jump animation is triggered correctly
        if (animator != null)
        {
            // Set both the trigger and the bool parameter
            animator.SetTrigger("Jump");
            animator.SetBool("IsJumping", true);
            Debug.Log("Jump animation triggered");
        }
        else
        {
            Debug.LogError("Animator reference is null!");
        }
    }
}