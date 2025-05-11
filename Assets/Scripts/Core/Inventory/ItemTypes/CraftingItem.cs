
using System.Text;
using UnityEngine;

[CreateAssetMenu(fileName = "New Crafting Item", menuName = "Items/Crafting Item")]
public class CraftingItem : InventoryItem
{
    [Header("Wood Data")]
    [SerializeField] private string useText = "Craft something...";
    public override string GetInfoDisplayText()
    {
        StringBuilder builder = new StringBuilder();
        builder.Append(Name).AppendLine();
        builder.Append("<color=green>Use: ").Append(useText).Append("</color>").AppendLine();
        // builder.Append("Max Stack: ").Append(MaxStackSize).AppendLine();

        return builder.ToString();
    }
}
