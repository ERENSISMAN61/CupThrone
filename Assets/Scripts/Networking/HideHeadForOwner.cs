using Unity.Netcode;
using UnityEngine;

public class HideHeadForOwner : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject headModel; // Oyuncunun kafa modeli
    [SerializeField] private GameObject faceStuff;
    [SerializeField] private GameObject armModel; // Oyuncunun gövde modeli

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            // Eğer bu oyuncu kendi karakteriyse, kafa modelini gizle
            headModel.SetActive(false);
            faceStuff.SetActive(false);
            armModel.SetActive(true); // Kolları gizle
        }
        else
        {
            // Diğer oyuncuların karakterlerinde kafa modeli görünür
            headModel.SetActive(true);
            faceStuff.SetActive(true);
            armModel.SetActive(false); // Kolları gizle
        }
    }
}