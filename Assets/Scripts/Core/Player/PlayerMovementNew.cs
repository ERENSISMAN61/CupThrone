using Unity.Netcode;
using UnityEngine;

public class PlayerMovementNew : NetworkBehaviour
{
    [SerializeField] private InputReader inputReader;
    [SerializeField] private Transform bodyTransform;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Animator animator; // Animator bileşeni

    [SerializeField] private float movementSpeed = 10f;
    [SerializeField] private float turnSpeed = 50f;

    private Vector2 movementInput;

    // NetworkVariables for animation parameters
    private NetworkVariable<float> networkSpeed = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<float> networkDirection = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            inputReader.MoveEvent += HandleMove;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        {
            inputReader.MoveEvent -= HandleMove;
        }
    }

    private void HandleMove(Vector2 movement)
    {
        movementInput = movement;
    }

    private void Update()
    {
        if (IsOwner)
        {
            // Ensure the bodyTransform follows the parent object's position
            bodyTransform.position = rb.position;

            // Rotate the character based on horizontal input
            float yRotation = movementInput.x * turnSpeed * Time.deltaTime;
            bodyTransform.Rotate(0, yRotation, 0);

            // Update NetworkVariables for animation parameters
            float forwardSpeed = movementInput.y; // İleri ve geri hareket
            float strafeSpeed = movementInput.x;  // Sağa ve sola hareket

            networkSpeed.Value = forwardSpeed;
            networkDirection.Value = strafeSpeed;
        }

        // Update the animator parameters for all clients
        animator.SetFloat("Speed", networkSpeed.Value);  // İleri/geri hareket için
        animator.SetFloat("Direction", networkDirection.Value); // Sağa/sola hareket için
    }

    private void FixedUpdate()
    {
        if (IsOwner)
        {
            // Move the character based on input
            Vector3 movement = new Vector3(movementInput.x, 0, movementInput.y) * movementSpeed;
            rb.linearVelocity = bodyTransform.TransformDirection(movement);
        }
    }
}