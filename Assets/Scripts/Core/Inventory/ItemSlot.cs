using System;
using UnityEngine;

[Serializable]
public struct ItemSlot
{
    public InventoryItem item; // InventoryItem türünde bir item değişkeni
    public int quantity;

    public ItemSlot(InventoryItem item, int quantity)
    {
        this.item = item; // item değişkenini parametre ile başlatıyoruz
        this.quantity = quantity; // quantity değişkenini parametre ile başlatıyoruz
    }

    public static bool operator == (ItemSlot a, ItemSlot b) { return a.Equals(b); }
    public static bool operator != (ItemSlot a, ItemSlot b) { return !a.Equals(b); }
}
