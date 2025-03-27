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

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) { return; }

        inputReader.MoveEvent += HandleMove;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) { return; }

        inputReader.MoveEvent -= HandleMove;
    }

    private void HandleMove(Vector2 movement)
    {
        movementInput = movement;
    }

    private void Update()
    {
        if (!IsOwner) { return; }

        // Ensure the bodyTransform follows the parent object's position
        bodyTransform.position = rb.position;

        // Rotate the character based on horizontal input
        float yRotation = movementInput.x * turnSpeed * Time.deltaTime;
        bodyTransform.Rotate(0, yRotation, 0);

        // Update the animator parameters
        float forwardSpeed = movementInput.y; // İleri ve geri hareket
        float strafeSpeed = movementInput.x;  // Sağa ve sola hareket

        animator.SetFloat("Speed", forwardSpeed);  // İleri/geri hareket için
        animator.SetFloat("Direction", strafeSpeed); // Sağa/sola hareket için
    }

    private void FixedUpdate()
    {
        // Move the character based on input
        Vector3 movement = new Vector3(movementInput.x, 0, movementInput.y) * movementSpeed;
        rb.linearVelocity = bodyTransform.TransformDirection(movement);
    }
}