using UnityEngine;
using UnityEngine.EventSystems;

public class InventoryItemDragHandler : ItemDragHandler
{
    [SerializeField] private ItemDestroyer itemDestroyer = null;
    public override void OnPointerUp(PointerEventData eventData)
    {
        base.OnPointerUp(eventData);

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (eventData.hovered.Count == 0)
            {
                // destroy or drop item
                InventorySlot thisSlot = ItemSlotUI as InventorySlot;
                itemDestroyer.Activate(thisSlot.ItemSlot, thisSlot.SlotIndex); // itemDestroyer'ı aktive ediyoruz. Bu sayede itemDestroyer'ı kullanabiliyoruz.
            }
        }
    }
}
