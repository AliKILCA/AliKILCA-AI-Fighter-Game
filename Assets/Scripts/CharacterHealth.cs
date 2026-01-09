using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CharacterHealth : MonoBehaviour
{
    [Header("Sağlık Ayarları")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("UI Referansları")]
    public Image healthBarFill;
    public Image healthBarBackground;

    [Header("Renk Ayarları")]
    public Color fullHealthColor = Color.green;
    public Color lowHealthColor = Color.red;
    [Range(0f, 1f)]
    public float lowHealthThreshold = 0.3f;

    [Header("Animator Ayarları")]
    [SerializeField] private string isDeadParamName = "IsDead";
    [SerializeField] private string hitTriggerName = "Hit";

    [Header("Hit Ayarları")]
    public float hitStunDuration = 0.5f;
    public bool stopMovementOnHit = true;
    
    [Header("Super Armor Ayarları")]
    [Tooltip("Saldırı sırasında hit animasyonu oynatılmasın")]
    public bool hasSuperArmorWhileAttacking = true;

    [Header("Ölüm Ayarları")]
    public float deathAnimationDuration = 2.2f;

    [Header("Debug")]
    public bool showDebugLogs = true;

    public System.Action<float, float> OnHealthChanged;
    public System.Action OnDeath;
    public System.Action OnHit;

    private Animator anim;
    private Rigidbody rb;
    private CharacterMovement movement;
    private ComboAttack comboAttack;
    private bool isDead = false;
    private bool isHitStunned = false;

    void Awake()
    {
        anim = GetComponent<Animator>();
        if (anim == null)
            anim = GetComponentInChildren<Animator>();

        rb = GetComponent<Rigidbody>();
        movement = GetComponent<CharacterMovement>();
        comboAttack = GetComponent<ComboAttack>();
    }

    void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthBar();

        if (showDebugLogs)
            Debug.Log("[Health] " + gameObject.name + " - Başlangıç Sağlık: " + currentHealth);
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);

        if (showDebugLogs)
            Debug.Log("[Health] " + gameObject.name + " - Hasar: " + damage + ", Kalan: " + currentHealth);

        UpdateHealthBar();
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // Ölmediyse hit animasyonu oynat (super armor kontrolü ile)
            TryPlayHitAnimation();
        }
    }

    private void TryPlayHitAnimation()
    {
        // Zaten hit stun'daysa tekrar tetikleme
        if (isHitStunned) return;

        // Super Armor kontrolü - saldırı sırasında hit animasyonu oynatma
        if (hasSuperArmorWhileAttacking && comboAttack != null && comboAttack.IsAttacking)
        {
            if (showDebugLogs)
                Debug.Log("[Health] " + gameObject.name + " - Super Armor! Hit animasyonu atlandı");
            return;
        }

        PlayHitAnimation();
    }

    private void PlayHitAnimation()
    {
        if (showDebugLogs)
            Debug.Log("[Health] " + gameObject.name + " - Hit animasyonu tetikleniyor!");

        OnHit?.Invoke();

        // Saldırıyı iptal et
        if (comboAttack != null && comboAttack.IsAttacking)
        {
            comboAttack.ForceStopAttack();
        }

        // Hit animasyonunu tetikle
        if (anim != null)
        {
            anim.SetTrigger(hitTriggerName);
        }

        // Hit stun başlat
        StartCoroutine(HitStunCoroutine());
    }

    private IEnumerator HitStunCoroutine()
    {
        isHitStunned = true;

        if (stopMovementOnHit)
        {
            if (rb != null && !rb.isKinematic)
            {
                rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            }
        }

        yield return new WaitForSeconds(hitStunDuration);

        isHitStunned = false;

        if (showDebugLogs)
            Debug.Log("[Health] " + gameObject.name + " - Hit stun bitti");
    }

    public void Heal(float amount)
    {
        if (isDead) return;

        currentHealth += amount;
        currentHealth = Mathf.Min(maxHealth, currentHealth);

        if (showDebugLogs)
            Debug.Log("[Health] " + gameObject.name + " - İyileşme: " + amount + ", Yeni: " + currentHealth);

        UpdateHealthBar();
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        isDead = false;
        isHitStunned = false;
        UpdateHealthBar();
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (anim != null)
        {
            anim.SetBool(isDeadParamName, false);
            anim.Rebind();
            anim.Update(0f);
        }

        if (movement != null) movement.enabled = true;
        if (comboAttack != null) comboAttack.enabled = true;
        if (rb != null) rb.isKinematic = false;

        if (showDebugLogs)
            Debug.Log("[Health] " + gameObject.name + " - Sağlık sıfırlandı: " + currentHealth);
    }

    private void UpdateHealthBar()
    {
        if (healthBarFill == null) return;

        float healthPercent = currentHealth / maxHealth;
        healthBarFill.fillAmount = healthPercent;

        if (healthPercent <= lowHealthThreshold)
        {
            healthBarFill.color = lowHealthColor;
        }
        else
        {
            healthBarFill.color = Color.Lerp(lowHealthColor, fullHealthColor,
                (healthPercent - lowHealthThreshold) / (1f - lowHealthThreshold));
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        if (showDebugLogs)
            Debug.Log("[Health] " + gameObject.name + " - ÖLDÜ!");

        if (comboAttack != null)
        {
            comboAttack.ForceStopAttack();
            comboAttack.enabled = false;
        }

        if (movement != null)
        {
            movement.enabled = false;
        }

        if (rb != null)
        {
            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            rb.isKinematic = true;
        }

        if (anim != null)
        {
            anim.SetFloat("HareketHizi", 0f);
            anim.SetBool("IsRunning", false);
            anim.SetBool("IsJumping", false);
            anim.SetInteger("ComboStep", 0);
            anim.ResetTrigger("Attack");
            anim.ResetTrigger(hitTriggerName);
        }

        StartCoroutine(PlayDeathAnimationNextFrame());

        OnDeath?.Invoke();
    }

    private IEnumerator PlayDeathAnimationNextFrame()
    {
        yield return null;

        if (anim != null)
        {
            anim.SetBool(isDeadParamName, true);
            anim.Play("Death", 0, 0f);

            if (showDebugLogs)
                Debug.Log("[Health] " + gameObject.name + " - Death animasyonu oynatıldı!");
        }

        yield return new WaitForSeconds(deathAnimationDuration);

        if (showDebugLogs)
            Debug.Log("[Health] " + gameObject.name + " - Ölüm animasyonu tamamlandı");
    }

    public float GetHealthPercent() => currentHealth / maxHealth;
    public bool IsAlive() => !isDead;
    public bool IsDead => isDead;
    public bool IsHitStunned => isHitStunned;
}