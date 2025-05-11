using Unity.Netcode;
using UnityEngine;

public class FoodWallet : NetworkBehaviour
{
    [SerializeField] private Inventory inventory = null;
    public NetworkVariable<int> TotalFoods = new NetworkVariable<int>();

    public void SpendCoins(int cost)
    {
        TotalFoods.Value -= cost;
    }

    private void OnTriggerEnter(Collider collider)
    {
        // Exit if not the local player - only process food collection for our own player
        if (!IsOwner) return;

        if (!collider.TryGetComponent<Food>(out Food food)) { return; }

        ConsumableItem foodItem = food.GetFoodItem();
        if (foodItem == null) { return; }

        // Tell the server this specific food was collected by this player
        food.CollectServerRpc(NetworkObjectId);

        // Add the food item to this player's inventory specifically
        AddFoodToInventoryServerRpc(foodItem.name, 1);
    }

    [ServerRpc(RequireOwnership = false)]
    private void AddFoodToInventoryServerRpc(string foodItemName, int quantity)
    {
        //Debug.Log($"Player {OwnerClientId} collected food: {foodItemName}");

        // Find the food item in the player's available items by name
        ConsumableItem foodItem = null;

        // Find the ConsumableItem by name in the Resources or other manager
        ConsumableItem[] allFoodItems = Resources.FindObjectsOfTypeAll<ConsumableItem>();
        foreach (var item in allFoodItems)
        {
            if (item.name == foodItemName)
            {
                foodItem = item;
                break;
            }
        }

        // Add to inventory if we found the item
        if (foodItem != null && inventory != null)
        {
            // Only add to server's (host) inventory if the server player collected it
            // Check if the collector is the host itself
            bool isHostCollector = OwnerClientId == 0;

            if (isHostCollector)
            {
                // Host collected - add to host inventory directly
                inventory.ItemContainer.AddItem(new ItemSlot(foodItem, quantity));
                //Debug.Log($"Host player collected and added {foodItem.name} to their inventory");
            }
            else
            {
                // Client collected - don't add to host inventory
                //Debug.Log($"Client player {OwnerClientId} collected {foodItem.name}, not adding to host inventory");
            }

            // Update total food count for this player
            TotalFoods.Value += quantity;

            // Send the update to the client who collected it
            UpdateClientInventoryClientRpc(foodItemName, quantity, new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new[] { OwnerClientId }
                }
            });
        }
        else
        {
            //Debug.LogWarning($"Could not find food item {foodItemName} or inventory is null");
        }
    }

    [ClientRpc]
    private void UpdateClientInventoryClientRpc(string foodItemName, int quantity, ClientRpcParams clientRpcParams)
    {
        // This will only run on the client that collected the item
        if (!IsOwner) return;

        //Debug.Log($"Client {OwnerClientId} received inventory update for item: {foodItemName}");

        // Find the ConsumableItem by name (similar to server method)
        ConsumableItem foodItem = null;
        ConsumableItem[] allFoodItems = Resources.FindObjectsOfTypeAll<ConsumableItem>();
        foreach (var item in allFoodItems)
        {
            if (item.name == foodItemName)
            {
                foodItem = item;
                break;
            }
        }

        // Update the client's local inventory if needed (if not already updated)
        if (foodItem != null && inventory != null && !IsServer)
        {
            inventory.ItemContainer.AddItem(new ItemSlot(foodItem, quantity));
            //Debug.Log($"Client {OwnerClientId} updated local inventory with: {foodItemName}");
        }
    }
}
