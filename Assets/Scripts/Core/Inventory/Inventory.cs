using System;
using DapperDino.Events.CustomEvents;
using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "New Inventory", menuName = "Items/Inventory")]
public class Inventory : ScriptableObject
{
    [SerializeField] private VoidEvent onInventoryItemsUpdated = null; 
    [SerializeField] private ItemSlot testItemSlot = new ItemSlot(); // Test item slot for testing purposes
    public ItemContainer ItemContainer { get; } = new ItemContainer(20);

    public void OnEnable()
    {
        ItemContainer.OnItemsUpdated += onInventoryItemsUpdated.Raise;
    }

    public void OnDisable()
    {
        ItemContainer.OnItemsUpdated -= onInventoryItemsUpdated.Raise;
    }

    [ContextMenu("Test Add Item")]
    public void TestAdd()
    {
        ItemContainer.AddItem(testItemSlot);
    }
}
