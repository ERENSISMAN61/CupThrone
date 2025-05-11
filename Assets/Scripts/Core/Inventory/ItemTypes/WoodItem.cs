
using System.Text;
using UnityEngine;

[CreateAssetMenu(fileName = "New Wood Item", menuName = "Items/Wood Item")]
public class WoodItem : InventoryItem
{
    [Header("Wood Data")]
    [SerializeField] private string useText = "Wood something...";
    public override string GetInfoDisplayText()
    {
        StringBuilder builder = new StringBuilder();
        builder.Append(Name).AppendLine();
        builder.Append("<color=green>Use: ").Append(useText).Append("</color>").AppendLine();
        // builder.Append("Max Stack: ").Append(MaxStackSize).AppendLine();

        return builder.ToString();
    }
}
