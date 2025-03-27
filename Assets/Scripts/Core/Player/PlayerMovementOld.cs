using Unity.Netcode;
using Unity.Services.Matchmaker.Models;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour
{
    [SerializeField] private InputReader inputReader;
    [SerializeField] private Transform bodyTransform;
    [SerializeField] private Rigidbody rb;


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

        //movementInput.x, yatay inputları alır. Yani "A" ve "D" tuşlarına basıldığında -1 ve 1 değerlerini alır.       
        //movementInput.x, -1 ile 1 arasında bir değer alır. turnSpeed ile çarparak dönüş hızını ayarlar.
        //Time.deltaTime, her frame arasındaki zaman farkını alır. Bu sayede dönüş hızı sabit kalır.
        //Eğer Time.deltaTime kullanmazsak, dönüş hızı frame rate'e bağlı olarak değişir.
        float yRotation = movementInput.x * turnSpeed * Time.deltaTime;
        //Time.fixedDeltaTime, sabit bir zaman aralığında çalışan fizik işlemleri için kullanılır.


        bodyTransform.Rotate(0, yRotation, 0);
    }

    private void FixedUpdate()
    {
        if (!IsOwner) { return; }

        //movementInput.y, dikey inputları alır. Yani "W" ve "S" tuşlarına basıldığında -1 ve 1 değerlerini alır.
        rb.linearVelocity = bodyTransform.forward * movementInput.y * movementSpeed;

    }

}
