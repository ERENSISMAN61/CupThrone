using System.Reflection;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public abstract class ItemSlotUI : MonoBehaviour, IDropHandler
{
    [SerializeField] protected Image itemIconImage = null;

    public int SlotIndex { get; private set; }

    public abstract HotbarItem SlotItem { get; set; }

    private void OnEnable() => UpdateSlotUI();

    protected virtual void Start()
    {
        SlotIndex = transform.GetSiblingIndex(); // SlotIndex'i alıyoruz. Bu sayede hangi slotta olduğumuzu öğreniyoruz.
        UpdateSlotUI(); // SlotUI'yi güncelliyoruz. Bu sayede slotun içeriğini güncelliyoruz.

    }

    public abstract void OnDrop(PointerEventData eventData); // OnDrop metodunu abstract olarak tanımlıyoruz. Bu sayede her slot için farklı bir OnDrop metodu yazabiliriz.

    public abstract void UpdateSlotUI(); // UpdateSlotUI metodunu abstract olarak tanımlıyoruz. Bu sayede her slot için farklı bir UpdateSlotUI metodu yazabiliriz.
    
    protected virtual void EnableSlotUI(bool enable) => itemIconImage.enabled = enable; // itemIconImage'i enable ediyoruz. Bu sayede slotun içeriğini güncelliyoruz.

}
