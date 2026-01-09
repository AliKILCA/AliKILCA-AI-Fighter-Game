using UnityEngine;

public class ComboAttack : MonoBehaviour
{
    [Header("Combo Ayarları")]
    public float comboWindow = 0.8f;
    public KeyCode attackKey = KeyCode.Mouse0;

    [Header("Hareket Ayarları")]
    [Tooltip("Her saldırının ileri hareket mesafesi")]
    public float[] attackDistances = new float[] { 0.8f, 0.7f, 1.0f };
    [Tooltip("Her saldırının hareket süresi")]
    public float[] attackDurations = new float[] { 0.3f, 0.25f, 0.4f };

    [Header("Debug")]
    public bool showDebugLogs = true;

    private Animator anim;
    private Rigidbody rb;
    private CharacterHealth health;
    private int comboStep = 0;
    private float lastClickTime = 0f;
    private bool isAttacking = false;
    private bool canCombo = false;
    private float attackStartTime;

    // Hareket için
    private bool isMoving = false;
    private Vector3 moveStartPos;
    private Vector3 moveTargetPos;
    private float moveStartTime;
    private float moveDuration;

    private readonly string attackTrigger = "Attack";
    private readonly string comboStepParam = "ComboStep";

    void Awake()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        health = GetComponent<CharacterHealth>();
    }

    void Start()
    {
        if (showDebugLogs)
            Debug.Log("[ComboAttack] Başlangıç Z: " + transform.position.z);
    }

    void Update()
    {
        // Ölüyse hiçbir şey yapma
        if (health != null && !health.IsAlive())
        {
            return;
        }

        if (Input.GetKeyDown(attackKey))
        {
            Attack();
        }

        // Combo penceresi
        if (isAttacking && !canCombo)
        {
            if (Time.time - attackStartTime > 0.2f)
            {
                canCombo = true;
            }
        }

        // Combo timeout
        if (isAttacking && Time.time - lastClickTime > comboWindow && !isMoving)
        {
            ResetCombo();
        }

        // Hareket güncelleme
        if (isMoving)
        {
            UpdateMovement();
        }
    }

    public void Attack()
    {
        // Ölüyse saldırma
        if (health != null && !health.IsAlive())
        {
            return;
        }

        lastClickTime = Time.time;

        if (!isAttacking)
        {
            // İlk saldırı
            comboStep = 1;
            isAttacking = true;
            canCombo = false;
            attackStartTime = Time.time;

            if (rb != null)
            {
                rb.isKinematic = true;
            }

            anim.SetInteger(comboStepParam, comboStep);
            anim.SetTrigger(attackTrigger);

            // Hareketi başlat
            StartMovement(comboStep);

            if (showDebugLogs)
                Debug.Log("[ComboAttack] ATTACK 1 - Z: " + transform.position.z);
        }
        else if (canCombo && comboStep < 3)
        {
            // Combo devam
            comboStep++;
            canCombo = false;
            attackStartTime = Time.time;

            anim.SetInteger(comboStepParam, comboStep);

            // Hareketi başlat
            StartMovement(comboStep);

            if (showDebugLogs)
                Debug.Log("[ComboAttack] ATTACK " + comboStep + " - Z: " + transform.position.z);
        }
    }

    private void StartMovement(int step)
    {
        int index = Mathf.Clamp(step - 1, 0, attackDistances.Length - 1);
        float distance = attackDistances[index];

        int durationIndex = Mathf.Clamp(step - 1, 0, attackDurations.Length - 1);
        moveDuration = attackDurations[durationIndex];

        moveStartPos = transform.position;

        // Karakterin baktığı yöne göre hareket et
        float yRotation = transform.eulerAngles.y;
        float direction = (yRotation > 90f && yRotation < 270f) ? -1f : 1f;

        moveTargetPos = moveStartPos + new Vector3(0f, 0f, distance * direction);
        moveStartTime = Time.time;
        isMoving = true;

        if (showDebugLogs)
            Debug.Log("[ComboAttack] Hareket başladı: " + moveStartPos.z + " -> " + moveTargetPos.z + " (Yön: " + (direction > 0 ? "Z+" : "Z-") + ")");
    }

    private void UpdateMovement()
    {
        float elapsed = Time.time - moveStartTime;
        float t = Mathf.Clamp01(elapsed / moveDuration);

        // Smooth hareket (ease out)
        float smoothT = 1f - Mathf.Pow(1f - t, 2f);

        Vector3 newPos = Vector3.Lerp(moveStartPos, moveTargetPos, smoothT);
        transform.position = newPos;

        if (rb != null)
        {
            rb.position = newPos;
        }

        if (t >= 1f)
        {
            isMoving = false;
            transform.position = moveTargetPos;

            if (showDebugLogs)
                Debug.Log("[ComboAttack] Hareket bitti - Z: " + transform.position.z);
        }
    }

    private void ResetCombo()
    {
        if (!isAttacking) return;

        if (showDebugLogs)
            Debug.Log("[ComboAttack] COMBO BİTTİ - Final Z: " + transform.position.z);

        comboStep = 0;
        isAttacking = false;
        canCombo = false;
        isMoving = false;
        anim.SetInteger(comboStepParam, 0);

        if (rb != null)
        {
            rb.isKinematic = false;
        }
    }

    // Saldırıyı zorla durdur (ölüm için)
    public void ForceStopAttack()
    {
        if (showDebugLogs)
            Debug.Log("[ComboAttack] Saldırı ZORLA durduruldu!");

        comboStep = 0;
        isAttacking = false;
        canCombo = false;
        isMoving = false;

        if (anim != null)
        {
            anim.SetInteger(comboStepParam, 0);
            anim.ResetTrigger(attackTrigger);
        }

        if (rb != null)
        {
            rb.isKinematic = false;
        }
    }

    // Animation Event fonksiyonları
    public void EnableCombo() => canCombo = true;
    public void NewEvent() => canCombo = true;
    public void AttackEnd()
    {
        if (comboStep >= 3 || !canCombo)
        {
            ResetCombo();
        }
    }
    public void OnAttackAnimationEnd() => AttackEnd();

    public bool IsAttacking => isAttacking;
    public int CurrentComboStep => comboStep;
}
