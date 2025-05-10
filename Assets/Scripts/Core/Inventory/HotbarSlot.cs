using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class HotbarSlot : ItemSlotUI, IDropHandler
{
    [SerializeField] private Inventory inventory = null;
    [SerializeField] private TextMeshProUGUI itemQuantityText = null;
    private HotbarItem slotItem = null; 

    public override HotbarItem SlotItem
    {
        get { return slotItem; }
        set { slotItem = value; UpdateSlotUI(); }
    }

    public bool AddItem(HotbarItem itemToAdd)
    {
        if (SlotItem != null) 
        {
            Debug.Log($"Hotbar slot {SlotIndex} is already occupied.");
            return false; // Slot is already occupied
        }

        SlotItem = itemToAdd;
        
        Debug.Log($"Added {itemToAdd.Name} to hotbar slot {SlotIndex}");
        return true;
    }

    public void UseSlot(int index)
    {
        if (index != SlotIndex) return;

        // Use item
    }

    public override void OnDrop(PointerEventData eventData)
    {
        ItemDragHandler itemDragHandler = eventData.pointerDrag.GetComponent<ItemDragHandler>();
        if (itemDragHandler == null) return;

        InventorySlot inventorySlot = itemDragHandler.ItemSlotUI as InventorySlot;
        if (inventorySlot != null)
        {
            SlotItem = inventorySlot.SlotItem;
            return;
        }

        HotbarSlot hotbarSlot = itemDragHandler.ItemSlotUI as HotbarSlot;
        if (hotbarSlot != null)
        {
            HotbarItem oldItem = SlotItem;
            SlotItem = hotbarSlot.SlotItem;
            hotbarSlot.SlotItem = oldItem;
            return;
        }
    }

    public override void UpdateSlotUI()
    {
        if(SlotItem == null)
        {
            EnableSlotUI(false);
            return;
        }

        itemIconImage.sprite = SlotItem.Icon;

        Debug.Log($"Hotbar slot {SlotIndex} updated with item: {SlotItem.Name}");

        EnableSlotUI(true);

        SetItemQuantityUI();
    }

    private void SetItemQuantityUI()
    {
        if (SlotItem is InventoryItem inventoryItem)
        {
            if(inventory.ItemContainer.HasItem(inventoryItem))
            {
                int quantityCount = inventory.ItemContainer.GetTotalQuantity(inventoryItem);
                itemQuantityText.text = quantityCount > 1 ? quantityCount.ToString() : " ";
            }
            else 
            {
                SlotItem = null;
            }
        }
        else
        {
            itemQuantityText.enabled = false;
        }
    }

    protected override void EnableSlotUI(bool enable)
    {
        base.EnableSlotUI(enable);
        itemQuantityText.enabled = enable;
    }
}