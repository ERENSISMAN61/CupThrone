using UnityEngine;
using System.Collections.Generic;

public class HandItems : MonoBehaviour
{
    [SerializeField] private Hotbar hotbar; // Reference to the Hotbar
    [SerializeField] private List<GameObject> HoldableHandItems;
    [SerializeField] private GameObject bowPrefab;

    private GameObject currentItemPrefab;
    private int currentHotbarIndex = 0;
    private bool hasBowInHand = false;

    void Start()
    {
        // Deactivate all items initially
        foreach (var item in HoldableHandItems)
        {
            if (item != null)
                item.SetActive(false);
        }

        // Start with the first hotbar slot
        SelectHotbarSlot(0);
    }

    void Update()
    {
        // Mouse wheel control
        float scrollDelta = Input.mouseScrollDelta.y;

        if (scrollDelta > 0) // Scroll up
        {
            SelectNextHotbarSlot();
        }
        else if (scrollDelta < 0) // Scroll down
        {
            SelectPreviousHotbarSlot();
        }

        // Optional: Number keys (1-9) to select hotbar slots directly
        for (int i = 0; i < 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i))
            {
                SelectHotbarSlot(i);
                //Debug.Log($"Selected hotbar slot {i + 1}");
            }
        }
    }

    private void SelectNextHotbarSlot()
    {
        currentHotbarIndex = (currentHotbarIndex + 1) % 9; // Assuming 9 slots max
        SelectHotbarSlot(currentHotbarIndex);
        //Debug.Log($"Selected hotbar slot {currentHotbarIndex + 1}");
    }

    private void SelectPreviousHotbarSlot()
    {
        currentHotbarIndex = (currentHotbarIndex - 1 + 9) % 9; // Assuming 9 slots max
        SelectHotbarSlot(currentHotbarIndex);
        //Debug.Log($"Selected hotbar slot {currentHotbarIndex - 1}");
    }

    private void SelectHotbarSlot(int slotIndex)
    {
        currentHotbarIndex = slotIndex;

        // Get all hotbar slots
        HotbarSlot[] slots = hotbar.GetComponentsInChildren<HotbarSlot>();

        if (slotIndex < slots.Length)
        {
            HotbarSlot slot = slots[slotIndex];
            if (slot != null)
            {
                // Deactivate current item
                if (currentItemPrefab != null)
                    currentItemPrefab.SetActive(false);

                // Get hand item based on hotbar item
                int handItemIndex = GetHandItemIndexForHotbarItem(slot.SlotItem);

                if (handItemIndex >= 0 && handItemIndex < HoldableHandItems.Count)
                {
                    // Activate the new hand item
                    currentItemPrefab = HoldableHandItems[handItemIndex];
                    currentItemPrefab.SetActive(true);
                    //Debug.Log($"Activated item: {currentItemPrefab.name}");

                    // Update bow status
                    hasBowInHand = (currentItemPrefab == bowPrefab);
                }
                else
                {
                    currentItemPrefab = null;
                    hasBowInHand = false;
                }
            }
        }
    }

    private int GetHandItemIndexForHotbarItem(HotbarItem item)
    {
        if (item == null)
            return -1;

        // Map HotbarItems to hand items by name or type
        // This is an example implementation - customize based on your needs
        for (int i = 0; i < HoldableHandItems.Count; i++)
        {
            //Debug.Log($"Checking item: {HoldableHandItems[i].name} against hotbar item: {item.Name}");
            if (HoldableHandItems[i].name.Contains(item.Name))
            {
                //Debug.Log($"Found matching item: {HoldableHandItems[i].name}");
                return i;
            }
        }

        // Default fallback - no matching item found
        return -1;
    }

    public bool GetHasBowInHand()
    {
        return hasBowInHand;
    }
}