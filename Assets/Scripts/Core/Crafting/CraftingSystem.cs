using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CraftingSystem : MonoBehaviour
{
    public List<CraftingRecipe> craftingRecipes; // Craftlanabilir tariflerin listesi
    public GameObject craftingUIPanel; // Crafting ekranı paneli
    public Transform craftingListContainer; // Craftlanabilir eşyaların listesi için UI container
    public GameObject craftingItemPrefab; // Craftlanabilir eşya için UI prefab

    [SerializeField] private Inventory inventory; // Inventory referansı

    void Start()
    {
        // inventory = Object.FindFirstObjectByType<Inventory>();
        Debug.Log("Inventory referansı: " + (inventory != null ? "Var" : "Yok"));
        if (inventory == null)
        {
            Debug.LogError("Inventory bulunamadı. Lütfen bir Inventory nesnesi sahneye ekleyin.");
            return;
        }
        Debug.Log("Inventory'nin ItemContainer içeriği:");
        foreach (var slot in inventory.ItemContainer.GetAllSlots())
        {
            Debug.Log(slot.item != null ? "Slotta bulunan item: " + slot.item.name : "Boş slot");
        }
        PopulateCraftingUI();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            craftingUIPanel.SetActive(!craftingUIPanel.activeSelf);
        }
    }

    void PopulateCraftingUI()
    {
        foreach (var recipe in craftingRecipes)
        {
            GameObject itemUI = Instantiate(craftingItemPrefab, craftingListContainer);
            itemUI.GetComponentInChildren<TextMeshProUGUI>().text = recipe.itemName;
            itemUI.transform.Find("CraftingItemIcon").GetComponent<Image>().sprite = recipe.itemIcon;

            Button craftButton = itemUI.GetComponentInChildren<Button>();
            craftButton.onClick.AddListener(() => AttemptCraft(recipe));
        }
    }

    public void AttemptCraft(CraftingRecipe recipe)
    {
        Debug.Log("Envanterdeki tüm slotlar:");
        foreach (var slot in inventory.ItemContainer.GetAllSlots())
        {
            Debug.Log(slot.item != null ? "Slotta bulunan item: " + slot.item.name : "Boş slot");
        }
        
        foreach (var material in recipe.materials)
        {
            InventoryItem materialItem = FindItemByName(material.materialName);
            Debug.Log("Aranan materyal: " + material.materialName + ", Bulunan: " + (materialItem != null ? materialItem.name : "Yok"));

            if (materialItem == null || !HasSufficientMaterial(materialItem, material.amount))
            {
                Debug.Log("Yeterli materyal yok: " + material.materialName);
                return;
            }
        }


        foreach (var material in recipe.materials)
        {
            InventoryItem materialItem = FindItemByName(material.materialName);
            RemoveMaterial(materialItem, material.amount);
        }

        InventoryItem craftedItem = FindCraftedItemInResources(recipe.itemName);
        Debug.Log("Craft edilen item (Resources'tan): " + (craftedItem != null ? craftedItem.name : "Yok"));
        if (craftedItem != null)
        {
            AddCraftedItem(craftedItem);
            Debug.Log(recipe.itemName + " craftlandı ve envantere eklendi.");
        }
        else
        {
            Debug.LogError("Craft edilen item Resources'ta bulunamadı: " + recipe.itemName);
        }
    }

    bool HasSufficientMaterial(InventoryItem item, int requiredAmount)
    {
        return inventory.ItemContainer.GetTotalQuantity(item) >= requiredAmount;
    }

    void RemoveMaterial(InventoryItem item, int amount)
    {
        inventory.ItemContainer.RemoveItem(new ItemSlot(item, amount));
    }

    void AddCraftedItem(InventoryItem item)
    {
        Debug.Log("Envantere ekleniyor: " + item.name);
        var result = inventory.ItemContainer.AddItem(new ItemSlot(item, 1));
        Debug.Log(result.quantity == 0 ? "Item başarıyla eklendi." : "Item eklenemedi.");
    }

    // Item'i craft etmek için gereken materyali envanterden bul
    InventoryItem FindItemByName(string itemName)
    {
        // Inventory'nin GetAllSlots metodunu kullanarak itemName ile eşleşen ilk öğeyi bul
        var slot = inventory.ItemContainer.GetAllSlots()
            .FirstOrDefault(slot => slot.item != null && slot.item.name == itemName);
        return slot != null ? slot.item : null;
    }

    // Item craft edildikten sonra Resources klasöründe craft edilen itemi bul
    InventoryItem FindCraftedItemInResources(string itemName)
    {
        // Resources klasöründe itemName ile eşleşen bir InventoryItem bul
        var craftedItem = Resources.Load<CraftableItem>($"Items/Craftables/{itemName}");
        Debug.Log(craftedItem != null ? $"Resources'ta bulunan item: {craftedItem.name}" : $"Resources'ta {itemName} bulunamadı.");
        return craftedItem;
    }
}
