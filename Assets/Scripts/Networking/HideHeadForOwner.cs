using Unity.Netcode;
using UnityEngine;

public class HideHeadForOwner : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject headModel; // Oyuncunun kafa modeli
    [SerializeField] private GameObject faceStuff;
    [SerializeField] private GameObject armModel; // Oyuncunun gövde modeli
    [SerializeField] private GameObject hotbarUI; // Oyuncunun hotbar UI'si
    // [SerializeField] private GameObject inventoryUI; // Oyuncunun envanter UI'si
    // [SerializeField] private GameObject healthBar;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            // Eğer bu oyuncu kendi karakteriyse, kafa modelini gizle
            headModel.SetActive(false);
            faceStuff.SetActive(false);
            armModel.SetActive(true); // Kolları gizle
            hotbarUI.SetActive(true); 
        }
        else
        {
            // Diğer oyuncuların karakterlerinde kafa modeli görünür
            headModel.SetActive(true);
            faceStuff.SetActive(true);
            armModel.SetActive(false); // Kolları gizle
            hotbarUI.SetActive(false); // Hotbar UI'sini gizle
        }
    }
}