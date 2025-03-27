using UnityEngine;

public class BodyRotation : MonoBehaviour
{
    [SerializeField] private Transform bodyTransform;

    void FixedUpdate()
    {
        transform.rotation = Quaternion.Euler(0f, bodyTransform.rotation.eulerAngles.y, 0f);
    }
}
