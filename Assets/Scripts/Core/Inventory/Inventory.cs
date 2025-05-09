using System;
using DapperDino.Events.CustomEvents;
using Unity.VisualScripting;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    [SerializeField] private VoidEvent onInventoryItemsUpdated = null;
    public ItemContainer ItemContainer { get; } = new ItemContainer(20);

    public void OnEnable()
    {
        ItemContainer.OnItemsUpdated += onInventoryItemsUpdated.Raise;
    }

    public void OnDisable()
    {
        ItemContainer.OnItemsUpdated -= onInventoryItemsUpdated.Raise;
    }


}
