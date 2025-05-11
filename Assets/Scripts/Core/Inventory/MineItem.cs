
using System.Text;
using UnityEngine;

[CreateAssetMenu(fileName = "New Mine Item", menuName = "Items/Mine Item")]
public class MineItem : InventoryItem
{
    [Header("Mine Data")]
    [SerializeField] private string useText = "Mine something...";
    public override string GetInfoDisplayText()
    {
        StringBuilder builder = new StringBuilder();
        builder.Append(Name).AppendLine();
        builder.Append("<color=green>Use: ").Append(useText).Append("</color>").AppendLine();
        builder.Append("Max Stack: ").Append(MaxStackSize).AppendLine();

        return builder.ToString();
    }
}
