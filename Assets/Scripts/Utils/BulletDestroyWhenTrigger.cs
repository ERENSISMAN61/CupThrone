using UnityEngine;

public class BulletDestroyWhenTrigger : MonoBehaviour
{

    private void OnTriggerEnter(Collider other)
    {

        Destroy(gameObject);
    }
}
