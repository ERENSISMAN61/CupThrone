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
        if (!collider.TryGetComponent<Food>(out Food food)) { return; }

        ConsumableItem foodItem = food.GetFoodItem();
        if (foodItem == null) { return; }
        
        // Collect the food object (hide/destroy it)
        int foodValue = food.Collect();
        
        if (!IsServer) { return; }

        // Add the food item to inventory
        inventory.ItemContainer.AddItem(new ItemSlot(foodItem, 1));
        
        // Still track total food count if needed
        TotalFoods.Value += foodValue;
    }
}
