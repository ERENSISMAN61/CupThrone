using UnityEngine;

public class ToggleActiveWithKeyPress : MonoBehaviour
{
    [SerializeField] private KeyCode keyCode = KeyCode.None; // Default key to toggle the GameObject
    [SerializeField] private GameObject objectToToggle = null; // The GameObject to toggle

    private void Update()
    {
        if (Input.GetKeyDown(keyCode))
        {
            objectToToggle.SetActive(!objectToToggle.activeSelf); // Toggle the active state of the GameObject

            if (objectToToggle.activeSelf)
            {
                Cursor.lockState = CursorLockMode.None; // Unlock the cursor
                Cursor.visible = true; // Make the cursor visible
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked; // Lock the cursor
                Cursor.visible = false; // Hide the cursor
            }
        }
    }
}
