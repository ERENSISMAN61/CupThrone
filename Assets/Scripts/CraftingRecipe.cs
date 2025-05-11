using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Crafting Recipe", menuName = "Crafting/Crafting Recipe")]
public class CraftingRecipe : ScriptableObject
{
    public string itemName; // Craftlanacak eşyanın adı
    public Sprite itemIcon; // Craftlanacak eşyanın ikonu
    public List<MaterialRequirement> materials; // Gerekli materyallerin listesi
}

[System.Serializable]
public class MaterialRequirement
{
    public string materialName; // Materyalin adı
    public int amount; // Gerekli miktar
}
