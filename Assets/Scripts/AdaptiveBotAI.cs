using UnityEngine;
using System.Collections.Generic;

public class AdaptiveBotAI : MonoBehaviour
{
    [Header("Referanslar")]
    public Transform player;
    public CharacterHealth myHealth;
    public CharacterHealth playerHealth;
    public ComboAttack myComboAttack;
    public CharacterMovement myMovement;

    [Header("AI Ayarları")]
    public float attackRange = 2.5f;
    public float safeDistance = 4f;
    public float decisionInterval = 0.3f;

    [Header("Öğrenme Ayarları")]
    [Tooltip("Oyuncunun kalıplarını ne kadar hatırlasın")]
    public int memorySize = 50;
    [Tooltip("Öğrenme hızı (0-1)")]
    [Range(0.01f, 0.5f)]
    public float learningRate = 0.1f;

    [Header("Debug")]
    public bool showDebugLogs = true;

    // Oyuncu kalıplarını kaydet
    private List<PlayerAction> playerHistory = new List<PlayerAction>();
    private float lastDecisionTime;
    private float lastPlayerZ;
    private bool wasPlayerAttacking;
    private float lastPlayerAttackTime;

    // Öğrenilen bilgiler
    private float playerAggressiveness = 0.5f;      // 0=pasif, 1=agresif
    private float playerAttackFrequency = 0.5f;     // Saldırı sıklığı
    private float preferredAttackDistance = 2f;      // Oyuncunun tercih ettiği mesafe
    private float playerPatternPredictability = 0f;  // Oyuncu ne kadar tahmin edilebilir

    // Bot stratejisi
    private float botAggressiveness = 0.5f;         // Botun agresiflik seviyesi
    private float botCaution = 0.5f;                // Botun dikkatlilik seviyesi

    private enum BotState { Idle, Approaching, Attacking, Retreating, Waiting }
    private BotState currentState = BotState.Idle;

    private struct PlayerAction
    {
        public float distance;
        public bool attacked;
        public float time;
        public float playerHealth;
        public float botHealth;
    }

    void Start()
    {
        if (myMovement != null)
        {
            myMovement.isAIControlled = true;
        }

        if (player != null)
        {
            lastPlayerZ = player.position.z;
        }

        // Event'lere abone ol
        if (myHealth != null)
        {
            myHealth.OnHealthChanged += OnBotDamageTaken;
        }

        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += OnPlayerDamageTaken;
        }

        if (showDebugLogs)
            Debug.Log("[AdaptiveAI] Başlatıldı - Öğrenmeye hazır!");
    }

    void Update()
    {
        // Ölüyse hiçbir şey yapma
        if (myHealth == null || !myHealth.IsAlive())
        {
            if (myMovement != null) myMovement.SetAIInput(0f);
            return;
        }

        if (player == null) return;

        // Oyuncuyu gözlemle ve kaydet
        ObservePlayer();

        // Belirli aralıklarla karar ver
        if (Time.time - lastDecisionTime >= decisionInterval)
        {
            AnalyzePlayerPatterns();
            MakeDecision();
            lastDecisionTime = Time.time;
        }
    }

    void ObservePlayer()
    {
        ComboAttack playerCombo = player.GetComponent<ComboAttack>();
        bool isPlayerAttacking = playerCombo != null && playerCombo.IsAttacking;

        // Oyuncu saldırı başlattıysa kaydet
        if (isPlayerAttacking && !wasPlayerAttacking)
        {
            float distance = Mathf.Abs(transform.position.z - player.position.z);

            PlayerAction action = new PlayerAction
            {
                distance = distance,
                attacked = true,
                time = Time.time,
                playerHealth = playerHealth != null ? playerHealth.currentHealth : 100f,
                botHealth = myHealth != null ? myHealth.currentHealth : 100f
            };

            playerHistory.Add(action);

            // Hafıza limitini aşma
            if (playerHistory.Count > memorySize)
            {
                playerHistory.RemoveAt(0);
            }

            // Saldırı sıklığını güncelle
            float timeSinceLastAttack = Time.time - lastPlayerAttackTime;
            if (timeSinceLastAttack > 0 && timeSinceLastAttack < 10f)
            {
                playerAttackFrequency = Mathf.Lerp(playerAttackFrequency, 1f / timeSinceLastAttack, learningRate);
            }
            lastPlayerAttackTime = Time.time;

            if (showDebugLogs)
                Debug.Log("[AdaptiveAI] Oyuncu saldırdı! Mesafe: " + distance.ToString("F2") + 
                          ", Sıklık: " + playerAttackFrequency.ToString("F2"));
        }

        wasPlayerAttacking = isPlayerAttacking;
        lastPlayerZ = player.position.z;
    }

    void AnalyzePlayerPatterns()
    {
        if (playerHistory.Count < 3) return;

        // Son aksiyonları analiz et
        int recentCount = Mathf.Min(15, playerHistory.Count);
        float totalDistance = 0f;
        int attackCount = 0;
        List<float> attackDistances = new List<float>();

        for (int i = playerHistory.Count - recentCount; i < playerHistory.Count; i++)
        {
            if (playerHistory[i].attacked)
            {
                totalDistance += playerHistory[i].distance;
                attackCount++;
                attackDistances.Add(playerHistory[i].distance);
            }
        }

        if (attackCount > 0)
        {
            // Tercih edilen saldırı mesafesini öğren
            float newPreferredDistance = totalDistance / attackCount;
            preferredAttackDistance = Mathf.Lerp(preferredAttackDistance, newPreferredDistance, learningRate);

            // Tahmin edilebilirlik hesapla (mesafelerin varyansı)
            if (attackDistances.Count > 2)
            {
                float variance = CalculateVariance(attackDistances);
                playerPatternPredictability = Mathf.Lerp(playerPatternPredictability, 
                    1f - Mathf.Clamp01(variance / 5f), learningRate);
            }
        }

        // Agresiflik hesapla (son 10 saniyede kaç saldırı)
        float recentAttacks = 0;
        float timeWindow = 10f;
        for (int i = playerHistory.Count - 1; i >= 0; i--)
        {
            if (Time.time - playerHistory[i].time > timeWindow) break;
            if (playerHistory[i].attacked) recentAttacks++;
        }
        
        float newAggressiveness = Mathf.Clamp01(recentAttacks / 5f); // 5 saldırı = max agresif
        playerAggressiveness = Mathf.Lerp(playerAggressiveness, newAggressiveness, learningRate);

        // Bot stratejisini güncelle
        UpdateBotStrategy();

        if (showDebugLogs && Time.frameCount % 120 == 0)
        {
            Debug.Log("[AdaptiveAI] === Öğrenilen Bilgiler ===" +
                      "\nOyuncu Agresifliği: " + playerAggressiveness.ToString("F2") +
                      "\nTercih Mesafesi: " + preferredAttackDistance.ToString("F2") +
                      "\nTahmin Edilebilirlik: " + playerPatternPredictability.ToString("F2") +
                      "\nBot Agresifliği: " + botAggressiveness.ToString("F2") +
                      "\nBot Dikkati: " + botCaution.ToString("F2"));
        }
    }

    void UpdateBotStrategy()
    {
        // Oyuncu agresifse → Bot daha dikkatli ve savunmacı olsun
        if (playerAggressiveness > 0.6f)
        {
            botAggressiveness = Mathf.Lerp(botAggressiveness, 0.3f, learningRate);
            botCaution = Mathf.Lerp(botCaution, 0.8f, learningRate);
        }
        // Oyuncu pasifse → Bot daha agresif olsun
        else if (playerAggressiveness < 0.3f)
        {
            botAggressiveness = Mathf.Lerp(botAggressiveness, 0.8f, learningRate);
            botCaution = Mathf.Lerp(botCaution, 0.3f, learningRate);
        }
        // Dengeli oyuncu → Bot da dengeli
        else
        {
            botAggressiveness = Mathf.Lerp(botAggressiveness, 0.5f, learningRate);
            botCaution = Mathf.Lerp(botCaution, 0.5f, learningRate);
        }

        // Oyuncu tahmin edilebilirse → Fırsatları değerlendir
        if (playerPatternPredictability > 0.7f)
        {
            botAggressiveness = Mathf.Min(botAggressiveness + 0.1f, 1f);
        }
    }

    void MakeDecision()
    {
        // Saldırı sırasında karar verme
        if (myComboAttack != null && myComboAttack.IsAttacking)
        {
            return;
        }

        float distance = Mathf.Abs(transform.position.z - player.position.z);
        float direction = Mathf.Sign(player.position.z - transform.position.z);

        // Oyuncu saldırıyor mu?
        ComboAttack playerCombo = player.GetComponent<ComboAttack>();
        bool isPlayerAttacking = playerCombo != null && playerCombo.IsAttacking;

        // === ADAPTIVE KARAR VERME ===

        // Oyuncu saldırıyorsa → Geri çekil (öğrenilmiş tepki)
        if (isPlayerAttacking && distance < safeDistance)
        {
            currentState = BotState.Retreating;
            myMovement.SetAIInput(-direction);
            
            if (showDebugLogs)
                Debug.Log("[AdaptiveAI] Oyuncu saldırıyor, geri çekiliyorum!");
            return;
        }

        // Rastgelelik ekle (tahmin edilemez olmak için)
        float randomFactor = Random.value;

        // DURUM 1: Saldırı menzilinde
        if (distance <= attackRange)
        {
            // Agresifliğe göre saldır veya bekle
            if (randomFactor < botAggressiveness)
            {
                currentState = BotState.Attacking;
                myMovement.SetAIInput(0f);
                myComboAttack.Attack();
                
                if (showDebugLogs)
                    Debug.Log("[AdaptiveAI] Saldırıyorum! (Agresiflik: " + botAggressiveness.ToString("F2") + ")");
            }
            else if (randomFactor < botCaution)
            {
                // Dikkatli ol, biraz geri çekil
                currentState = BotState.Retreating;
                myMovement.SetAIInput(-direction * 0.5f);
            }
            else
            {
                currentState = BotState.Waiting;
                myMovement.SetAIInput(0f);
            }
        }
        // DURUM 2: Oyuncunun tercih ettiği mesafede (tehlikeli bölge)
        else if (distance <= preferredAttackDistance + 1f && distance > attackRange)
        {
            // Dikkatli yaklaş veya bekle
            if (botCaution > 0.5f && randomFactor < botCaution)
            {
                // Dikkatli - yavaş yaklaş
                currentState = BotState.Approaching;
                myMovement.SetAIInput(direction * 0.5f);
            }
            else if (randomFactor < botAggressiveness)
            {
                // Agresif - hızlı yaklaş
                currentState = BotState.Approaching;
                myMovement.SetAIInput(direction);
            }
            else
            {
                currentState = BotState.Waiting;
                myMovement.SetAIInput(0f);
            }
        }
        // DURUM 3: Uzak mesafe
        else
        {
            // Yaklaş
            currentState = BotState.Approaching;
            myMovement.SetAIInput(direction);
        }
    }

    // Bot hasar aldığında
    void OnBotDamageTaken(float current, float max)
    {
        // Hasar alınca daha dikkatli ol
        botCaution = Mathf.Clamp01(botCaution + 0.15f);
        botAggressiveness = Mathf.Clamp01(botAggressiveness - 0.1f);

        // Oyuncu agresifliğini artır
        playerAggressiveness = Mathf.Clamp01(playerAggressiveness + 0.1f);

        if (showDebugLogs)
            Debug.Log("[AdaptiveAI] Hasar aldım! Daha dikkatli oluyorum. Dikkat: " + botCaution.ToString("F2"));
    }

    // Oyuncu hasar aldığında (biz vurduk)
    void OnPlayerDamageTaken(float current, float max)
    {
        // Başarılı saldırı - bu strateji işe yarıyor
        botAggressiveness = Mathf.Clamp01(botAggressiveness + 0.05f);

        if (showDebugLogs)
            Debug.Log("[AdaptiveAI] Oyuncuya vurdum! Agresiflik: " + botAggressiveness.ToString("F2"));
    }

    float CalculateVariance(List<float> values)
    {
        if (values.Count < 2) return 0f;

        float mean = 0f;
        foreach (float v in values) mean += v;
        mean /= values.Count;

        float variance = 0f;
        foreach (float v in values)
        {
            variance += (v - mean) * (v - mean);
        }
        return variance / values.Count;
    }

    void OnDestroy()
    {
        if (myHealth != null)
            myHealth.OnHealthChanged -= OnBotDamageTaken;

        if (playerHealth != null)
            playerHealth.OnHealthChanged -= OnPlayerDamageTaken;
    }

    // Debug için mevcut durumu göster
    void OnGUI()
    {
        if (!showDebugLogs) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label("=== Adaptive AI Debug ===");
        GUILayout.Label("Durum: " + currentState.ToString());
        GUILayout.Label("Oyuncu Agresifliği: " + playerAggressiveness.ToString("F2"));
        GUILayout.Label("Tercih Mesafesi: " + preferredAttackDistance.ToString("F2"));
        GUILayout.Label("Bot Agresifliği: " + botAggressiveness.ToString("F2"));
        GUILayout.Label("Bot Dikkati: " + botCaution.ToString("F2"));
        GUILayout.Label("Hafıza: " + playerHistory.Count + "/" + memorySize);
        GUILayout.EndArea();
    }
}
