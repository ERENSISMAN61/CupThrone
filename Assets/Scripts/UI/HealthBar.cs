using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public Image healthSlider; // Sağlık barı slider'ı
    public Image backHealthSlider; // Arka plan slider'ı

    public float currentHealth = 100f; // Mevcut sağlık
    public float maxHealth = 100f; // Maksimum sağlık

    private float lerpSpeed = 2f; // Lerp hızı

    private void Update()
    {
        UpdateHealthBar();
    }

    private void UpdateHealthBar()
    {
        // Sağlık oranını hesapla
        float healthFraction = currentHealth / maxHealth;

        // Sağlık barını güncelle
        healthSlider.fillAmount = Mathf.Lerp(healthSlider.fillAmount, healthFraction, Time.deltaTime * lerpSpeed);

        // Arka plan barını güncelle (daha yavaş bir şekilde)
        backHealthSlider.fillAmount = Mathf.Lerp(backHealthSlider.fillAmount, healthFraction, Time.deltaTime * (lerpSpeed / 2));
    }

    // Sağlık değerini değiştirmek için bir metod
    public void SetHealth(float newHealth)
    {
        currentHealth = Mathf.Clamp(newHealth, 0, maxHealth);
    }
}
