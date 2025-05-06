using UnityEngine;

public interface IItemContainer
{
    public ItemSlot AddItem(ItemSlot itemSlot);

    void RemoveItem(ItemSlot itemSlot);

    void RemoveAt(int slotIndex);

    void Swap(int fromIndex, int toIndex);

    bool HasItem(InventoryItem item);

    int GetTotalQuantity(InventoryItem item);
    
}
