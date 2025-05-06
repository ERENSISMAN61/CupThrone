using UnityEngine;
using UnityEngine.EventSystems;

public class HotbarItemDragHandler : ItemDragHandler
{
    public override void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            base.OnPointerUp(eventData);

            if(eventData.hovered.Count == 0)
            {
                (ItemSlotUI as HotbarSlot).SlotItem = null; // Remove item from hotbar slot if dropped outside
            }
        }
    }
}
