using Unity.Netcode;
using UnityEngine;

public class ResourceWallet : NetworkBehaviour
{
    [SerializeField] private Inventory inventory = null;
    public NetworkVariable<int> TotalResources = new NetworkVariable<int>();

    public void SpendResources(int cost)
    {
        TotalResources.Value -= cost;
    }

    private void OnTriggerEnter(Collider collider)
    {
        // Exit if not the local player - only process resource collection for our own player
        if (!IsOwner) return;

        // Check if it's a food item
        if (collider.TryGetComponent<Food>(out Food food))
        {
            ConsumableItem foodItem = food.GetFoodItem();
            if (foodItem != null)
            {
                // Tell the server this specific food was collected by this player
                food.CollectServerRpc(NetworkObjectId);

                // Add the food item to this player's inventory specifically
                AddItemToInventoryServerRpc(foodItem.name, 1, "ConsumableItem");
            }
            return;
        }
    }

    // Public method to add items to inventory that can be called from other scripts
    public void AddResourceToInventory(string itemName, int quantity, string itemType)
    {
        AddItemToInventoryServerRpc(itemName, quantity, itemType);
    }

    [ServerRpc(RequireOwnership = false)]
    private void AddItemToInventoryServerRpc(string itemName, int quantity, string itemType)
    {
        //Debug.Log($"Player {OwnerClientId} collected item: {itemName} of type {itemType}");

        // Find the item in the player's available items by name
        Object item = null;

        if (itemType == "ConsumableItem")
        {
            // Find the ConsumableItem by name in the Resources or other manager
            ConsumableItem[] allFoodItems = Resources.FindObjectsOfTypeAll<ConsumableItem>();
            foreach (var foodItem in allFoodItems)
            {
                if (foodItem.name == itemName)
                {
                    item = foodItem;
                    break;
                }
            }
        }
        else if (itemType == "CraftingItem")
        {
            // Find the MineItem by name in the Resources or other manager
            CraftingItem[] allCraftingItems = Resources.FindObjectsOfTypeAll<CraftingItem>();
            foreach (var craftingItem in allCraftingItems)
            {
                if (craftingItem.name == itemName)
                {
                    item = craftingItem;
                    break;
                }
            }
        }

        // Add to inventory if we found the item
        if (item != null && inventory != null)
        {
            // Only add to server's (host) inventory if the server player collected it
            // Check if the collector is the host itself
            bool isHostCollector = OwnerClientId == 0;

            if (isHostCollector)
            {
                // Host collected - add to host inventory directly
                inventory.ItemContainer.AddItem(new ItemSlot(item as InventoryItem, quantity));
                //Debug.Log($"Host player collected and added {itemName} to their inventory");
            }
            else
            {
                // Client collected - don't add to host inventory
                //Debug.Log($"Client player {OwnerClientId} collected {itemName}, not adding to host inventory");
            }

            // Update total resource count for this player
            TotalResources.Value += quantity;

            // Send the update to the client who collected it
            UpdateClientInventoryClientRpc(itemName, quantity, itemType, new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new[] { OwnerClientId }
                }
            });
        }
        else
        {
            //Debug.LogWarning($"Could not find item {itemName} of type {itemType} or inventory is null");
        }
    }

    [ClientRpc]
    private void UpdateClientInventoryClientRpc(string itemName, int quantity, string itemType, ClientRpcParams clientRpcParams)
    {
        // This will only run on the client that collected the item
        if (!IsOwner) return;

        //Debug.Log($"Client {OwnerClientId} received inventory update for item: {itemName}");

        // Find the item by name and type
        Object item = null;
        
        if (itemType == "ConsumableItem")
        {
            // Find the ConsumableItem by name
            ConsumableItem[] allFoodItems = Resources.FindObjectsOfTypeAll<ConsumableItem>();
            foreach (var foodItem in allFoodItems)
            {
                if (foodItem.name == itemName)
                {
                    item = foodItem;
                    break;
                }
            }
        }
        else if (itemType == "CraftingItem")
        {
            // Find the MineItem by name
            CraftingItem[] allCraftingItems = Resources.FindObjectsOfTypeAll<CraftingItem>();
            foreach (var craftingItem in allCraftingItems)
            {
                if (craftingItem.name == itemName)
                {
                    item = craftingItem;
                    break;
                }
            }
        }
        

        // Update the client's local inventory if needed (if not already updated)
        if (item != null && inventory != null && !IsServer)
        {
            inventory.ItemContainer.AddItem(new ItemSlot(item as InventoryItem, quantity));
            //Debug.Log($"Client {OwnerClientId} updated local inventory with: {itemName}");
        }
    }
}
