using UnityEngine;

public class FPSCameraController : MonoBehaviour
{
    [Header("Bakış Ayarları")]
    [SerializeField] private float mouseSensitivity = 100f;
    [SerializeField] private Transform playerBody;
    [SerializeField] private bool lockCursor = true;

    private float xRotation = 0f;
    
    void Start()
    {
        
        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        
        // Eğer playerBody atanmamışsa, kameranın bağlı olduğu objeyi kullan
        if (playerBody == null)
            playerBody = transform.parent;

    }

    void Update()
    {
        // Bakış kontrolü
        MouseLook();

    }

    void MouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Dikey rotasyon (kamera için)
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        
        // Yatay rotasyon (oyuncu gövdesi için)
        if (playerBody != null)
            playerBody.Rotate(Vector3.up * mouseX);
    }

}
