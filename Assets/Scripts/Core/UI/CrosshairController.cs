using UnityEngine;
using UnityEngine.UI;

public class CrosshairController : MonoBehaviour
{
    [SerializeField] private Image crosshairImage; // Crosshair UI Image referansı
    [SerializeField] private Canvas crosshairCanvas; // Crosshair Canvas referansı

    private void Start()
    {
        if (crosshairImage == null)
        {
            Debug.LogError("Crosshair Image is not assigned in the Inspector!");
        }

        if (crosshairCanvas == null)
        {
            Debug.LogError("Crosshair Canvas is not assigned in the Inspector!");
        }
    }

    public void SetCrosshairVisibility(bool isVisible)
    {
        if (crosshairImage != null)
        {
            crosshairImage.enabled = isVisible; // Crosshair'i görünür/görünmez yapar
        }
    }

    public void AssignCamera(Camera playerCamera)
    {
        if (crosshairCanvas != null && playerCamera != null)
        {
            crosshairCanvas.worldCamera = playerCamera; // Canvas'ı oyuncunun kamerasına bağla
        }
    }
}
