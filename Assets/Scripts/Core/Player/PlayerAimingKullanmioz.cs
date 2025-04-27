using Unity.Netcode;
using UnityEngine;

public class PlayerAiming : NetworkBehaviour
{
    [SerializeField] private InputReader inputReader;
    [SerializeField] private Transform towerTransform;

    private void LateUpdate()
    {
        if (!IsOwner) { return; }

        Vector3 mouseScreenPosition = inputReader.AimPosition;
        mouseScreenPosition.z = Camera.main.WorldToScreenPoint(towerTransform.position).z;
        Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(mouseScreenPosition);



        // Yön vektörünü hesaplıyoruz
        Vector3 direction = mouseWorldPosition - towerTransform.position;  // Kule pozisyonundan hedef pozisyonunu çıkararak yön vektörünü buluyoruz
        direction.y = 0f; // Y eksenini sıfırlayarak sadece yatay düzlemde dönmeyi sağlıyoruz

        // Eğer direction sıfır vektörü değilse işlem yapıyoruz
        if (direction != Vector3.zero)
        {
            // Hedef rotasyonu hesaplıyoruz
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            // Kuleyi hedef rotasyona döndürüyoruz (Y ekseni etrafında)
            towerTransform.rotation = Quaternion.Euler(0f, targetRotation.eulerAngles.y, 0f);
            //eulerAngles, bir rotasyonun x, y ve z eksenlerindeki açısal değerlerini döndürür.
        }


        // towerTransform.up = new Vector2(0, mouseWorldPosition.y - towerTransform.position.y);
        //transform.up, nedir? transform.up, transform'un yukarısını temsil eder.
        //transform.forward, transform'un ileri yönünü temsil eder. 
    }
}
