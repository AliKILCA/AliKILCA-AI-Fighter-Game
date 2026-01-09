using UnityEngine;

public class WeaponDamage : MonoBehaviour
{
    [Header("Hasar Ayarları")]
    [Tooltip("Her vuruşta verilecek hasar")]
    public float damageAmount = 5f;

    [Header("Sahip Referansı")]
    [Tooltip("Bu silahın sahibi (kendine hasar vermemek için)")]
    public Transform owner;

    [Header("Vuruş Zamanlaması (Saniye)")]
    [Tooltip("Combo 1 için vuruş penceresi başlangıcı")]
    public float combo1HitStart = 0.2f;
    [Tooltip("Combo 1 için vuruş penceresi bitişi")]
    public float combo1HitEnd = 0.5f;
    
    [Tooltip("Combo 2 için vuruş penceresi başlangıcı")]
    public float combo2HitStart = 0.15f;
    [Tooltip("Combo 2 için vuruş penceresi bitişi")]
    public float combo2HitEnd = 0.4f;
    
    [Tooltip("Combo 3 için vuruş penceresi başlangıcı")]
    public float combo3HitStart = 0.3f;
    [Tooltip("Combo 3 için vuruş penceresi bitişi")]
    public float combo3HitEnd = 0.8f;

    [Header("Debug")]
    public bool showDebugLogs = true;

    private ComboAttack comboAttack;
    private bool canDealDamage = false;
    private bool hasDealtDamageThisSwing = false;
    private float attackStartTime;
    private int lastComboStep = 0;

    void Start()
    {
        // Sahibi otomatik bul
        if (owner == null)
        {
            owner = GetComponentInParent<CharacterHealth>()?.transform;
        }

        // ComboAttack referansını al
        if (owner != null)
        {
            comboAttack = owner.GetComponent<ComboAttack>();
        }

        if (comboAttack == null && showDebugLogs)
        {
            Debug.LogWarning("[WeaponDamage] ComboAttack bulunamadı!");
        }
    }

    void Update()
    {
        if (comboAttack == null) return;

        // Saldırı durumunu kontrol et
        if (comboAttack.IsAttacking)
        {
            int currentStep = comboAttack.CurrentComboStep;

            // Yeni combo adımı başladı mı?
            if (currentStep != lastComboStep && currentStep > 0)
            {
                lastComboStep = currentStep;
                attackStartTime = Time.time;
                hasDealtDamageThisSwing = false;
                canDealDamage = false;

                if (showDebugLogs)
                    Debug.Log("[WeaponDamage] Combo " + currentStep + " başladı - Zamanlayıcı sıfırlandı");
            }

            // Zamanlama kontrolü
            if (currentStep > 0)
            {
                float elapsed = Time.time - attackStartTime;
                float hitStart = GetHitStartTime(currentStep);
                float hitEnd = GetHitEndTime(currentStep);

                // Vuruş penceresi içinde miyiz?
                bool inHitWindow = elapsed >= hitStart && elapsed <= hitEnd;

                if (inHitWindow && !canDealDamage)
                {
                    canDealDamage = true;
                    if (showDebugLogs)
                        Debug.Log("[WeaponDamage] >>> HASAR PENCERESİ AÇIK <<< (Combo " + currentStep + ", Süre: " + elapsed.ToString("F2") + "s)");
                }
                else if (!inHitWindow && canDealDamage)
                {
                    canDealDamage = false;
                    if (showDebugLogs)
                        Debug.Log("[WeaponDamage] Hasar penceresi kapandı (Combo " + currentStep + ")");
                }
            }
        }
        else
        {
            // Saldırı bitti - her şeyi sıfırla
            if (lastComboStep != 0)
            {
                if (showDebugLogs)
                    Debug.Log("[WeaponDamage] Saldırı bitti - Sıfırlanıyor");
                
                lastComboStep = 0;
                canDealDamage = false;
                hasDealtDamageThisSwing = false;
            }
        }
    }

    float GetHitStartTime(int comboStep)
    {
        switch (comboStep)
        {
            case 1: return combo1HitStart;
            case 2: return combo2HitStart;
            case 3: return combo3HitStart;
            default: return 0.2f;
        }
    }

    float GetHitEndTime(int comboStep)
    {
        switch (comboStep)
        {
            case 1: return combo1HitEnd;
            case 2: return combo2HitEnd;
            case 3: return combo3HitEnd;
            default: return 0.5f;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        TryDealDamage(other);
    }

    void OnTriggerStay(Collider other)
    {
        TryDealDamage(other);
    }

    void TryDealDamage(Collider other)
    {
        // Hasar verme aktif değilse çık
        if (!canDealDamage) 
        {
            return;
        }

        // Bu swing'de zaten hasar verdiyse çık
        if (hasDealtDamageThisSwing) 
        {
            return;
        }

        // Kendine hasar verme kontrolü
        if (owner != null)
        {
            if (other.transform == owner) return;
            if (other.transform.IsChildOf(owner)) return;
        }

        // Hedefte CharacterHealth var mı?
        CharacterHealth targetHealth = other.GetComponent<CharacterHealth>();
        if (targetHealth == null)
        {
            targetHealth = other.GetComponentInParent<CharacterHealth>();
        }

        // Hasar ver
        if (targetHealth != null && targetHealth.transform != owner)
        {
            targetHealth.TakeDamage(damageAmount);
            hasDealtDamageThisSwing = true;

            if (showDebugLogs)
                Debug.Log("[WeaponDamage] !!! VURUŞ !!! " + owner?.name + " -> " + targetHealth.gameObject.name + " = " + damageAmount + " hasar");
        }
    }

    // Debug görselleştirme
    void OnDrawGizmosSelected()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = canDealDamage ? Color.red : Color.gray;
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }
    }
}