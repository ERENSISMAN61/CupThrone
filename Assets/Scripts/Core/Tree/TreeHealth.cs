using Unity.Netcode; // Netcode for GameObjects kütüphanesi
using UnityEngine;

public class TreeHealth : NetworkBehaviour, IInteractable // NetworkBehaviour olarak değiştirildi
{
    [SerializeField] private int maxHealth = 100;
    private int currentHealth;

    private void Start()
    {
        currentHealth = maxHealth;
    }

    public void Interact()
    {
        // İstemci, sunucuya hasar verme isteği gönderir
        TakeDamageServerRpc(25);
    }

    [ServerRpc(RequireOwnership = false)] // Sahiplik gereksinimini kaldırdık
    private void TakeDamageServerRpc(int damage)
    {
        currentHealth -= damage;
        Debug.Log($"Tree took {damage} damage. Remaining health: {currentHealth}");

        if (currentHealth <= 0)
        {
            // Yalnızca sunucu tarafında yok edilme işlemi yapılır
            GetComponent<NetworkObject>().Despawn();
        }
    }
}
