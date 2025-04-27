using UnityEngine;

public class ProjectileFallingController : MonoBehaviour
{
    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 40f;       // Okun dönme hızı (derece/saniye)
    [SerializeField] private float maxDownwardAngle = 90f;    // Maksimum aşağı yöneliş açısı
    [SerializeField] private float fallDetectionDelay = 0.2f; // Düşüşü algılamadan önce bekleme süresi
    [SerializeField] private float velocityThreshold = 0.1f;  // Y hızının sıfır kabul edilmesi için eşik değeri

    private Rigidbody rb;
    private Vector3 previousPosition;
    private float timeSinceLaunch = 0f;
    private bool isFalling = false;
    private bool isRising = true;
    private Vector3 initialForward;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("ProjectileFallingController requires a Rigidbody component!");
            enabled = false;
            return;
        }

        previousPosition = transform.position;
        initialForward = transform.forward;

        // Başlangıçta ok yükseliyorsa rotasyonu sıfırla
        if (rb.linearVelocity.y > 0)
        {
            isRising = true;
            SetAscendingRotation();
        }
    }

    void Update()
    {
        if (rb == null) return;

        timeSinceLaunch += Time.deltaTime;

        // Başlangıçta bir süre geçmeden düşme kontrolü yapma
        if (timeSinceLaunch < fallDetectionDelay)
            return;

        // Y hızını kontrol et
        float yVelocity = rb.linearVelocity.y;

        // Yükselme durumunda
        if (isRising && yVelocity <= velocityThreshold)
        {
            isRising = false;
            Debug.Log("Ok tepe noktasına ulaştı!");
        }

        // Düşme durumunda
        if (!isFalling && yVelocity < -velocityThreshold)
        {
            isFalling = true;
            Debug.Log("Ok düşmeye başladı!");
        }

        // Rotasyonu güncelle
        if (isRising)
        {
            SetAscendingRotation();
        }
        else if (isFalling)
        {
            RotateTowardsFallingDirection();
        }
    }

    private void SetAscendingRotation()
    {
        // Mevcut rotasyonu al
        Vector3 currentRotation = transform.rotation.eulerAngles;

        // X rotasyonunu 0'a doğru yumuşak geçiş yap
        float currentXRotation = currentRotation.x;
        if (currentXRotation > 180)
            currentXRotation -= 360;

        float newXRotation = Mathf.MoveTowards(currentXRotation, 0f, rotationSpeed * Time.deltaTime);

        // Yeni rotasyonu uygula, Y ve Z değerlerini koru
        transform.rotation = Quaternion.Euler(newXRotation, currentRotation.y, currentRotation.z);
    }

    private void RotateTowardsFallingDirection()
    {
        // Mevcut rotasyonu al
        Vector3 currentRotation = transform.rotation.eulerAngles;

        // X eksenindeki rotasyonu ayarla (0-360 aralığından 0-180 aralığına dönüştür)
        float currentXRotation = currentRotation.x;
        if (currentXRotation > 180)
            currentXRotation -= 360;

        // Hedef rotasyona doğru yumuşak geçiş yap
        float targetXRotation = maxDownwardAngle;
        float newXRotation = Mathf.MoveTowards(currentXRotation, targetXRotation, rotationSpeed * Time.deltaTime);

        // Yeni rotasyonu uygula, Y ve Z değerlerini koru
        transform.rotation = Quaternion.Euler(newXRotation, currentRotation.y, currentRotation.z);
    }
}
