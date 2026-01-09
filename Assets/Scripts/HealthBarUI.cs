using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [Header("Player 1 (Sol Üst)")]
    public CharacterHealth player1Health;
    public Image player1Fill;

    [Header("Player 2 (Sağ Üst)")]
    public CharacterHealth player2Health;
    public Image player2Fill;

    [Header("Renk Ayarları")]
    public Color fullHealthColor = Color.green;
    public Color lowHealthColor = Color.red;
    [Range(0f, 1f)]
    public float lowHealthThreshold = 0.3f;

    void Start()
    {
        // Başlangıçta barları güncelle
        UpdateHealthBar(player1Health, player1Fill);
        UpdateHealthBar(player2Health, player2Fill);

        // Event'lere abone ol
        if (player1Health != null)
            player1Health.OnHealthChanged += (current, max) => UpdateHealthBar(player1Health, player1Fill);

        if (player2Health != null)
            player2Health.OnHealthChanged += (current, max) => UpdateHealthBar(player2Health, player2Fill);
    }

    void Update()
    {
        // Her frame güncelle (event çalışmazsa diye)
        UpdateHealthBar(player1Health, player1Fill);
        UpdateHealthBar(player2Health, player2Fill);
    }

    private void UpdateHealthBar(CharacterHealth health, Image fill)
    {
        if (health == null || fill == null) return;

        float percent = health.currentHealth / health.maxHealth;
        fill.fillAmount = percent;

        // Renk geçişi
        if (percent <= lowHealthThreshold)
        {
            fill.color = lowHealthColor;
        }
        else
        {
            fill.color = Color.Lerp(lowHealthColor, fullHealthColor,
                (percent - lowHealthThreshold) / (1f - lowHealthThreshold));
        }
    }
}
