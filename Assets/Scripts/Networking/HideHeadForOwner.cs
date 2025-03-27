using Unity.Netcode;
using UnityEngine;

public class HideHeadForOwner : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject headModel; // Oyuncunun kafa modeli
    [SerializeField] private GameObject faceStuff;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            // Eğer bu oyuncu kendi karakteriyse, kafa modelini gizle
            headModel.SetActive(false);
            faceStuff.SetActive(false);
        }
        else
        {
            // Diğer oyuncuların karakterlerinde kafa modeli görünür
            headModel.SetActive(true);
            faceStuff.SetActive(true);
        }
    }
}