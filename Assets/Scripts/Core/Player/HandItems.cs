using UnityEngine;
using System.Collections.Generic;

public class HandItems : MonoBehaviour
{
    [SerializeField] private List<GameObject> HoldableHandItems;
    [SerializeField] private GameObject currentItemPrefab;
    [SerializeField] private GameObject bowPrefab; // Ok için prefab
    private int currentItemIndex = 0; // Mevcut nesnenin indeksini takip etmek için
    private bool hasBowInHand = false; // Ok var mı kontrolü için

    void Start()
    {
        // Başlangıçta tüm nesneleri deaktif et
        foreach (var item in HoldableHandItems)
        {
            if (item != null)
                item.SetActive(false);
        }

        // İlk nesneyi aktif et ve currentItemPrefab'a ata
        if (HoldableHandItems.Count > 0 && HoldableHandItems[0] != null)
        {
            currentItemIndex = 0;
            currentItemPrefab = HoldableHandItems[0];
            currentItemPrefab.SetActive(true);
        }
    }

    void Update()
    {
        // Mouse tekerleği kontrolü
        float scrollDelta = Input.mouseScrollDelta.y;
        Debug.Log("Scroll Delta: " + scrollDelta);

        if (scrollDelta > 0) // Yukarı kaydırma
        {
            SwitchToNextItem();
        }
        else if (scrollDelta < 0) // Aşağı kaydırma
        {
            SwitchToPreviousItem();
        }
    }

    private void SwitchToNextItem()
    {
        // Mevcut öğeyi devre dışı bırak
        if (currentItemPrefab != null)
            currentItemPrefab.SetActive(false);

        // Bir sonraki indekse geç (dairesel olarak)
        currentItemIndex = (currentItemIndex + 1) % HoldableHandItems.Count;

        // Yeni öğeyi etkinleştir ve currentItemPrefab olarak ata
        currentItemPrefab = HoldableHandItems[currentItemIndex];
        if (currentItemPrefab != null)
        {
            currentItemPrefab.SetActive(true);

            if (currentItemPrefab == bowPrefab) // İlk öğe (ok) seçildiğinde
            {
                hasBowInHand = true; // Ok var
            }
            else
            {
                hasBowInHand = false; // Ok yok
            }
        }
    }

    private void SwitchToPreviousItem()
    {
        // Mevcut öğeyi devre dışı bırak
        if (currentItemPrefab != null)
            currentItemPrefab.SetActive(false);

        // Bir önceki indekse geç (dairesel olarak)
        currentItemIndex = (currentItemIndex - 1 + HoldableHandItems.Count) % HoldableHandItems.Count;

        // Yeni öğeyi etkinleştir ve currentItemPrefab olarak ata
        currentItemPrefab = HoldableHandItems[currentItemIndex];
        if (currentItemPrefab != null)
        {
            currentItemPrefab.SetActive(true);

            if (currentItemPrefab == bowPrefab) // İlk öğe (ok) seçildiğinde
            {
                hasBowInHand = true; // Ok var
            }
            else
            {
                hasBowInHand = false; // Ok yok
            }
        }
    }

    public bool GetHasBowInHand()
    {
        return hasBowInHand; // Ok var mı kontrolü
    }
}
