using UnityEngine;

public class BulletLifeTime : MonoBehaviour
{
    [SerializeField] private float DestroyTime = 2f;
    void Start()
    {
        Destroy(gameObject, DestroyTime);
    }


}
