using UnityEngine;
using Unity.Cinemachine;

public class CinemachineFPSController : MonoBehaviour
{
    [Header("Camera Settings")]
    public CinemachineCamera virtualCamera;
    public float lookSensitivity = 1.0f;
    public float maxVerticalAngle = 80f; // Maximum look up/down angle

    // Internal variables
    private float currentYaw = 0f;
    private float currentPitch = 0f;
    private float targetYaw = 0f;
    private float targetPitch = 0f;
    private Transform cameraTarget;

    private void Start()
    {
        if (virtualCamera == null)
        {
            virtualCamera = GetComponent<CinemachineCamera>();
            if (virtualCamera == null)
            {
                Debug.LogError("No Cinemachine Virtual Camera assigned to FPS Controller!");
                enabled = false;
                return;
            }
        }

        // Get or create camera target
        cameraTarget = virtualCamera.Follow;
        if (cameraTarget == null)
        {
            // Create a target if none exists
            GameObject targetObj = new GameObject("CameraTarget");
            targetObj.transform.position = transform.position;
            targetObj.transform.rotation = transform.rotation;
            cameraTarget = targetObj.transform;
            virtualCamera.Follow = cameraTarget;
            virtualCamera.LookAt = cameraTarget;
        }

        // Lock cursor for FPS controls
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        // Toggle cursor lock with Escape key
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = Cursor.lockState == CursorLockMode.Locked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = !Cursor.visible;
        }

        // Only process camera movement when cursor is locked
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            // Get mouse input
            float mouseX = Input.GetAxis("Mouse X") * lookSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * lookSensitivity;

            // Update target angles
            targetYaw += mouseX;
            targetPitch -= mouseY; // Inverted for natural control
            targetPitch = Mathf.Clamp(targetPitch, -maxVerticalAngle, maxVerticalAngle);


            currentYaw = targetYaw;
            currentPitch = targetPitch;


            // Apply rotation to camera target
            cameraTarget.rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
        }
    }

    private void OnDisable()
    {
        // Unlock cursor when disabled
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
