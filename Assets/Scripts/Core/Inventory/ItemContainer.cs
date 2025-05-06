using System;
using Unity.VisualScripting;
using UnityEngine;

[Serializable]
public class ItemContainer : IItemContainer
{
    private ItemSlot[] itemSlots = new ItemSlot[0]; // 0 slotlu bir envanter oluşturuyoruz. Bu slotlar ItemSlot türünde olacak.

    public Action OnItemsUpdated = delegate { }; // Itemlar güncellendiğinde çağrılacak olan bir action oluşturuyoruz.

    public ItemContainer(int size) => itemSlots = new ItemSlot[size]; // Slot sayısını parametre olarak alıyoruz. Bu sayede slot sayısını değiştirebiliriz.

    public ItemSlot GetSlotByIndex(int index) => itemSlots[index]; // Slot indexini alıyoruz. Bu sayede hangi slotta olduğumuzu öğreniyoruz.
    
    public ItemSlot AddItem(ItemSlot itemSlot)
    {
        for (int i = 0; i < itemSlots.Length; i++)
        {
            if (itemSlots[i].item != null)
            {
                if (itemSlots[i].item == itemSlot.item)
                {
                    int slotRemainingSpace = itemSlots[i].item.MaxStackSize - itemSlots[i].quantity; // Slotun kalan boşluğunu alıyoruz.

                    if (itemSlot.quantity <= slotRemainingSpace)
                    {
                        itemSlots[i].quantity += itemSlot.quantity;
                        itemSlot.quantity = 0; // Slotun miktarını arttırıyoruz ve aradığımız itemin miktarını sıfırlıyoruz.

                        OnItemsUpdated.Invoke(); // Itemlar güncellendiğinde çağrılacak olan action'ı çağırıyoruz.

                        return itemSlot; // Aradığımız itemin miktarını döndürüyoruz.
                    }
                    else if (slotRemainingSpace > 0) // Eğer slotun kalan boşluğu 0'dan büyükse
                    {
                        itemSlots[i].quantity += slotRemainingSpace; // Slotun miktarını arttırıyoruz.

                        itemSlot.quantity -= slotRemainingSpace; // Aradığımız itemin miktarını azaltıyoruz.
                    }
                }
            }
        }

        for (int i = 0; i < itemSlots.Length; i++)
        {
            if (itemSlots[i].item == null) // Eğer slotta bulunan item, null ise
            {
                if (itemSlot.quantity <= itemSlot.item.MaxStackSize)
                {
                    itemSlots[i] = itemSlot; // Slotu aradığımız item ile dolduruyoruz.

                    itemSlot.quantity = 0; // Aradığımız itemin miktarını sıfırlıyoruz.

                    OnItemsUpdated.Invoke(); // Itemlar güncellendiğinde çağrılacak olan action'ı çağırıyoruz.

                    return itemSlot; // Aradığımız itemin miktarını döndürüyoruz.
                }
                else
                {
                    itemSlots[i] = new ItemSlot(itemSlot.item, itemSlot.item.MaxStackSize); // Slotu aradığımız item ile dolduruyoruz.

                    itemSlot.quantity -= itemSlot.item.MaxStackSize; // Aradığımız itemin miktarını azaltıyoruz.
                }
            }
        }

        OnItemsUpdated.Invoke(); // Itemlar güncellendiğinde çağrılacak olan action'ı çağırıyoruz.

        return itemSlot; // Aradığımız itemin miktarını döndürüyoruz.
    }

    public void RemoveItem(ItemSlot itemSlot)
    {
        for (int i = 0; i < itemSlots.Length; i++)
        {
            if (itemSlots[i].item != null)
            {
                if (itemSlots[i].item == itemSlot.item) // Eğer slotta bulunan item, aradığımız item ise
                {
                    if (itemSlots[i].quantity < itemSlot.quantity)
                    {
                        itemSlot.quantity -= itemSlots[i].quantity; // Eğer slotta bulunan itemin miktarı, aradığımız itemin miktarından küçükse, aradığımız itemin miktarını azaltıyoruz.

                        itemSlots[i] = new ItemSlot(); // Slotu boşaltıyoruz.

                    }
                    else
                    {
                        itemSlots[i].quantity -= itemSlot.quantity; // Eğer slotta bulunan itemin miktarı, aradığımız itemin miktarından büyükse, slotta bulunan itemin miktarını azaltıyoruz.

                        if (itemSlots[i].quantity == 0) // Eğer slotta bulunan itemin miktarı 0'dan küçük veya eşitse
                        {
                            itemSlots[i] = new ItemSlot(); // Slotu boşaltıyoruz.

                            OnItemsUpdated.Invoke(); // Itemlar güncellendiğinde çağrılacak olan action'ı çağırıyoruz.

                            return;
                        }
                    }
                }
            }
        }
    }

    public void RemoveAt(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex > itemSlots.Length - 1) return; // Eğer slot indexi, 0'dan küçük veya slot sayısından büyükse return et.

        itemSlots[slotIndex] = new ItemSlot(); // Slot indexini boşaltıyoruz.

        OnItemsUpdated.Invoke(); // Itemlar güncellendiğinde çağrılacak olan action'ı çağırıyoruz.
    }

    public bool HasItem(InventoryItem item)
    {
        foreach (ItemSlot itemSlot in itemSlots)
        {
            if (itemSlot.item == null) continue; // Eğer item slotta bulunan item, null ise devam et.

            if (itemSlot.item != item) continue; // Eğer item slotta bulunan item, aradığımız item değilse devam et. 

            return true; // Eğer item slotta bulunan item, aradığımız item ise true döndür.
        }

        return false; // Eğer hiçbiri değilse false döndür.
    }

    public void Swap(int fromIndex, int toIndex)
    {
        ItemSlot firstSlot = itemSlots[fromIndex]; // İlk slotu alıyoruz.
        ItemSlot secondSlot = itemSlots[toIndex]; // İkinci slotu alıyoruz.

        if (firstSlot == secondSlot) return; // Eğer ilk slot ile ikinci slot aynı ise return et.

        if (secondSlot.item != null)
        {
            if (firstSlot.item == secondSlot.item)
            {
                int secondSlotRemainingSpace = secondSlot.item.MaxStackSize - secondSlot.quantity; // İkinci slotun kalan boşluğunu alıyoruz.

                if (firstSlot.quantity <= secondSlotRemainingSpace) // Eğer ilk slotun miktarı, ikinci slotun kalan boşluğundan küçük veya eşitse
                {
                    itemSlots[toIndex].quantity += firstSlot.quantity; // İkinci slotun miktarını arttırıyoruz. 
                    itemSlots[fromIndex] = new ItemSlot(); // İlk slotu boşaltıyoruz.

                    OnItemsUpdated.Invoke(); // Itemlar güncellendiğinde çağrılacak olan action'ı çağırıyoruz.

                    return;
                }
            }
        }

        itemSlots[fromIndex] = secondSlot; // İlk slotu ikinci slot ile değiştiriyoruz.
        itemSlots[toIndex] = firstSlot; // İkinci slotu ilk slot ile değiştiriyoruz.

        OnItemsUpdated.Invoke(); // Itemlar güncellendiğinde çağrılacak olan action'ı çağırıyoruz.
    }
    public int GetTotalQuantity(InventoryItem item)
    {
        int totalCount = 0; // Toplam sayıyı tutacak bir değişken oluşturuyoruz ve 0 ile başlatıyoruz.

        foreach (ItemSlot itemSlot in itemSlots)
        {
            if (itemSlot.item == null) continue;
            if (itemSlot.item != item) continue; // Eğer item slotta bulunan item, aradığımız item değilse devam et.

            totalCount += itemSlot.quantity; // Eğer item slotta bulunan item, aradığımız item ise toplam sayıya ekle.
        }

        return totalCount; // Toplam sayıyı döndürüyoruz.
    }
}
