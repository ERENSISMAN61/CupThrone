using System;
using System.IO.Enumeration;
using UnityEngine;
using UnityEngine.InputSystem;
using static Controls;

[CreateAssetMenu(fileName = "New Input Reader", menuName = "Input/Input Reader")]
public class InputReader : ScriptableObject, IPlayerActions
{
    // Hareket olayını tetikleyen event
    public event Action<Vector2> MoveEvent;
    // Birincil ateşleme olayını tetikleyen event
    public event Action<bool> PrimaryFireEvent;
    // Sprint olayını tetikleyen event
    public event Action<bool> SprintEvent;

    public event Action JumpEvent; // Zıplama olayını tetikleyen event

    public Vector2 AimPosition { get; private set; }

    private Controls controls; // oto generate edilmiş Kontroller scripti 

    // ScriptableObject etkinleştirildiğinde çağrılır
    private void OnEnable()
    {
        // Kontrolleri başlat ve geri çağırmaları ayarla
        if (controls == null)
        {
            controls = new Controls();
            controls.Player.SetCallbacks(this); // Player çağrılarını bu sınıfa yönlendir
        }

        // Kontrolleri etkinleştir
        controls.Player.Enable();
    }

    private void OnDisable() // ScriptableObject devre dışı bırakıldığında çağrılır
                             //olası bellek sızıntılarını önlemek için disable işlemlerini yapmalıyız.
    {
        if (controls != null)
        {
            controls.Player.Disable();

            if (Application.isPlaying)//sadece oyun çalışırken bu kodu çalıştır. yoksa hata veriyor.
            {
                controls.Dispose();
            }

            controls = null;
        }
    }

    // Hareket girişi alındığında çağrılır
    public void OnMove(InputAction.CallbackContext context)
    {
        MoveEvent?.Invoke(context.ReadValue<Vector2>()); // "?" = null check . 
    }

    // Birincil ateşleme girişi alındığında çağrılır
    public void OnPrimaryFire(InputAction.CallbackContext context)
    {
        if (context.performed) // Eylem gerçekleştiğinde
        {
            PrimaryFireEvent?.Invoke(true);// Birincil ateşleme olayını tetikle
        }
        else if (context.canceled)// Eylem iptal edildiğinde
        {
            PrimaryFireEvent?.Invoke(false);// Birincil ateşleme olayını iptal et
        }
    }

    public void OnAim(InputAction.CallbackContext context)
    {
        AimPosition = context.ReadValue<Vector2>();
    }

    // Zıplama girişi alındığında çağrılır
    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed) // Eylem gerçekleştiğinde
        {
            JumpEvent?.Invoke(); // Zıplama olayını tetikle
        }
    }
    
    // Sprint girişi alındığında çağrılır
    public void OnSprint(InputAction.CallbackContext context)
    {
        if (context.performed) // Eylem gerçekleştiğinde
        {
            SprintEvent?.Invoke(true); // Sprint olayını tetikle
        }
        else if (context.canceled) // Eylem iptal edildiğinde
        {
            SprintEvent?.Invoke(false); // Sprint olayını durdur
        }
    }
}
