using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;

public class HandItems : NetworkBehaviour
{
    [SerializeField] private Hotbar hotbar; // Reference to the Hotbar
    [SerializeField] private List<GameObject> HoldableHandItems;
    [SerializeField] private List<GameObject> BodyHandItems; // Player modelinin elindeki itemler
    [SerializeField] private GameObject bowPrefab;

    private GameObject currentItemPrefab;
    private int currentHotbarIndex = 0;
    private bool hasBowInHand = false;

    // NetworkVariable for selected hotbar index
    private NetworkVariable<int> networkHotbarIndex = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    void Start()
    {
        // Deactivate all items initially
        foreach (var item in HoldableHandItems)
        {
            if (item != null)
                item.SetActive(false);
        }
        // BodyHandItems'i deaktif et
        if (BodyHandItems != null)
        {
            foreach (var item in BodyHandItems)
            {
                if (item != null)
                    item.SetActive(false);
            }
        }
        // NetworkVariable değişimini dinle
        networkHotbarIndex.OnValueChanged += OnNetworkHotbarIndexChanged;
        // Start with the first hotbar slot
        SelectHotbarSlot(0, true);
    }

    void Update()
    {
        if (!IsOwner) return; // Sadece local player input alır

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
        int nextIndex = (currentHotbarIndex + 1) % 9;
        SelectHotbarSlot(nextIndex);
    }

    private void SelectPreviousHotbarSlot()
    {
        int prevIndex = (currentHotbarIndex - 1 + 9) % 9;
        SelectHotbarSlot(prevIndex);
    }

    private void SelectHotbarSlot(int slotIndex, bool forceLocal = false)
    {
        if (!forceLocal && IsOwner)
        {
            // Sadece owner input alır, server'a bildirir
            SelectHotbarSlotServerRpc(slotIndex);
        }
        currentHotbarIndex = slotIndex;
        // FPS eldeki item sadece local player'da aktif
        if (IsOwner)
        {
            // Deactivate current item (FPS eldeki)
            if (currentItemPrefab != null)
                currentItemPrefab.SetActive(false);
            // Get hand item based on hotbar item
            int handItemIndex = GetHandItemIndexForHotbarItem(GetHotbarSlotItem(slotIndex));
            if (handItemIndex >= 0 && handItemIndex < HoldableHandItems.Count)
            {
                // Activate the new hand item (FPS eldeki)
                currentItemPrefab = HoldableHandItems[handItemIndex];
                currentItemPrefab.SetActive(true);
                // Update bow status
                hasBowInHand = (currentItemPrefab == bowPrefab);
            }
            else
            {
                currentItemPrefab = null;
                hasBowInHand = false;
            }
        }
        // BodyHandItems her client'ta aktif/pasif yapılır
        UpdateBodyHandItems(slotIndex);
    }

    [ServerRpc]
    private void SelectHotbarSlotServerRpc(int slotIndex)
    {
        networkHotbarIndex.Value = slotIndex;
    }

    private void OnNetworkHotbarIndexChanged(int oldValue, int newValue)
    {
        // networkHotbarIndex değiştiğinde her iki taraf da güncellenir
        SelectHotbarSlot(newValue, true);
    }

    private void UpdateBodyHandItems(int slotIndex)
    {
        if (BodyHandItems != null)
        {
            // Eğer bu obje local player ise kendi body itemlarını asla gösterme
            // Yani: BodyHandItems sadece diğer oyunculara gösterilir, kendine asla gösterilmez!
            if (IsOwner)
            {
                foreach (var item in BodyHandItems)
                {
                    if (item != null)
                        item.SetActive(false);
                }
                return;
            }
            // Diğer oyuncular için body itemları göster
            foreach (var item in BodyHandItems)
            {
                if (item != null)
                    item.SetActive(false);
            }
            int handItemIndex = GetHandItemIndexForHotbarItem(GetHotbarSlotItem(slotIndex));
            if (handItemIndex >= 0 && handItemIndex < BodyHandItems.Count)
            {
                var bodyItem = BodyHandItems[handItemIndex];
                if (bodyItem != null)
                    bodyItem.SetActive(true);
            }
        }
    }

    private HotbarItem GetHotbarSlotItem(int slotIndex)
    {
        HotbarSlot[] slots = hotbar.GetComponentsInChildren<HotbarSlot>();
        if (slotIndex < slots.Length)
            return slots[slotIndex].SlotItem;
        return null;
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