using Unity.Netcode; // Netcode for GameObjects kütüphanesi
using UnityEngine;
using System.Collections.Generic;

public class TreeHealth : NetworkBehaviour, IInteractable
{
    [SerializeField] private int maxHealth = 100;
    private NetworkVariable<int> currentHealth = new NetworkVariable<int>();
    
    // Track recent damage events to prevent duplicates
    private Dictionary<ulong, float> clientDamageTimestamps = new Dictionary<ulong, float>();
    private float damageTimeout = 0.5f; // Cooldown in seconds
    
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentHealth.Value = maxHealth;
        }
    }

    public void Interact()
    {
        Debug.Log($"Tree Interact called. IsServer: {IsServer}, IsClient: {IsClient}, IsHost: {IsHost}");
        
        // If we're on a dedicated client, we should call the ServerRpc
        if (IsClient && !IsHost)
        {
            Debug.Log("CLIENT MODE: Sending damage request to server");
            TakeDamageServerRpc(25, NetworkManager.Singleton.LocalClientId);
        }
        // If we're on the server (or host), apply damage directly
        else if (IsServer)
        {
            Debug.Log("SERVER MODE: Applying damage directly");
            // For server, use server's client ID (usually 0)
            ApplyDamage(25, NetworkManager.Singleton.LocalClientId);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void TakeDamageServerRpc(int damage, ulong clientId)
    {
        Debug.Log($"ServerRpc received with damage: {damage} from client: {clientId}");
        ApplyDamage(damage, clientId);
    }

    private void ApplyDamage(int damage, ulong clientId)
    {
        // Only the server can modify NetworkVariables
        if (!IsServer)
        { 
            Debug.LogWarning("ApplyDamage called on client! This shouldn't happen.");
            return;
        }
        
        // Check if this client has recently done damage
        if (clientDamageTimestamps.TryGetValue(clientId, out float lastDamageTime))
        {
            if (Time.time - lastDamageTime < damageTimeout)
            {
                Debug.Log($"Damage cooldown active for client {clientId}. Ignoring this hit.");
                return;
            }
        }
        
        // Record this damage event
        clientDamageTimestamps[clientId] = Time.time;
        
        Debug.Log($"BEFORE: Tree health: {currentHealth.Value}");
        currentHealth.Value -= damage;
        Debug.Log($"AFTER: Tree took {damage} damage from client {clientId}. Remaining health: {currentHealth.Value}");

        if (currentHealth.Value <= 0)
        {
            Debug.Log("Tree health reached zero, despawning");
            GetComponent<NetworkObject>().Despawn();
        }
    }
}
