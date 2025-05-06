using UnityEngine;
public class Hotbar : MonoBehaviour
{   
    [SerializeField] private HotbarSlot[] hotbarSlots = new HotbarSlot[9];

    public void Add(HotbarItem itemToAdd)
    {
        foreach (HotbarSlot hotbarSlot in hotbarSlots)
        {
            if (hotbarSlot.AddItem(itemToAdd)) return;
        }
    }
}
