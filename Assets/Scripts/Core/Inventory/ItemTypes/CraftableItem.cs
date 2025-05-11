
using System.Text;
using UnityEngine;

[CreateAssetMenu(fileName = "New Craftable Item", menuName = "Items/Craftable Item")]
public class CraftableItem : InventoryItem
{
    [Header("Crafting Item Data")]
    [SerializeField] private string useText = "Does something...";
    public override string GetInfoDisplayText()
    {
        StringBuilder builder = new StringBuilder();
        builder.Append(Name).AppendLine();
        builder.Append("<color=green>Use: ").Append(useText).Append("</color>").AppendLine();
        // builder.Append("Max Stack: ").Append(MaxStackSize).AppendLine();

        return builder.ToString();
    }
}
