using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HealthBarControl : MonoBehaviour
{
    //--------------------------------------------------------------VEYSEL BASLANGIC--------------------------------------------------------------

    public float totalPlayerCurrentHealth;
    public float totalPlayerMaxHealth;

    public Image playerHealthSlider;
    public Image playerBackHealthSlider;



    public float lerpTimer; // Sağlık barının geçiş süresi
    private float chipSeed = 0.5f; // Sağlık azalma hızı

    [SerializeField] private Health healthScript;



    //--------------------------------------------------------------VEYSEL BITIS--------------------------------------------------------------







    private void LateUpdate()
    {
        CalculateHealth();
        UpdatePlayerCrewHealthUI();



    }

    private void CalculateHealth()
    {
        totalPlayerCurrentHealth = 0;
        totalPlayerMaxHealth = 0;

        // Access the Value property of NetworkVariable<int> to perform arithmetic operations
        totalPlayerCurrentHealth += healthScript.CurrentHealth.Value;
        totalPlayerMaxHealth += healthScript.MaxHealth;
    }



    public void UpdatePlayerCrewHealthUI()
    {
        float fillF = playerHealthSlider.fillAmount; // Sağlık barının doluluk oranını al
        float fillB = playerBackHealthSlider.fillAmount; // 2.Sağlık barının doluluk oranını al
        float hFraction = totalPlayerCurrentHealth / totalPlayerMaxHealth;//
        if (fillB > hFraction)
        {
            playerHealthSlider.fillAmount = hFraction;
            playerBackHealthSlider.color = Color.red;
            lerpTimer += Time.deltaTime; // Zamanı güncelle
            float percentComplete = lerpTimer / chipSeed; // Yüzde tamamlama oranını hesapla
            percentComplete *= percentComplete; // Yüzde tamamlama oranını hesapla
            playerBackHealthSlider.fillAmount = Mathf.Lerp(fillB, hFraction, percentComplete); // 2.Sağlık barını güncelle
        }
        //Canı arttırınca kullanılacak
        if (fillF < hFraction)
        {
            playerBackHealthSlider.color = Color.green; // 2.Sağlık barının rengini yeşil yap
            playerBackHealthSlider.fillAmount = hFraction; // 2.Sağlık barını güncelle
            lerpTimer += Time.deltaTime; // Zamanı güncelle
            float percentComplete = lerpTimer / chipSeed; // Yüzde tamamlama oranını hesapla
            percentComplete *= percentComplete; // Yüzde tamamlama oranını hesapla
            playerHealthSlider.fillAmount = Mathf.Lerp(fillF, playerBackHealthSlider.fillAmount, percentComplete); // Sağlık barını güncelle
        }
    }




}
