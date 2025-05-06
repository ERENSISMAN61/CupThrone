using UnityEngine;

public abstract class InventoryItem : HotbarItem
{
    [Header("Item Data")]
    [SerializeField] [Min(1)] private int maxStackSize = 1; // En fazla kaç tane stacklenebilir

    public override string ColouredName 
    
    {
        get
        {
            return Name;
        }
    }

    public int MaxStackSize => maxStackSize; // Max stack sayısını döndürüyoruz. Bu sayede itemin max stack sayısını öğrenebiliriz.


}
