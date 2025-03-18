using System.Runtime.CompilerServices;
using UnityEngine;

public class TestScript : MonoBehaviour
{

    [SerializeField] private InputReader inputReader;

    void Start()
    {

        inputReader.MoveEvent += HandleMove; // inputReader.MoveEvent olayı tetiklendiğinde HandleMove fonksiyonunu çağır
    }

    private void OnDestroy()
    { // Script yok edildiğinde
        inputReader.MoveEvent -= HandleMove; // inputReader.MoveEvent olayını HandleMove fonksiyonundan kaldır
    }
    private void HandleMove(Vector2 movement)
    {

        Debug.Log(movement);
    }
}
