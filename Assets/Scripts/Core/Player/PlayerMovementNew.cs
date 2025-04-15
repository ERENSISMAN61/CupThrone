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
    [SerializeField] private float turnSpeed = 50f;
    [SerializeField] private float jumpForce; // Zıplama kuvveti
    [SerializeField] private LayerMask groundLayer; // Zemin kontrolü için layer

    [SerializeField] private float jumpCooldown; // Zıplama süresi
    [SerializeField] private float airMultiplier; // Hava kontrolü için çarpan

    private bool readyToJump = true; // Zıplama hazır mı değil mi

    private Vector2 movementInput;
    private bool isGrounded;

    // NetworkVariables for animation parameters
    private NetworkVariable<float> networkSpeed = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<float> networkDirection = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            inputReader.MoveEvent += HandleMove;
            inputReader.JumpEvent += HandleJump; // Zıplama olayını dinle
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        {
            inputReader.MoveEvent -= HandleMove;
            inputReader.JumpEvent -= HandleJump; // Zıplama olayını kaldır
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
            Jump(); // Zıplama fonksiyonunu çağır
            readyToJump = false; // Zıplama hazır değil
            Invoke(nameof(ResetJump), jumpCooldown); // Zıplama süresini sıfırla
        }
    }

    private void FixedUpdate()
    {
        // Only process movement for the owner
        if (!IsOwner) { return; }

        // Check if grounded
        isGrounded = Physics.Raycast(bodyTransform.position, Vector3.down, 0.2f, groundLayer);

        if (isGrounded)
        {
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

        // Update network variables for animation
        networkSpeed.Value = rb.linearVelocity.magnitude;
        networkDirection.Value = movementInput.x;

        // Update animator if present
        if (animator != null)
        {
            animator.SetFloat("Speed", rb.linearVelocity.magnitude);
            animator.SetFloat("Direction", movementInput.x);
            animator.SetBool("IsGrounded", isGrounded);
        }
    }

    private void MovePlayer()
    {
        // Calculate movement direction
        Vector3 movementDirection = bodyTransform.forward * movementInput.y + bodyTransform.right * movementInput.x;

        if (isGrounded)
            // Apply stronger force for ground movement with ForceMode.Force for better acceleration
            rb.AddForce(movementDirection.normalized * movementSpeed * 10f, ForceMode.Force);
        else if (!isGrounded)
            // Apply gentler force for air control without the excessive multiplier
            rb.AddForce(movementDirection.normalized * movementSpeed * 10f * airMultiplier, ForceMode.Force);
    }

    private void SpeedControl()
    {
        Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z); // Y eksenindeki hızı sıfırla
        if (flatVel.magnitude > movementSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * movementSpeed;
            rb.linearVelocity = new Vector3(limitedVel.x, rb.linearVelocity.y, limitedVel.z); // Y eksenindeki hızı koru
        }
    }

    private void Jump()
    {
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z); // Y eksenindeki hızı sıfırla

        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse); // Zıplama kuvvetini uygula
    }

    private void ResetJump()
    {
        readyToJump = true; // Zıplama hazır
    }
}