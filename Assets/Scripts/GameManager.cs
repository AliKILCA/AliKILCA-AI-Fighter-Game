using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
{
    [Header("Karakterler")]
    public Transform player1;
    public Transform bot;

    [Header("Başlangıç Pozisyonları (Manuel Gir)")]
    public Vector3 player1StartPos = new Vector3(-0.17f, 0.043f, -6.63f);
    public Vector3 botStartPos = new Vector3(-0.17f, 0f, 0f);

    [Header("Başlangıç Rotasyonları")]
    public Vector3 player1StartRot = new Vector3(0f, 0f, 0f);
    public Vector3 botStartRot = new Vector3(0f, 180f, 0f);

    [Header("Yeniden Başlatma Ayarları")]
    public float restartDelay = 3f;

    [Header("Debug")]
    public bool showDebugLogs = true;

    private CharacterHealth player1Health;
    private CharacterHealth botHealth;
    private bool isRestarting = false;

    void Start()
    {
        if (player1 != null)
        {
            player1Health = player1.GetComponent<CharacterHealth>();
            player1Health.OnDeath += OnPlayerDeath;
        }

        if (bot != null)
        {
            botHealth = bot.GetComponent<CharacterHealth>();
            botHealth.OnDeath += OnBotDeath;
        }

        if (showDebugLogs)
            Debug.Log("[GameManager] Başlatıldı!");
    }

    void OnPlayerDeath()
    {
        if (showDebugLogs)
            Debug.Log("[GameManager] Player 1 öldü! Bot kazandı!");
        StartCoroutine(RestartRound());
    }

    void OnBotDeath()
    {
        if (showDebugLogs)
            Debug.Log("[GameManager] Bot öldü! Player 1 kazandı!");
        StartCoroutine(RestartRound());
    }

    IEnumerator RestartRound()
    {
        if (isRestarting) yield break;
        isRestarting = true;

        if (showDebugLogs)
            Debug.Log("[GameManager] " + restartDelay + " saniye sonra yeniden başlıyor...");

        yield return new WaitForSeconds(restartDelay);

        // Player 1'i sıfırla
        ResetCharacter(player1, player1Health, player1StartPos, player1StartRot);

        // Bot'u sıfırla
        ResetCharacter(bot, botHealth, botStartPos, botStartRot);

        isRestarting = false;

        if (showDebugLogs)
            Debug.Log("[GameManager] Round yeniden başladı!");
    }

    void ResetCharacter(Transform character, CharacterHealth health, Vector3 startPos, Vector3 startRot)
    {
        if (character == null) return;

        if (showDebugLogs)
            Debug.Log("[GameManager] " + character.name + " sıfırlanıyor -> Pos: " + startPos);

        // Rigidbody'yi önce kinematic yap (pozisyon değişikliği için)
        Rigidbody rb = character.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Pozisyonu sıfırla
        character.position = startPos;
        character.eulerAngles = startRot;

        // Rigidbody'yi tekrar aç
        if (rb != null)
        {
            rb.isKinematic = false;
        }

        // Sağlığı sıfırla
        if (health != null)
        {
            health.ResetHealth();
        }

        // Movement'ı aç
        CharacterMovement movement = character.GetComponent<CharacterMovement>();
        if (movement != null)
        {
            movement.enabled = true;
        }

        // ComboAttack'ı aç
        ComboAttack combo = character.GetComponent<ComboAttack>();
        if (combo != null)
        {
            combo.enabled = true;
        }
    }

    // Inspector'da butona basınca pozisyonları kaydet
    [ContextMenu("Mevcut Pozisyonları Kaydet")]
    public void SaveCurrentPositions()
    {
        if (player1 != null)
        {
            player1StartPos = player1.position;
            player1StartRot = player1.eulerAngles;
            Debug.Log("Player 1 pozisyonu kaydedildi: " + player1StartPos);
        }

        if (bot != null)
        {
            botStartPos = bot.position;
            botStartRot = bot.eulerAngles;
            Debug.Log("Bot pozisyonu kaydedildi: " + botStartPos);
        }
    }

    void OnDestroy()
    {
        if (player1Health != null)
            player1Health.OnDeath -= OnPlayerDeath;

        if (botHealth != null)
            botHealth.OnDeath -= OnBotDeath;
    }
}