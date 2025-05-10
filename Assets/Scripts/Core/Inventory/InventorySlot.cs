using System.IO.Compression;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlot : ItemSlotUI, IDropHandler
{
    [SerializeField] private Inventory inventory = null; // Inventory referansı
    [SerializeField] private TextMeshProUGUI itemQuantityText = null; // Item sayısını gösterecek TextMeshProUGUI referansı

    public override HotbarItem SlotItem
    {
        get { return ItemSlot.item; }
        set { }
    }

    public ItemSlot ItemSlot => inventory.ItemContainer.GetSlotByIndex(SlotIndex); // Inventory'den slotu alıyoruz. Bu sayede hangi slotta olduğumuzu öğreniyoruz.

    private void Update()
    {
        UpdateSlotUI(); // SlotUI'yi güncelliyoruz. Bu sayede slotun içeriğini güncelliyoruz.
    }

    public override void OnDrop(PointerEventData eventData)
    {
        ItemDragHandler itemDragHandler = eventData.pointerDrag.GetComponent<ItemDragHandler>();

        if (itemDragHandler == null) return; // Eğer itemDragHandler null ise return ediyoruz. Bu sayede itemDragHandler'ı kontrol ediyoruz.

        if ((itemDragHandler.ItemSlotUI as InventorySlot) != null)
        {
            inventory.ItemContainer.Swap(itemDragHandler.ItemSlotUI.SlotIndex, SlotIndex); // Inventory'den slotu alıyoruz. Bu sayede hangi slotta olduğumuzu öğreniyoruz.
        }
    }

    public override void UpdateSlotUI()
    {
        if (ItemSlot.item == null)
        {
            EnableSlotUI(false);
            return;
        }

        EnableSlotUI(true); // SlotUI'yi enable ediyoruz. Bu sayede slotun içeriğini güncelliyoruz.

        itemIconImage.sprite = ItemSlot.item.Icon; // Slotun iconunu alıyoruz. Bu sayede slotun içeriğini güncelliyoruz.
        itemQuantityText.text = ItemSlot.quantity > 0 ? ItemSlot.quantity.ToString() : " "; // Slotun miktarını alıyoruz. Bu sayede slotun içeriğini güncelliyoruz.
    }

    protected override void EnableSlotUI(bool enable)
    {
        base.EnableSlotUI(enable);
        itemQuantityText.enabled = enable; // itemQuantityText'i enable ediyoruz. Bu sayede slotun içeriğini güncelliyoruz.
    }
}
