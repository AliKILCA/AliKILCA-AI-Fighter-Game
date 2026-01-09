using UnityEngine;

public class CharacterMovement : MonoBehaviour
{
    [Header("Movement Ayarları")]
    public float walkSpeed = 7f;
    public float runSpeed = 12f;
    public float jumpForce = 12f;

    [Header("Zemin Kontrolü")]
    public Transform groundCheck;
    public LayerMask groundLayer;
    public float groundCheckRadius = 0.2f;

    [Header("Dövüş Ayarları")]
    public Transform opponent;

    [Header("Kontrol Tuşları")]
    public KeyCode forwardKey = KeyCode.D;
    public KeyCode backwardKey = KeyCode.A;
    public KeyCode runKey = KeyCode.LeftShift;
    public KeyCode jumpKey = KeyCode.Space;

    [Header("AI Kontrolü")]
    [Tooltip("AI tarafından kontrol ediliyorsa true yap")]
    public bool isAIControlled = false;
    private float aiInput = 0f;

    [Header("Debug")]
    public bool showDebugLogs = false;

    private Rigidbody rb;
    private Animator anim;
    private ComboAttack comboAttack;
    private CharacterHealth health;
    private bool isGrounded;
    private bool wasGrounded;
    private bool isJumping;
    private float horizontalInput;
    private bool isRunning;

    private float landingDelay = 0.1f;
    private float landingTimer = 0f;

    [Header("Animator Parametreleri")]
    [SerializeField] private string speedParamName = "HareketHizi";
    [SerializeField] private string isRunningParamName = "IsRunning";
    [SerializeField] private string isJumpingParamName = "IsJumping";
    [SerializeField] private string isGroundedParamName = "IsGrounded";

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody yok!");
            enabled = false;
            return;
        }

        anim = GetComponent<Animator>();
        if (anim == null)
        {
            Debug.LogError("Animator yok!");
            enabled = false;
            return;
        }

        comboAttack = GetComponent<ComboAttack>();
        health = GetComponent<CharacterHealth>();
    }

    void Start()
    {
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.isKinematic = false;
        rb.constraints = RigidbodyConstraints.FreezePositionX |
                         RigidbodyConstraints.FreezeRotation;

        wasGrounded = true;
        isGrounded = true;
    }

    void Update()
    {
        // Ölü veya hit stun kontrolü
        if (health != null)
        {
            if (!health.IsAlive() || health.IsHitStunned)
            {
                horizontalInput = 0f;
                isRunning = false;
                UpdateAnimator();
                return;
            }
        }

        // Saldırı sırasında hareket girişini engelle
        if (comboAttack != null && comboAttack.IsAttacking)
        {
            horizontalInput = 0f;
            isRunning = false;
        }
        else
        {
            // AI kontrolü mü yoksa oyuncu kontrolü mü?
            if (isAIControlled)
            {
                horizontalInput = aiInput;
                isRunning = false;
            }
            else
            {
                // Oyuncu kontrolü - Inspector'dan ayarlanan tuşları kullan
                horizontalInput = 0f;
                if (Input.GetKey(forwardKey))
                    horizontalInput = 1f;
                else if (Input.GetKey(backwardKey))
                    horizontalInput = -1f;

                // Karakterin baktığı yöne göre koşma kontrolü
                float yRotation = transform.eulerAngles.y;
                bool facingNegativeZ = (yRotation > 90f && yRotation < 270f);

                if (facingNegativeZ)
                {
                    isRunning = Input.GetKey(runKey) && horizontalInput < 0;
                }
                else
                {
                    isRunning = Input.GetKey(runKey) && horizontalInput > 0;
                }
            }
        }

        // Zıplama kontrolü (sadece oyuncu için)
        if (!isAIControlled)
        {
            bool canJump = comboAttack == null || !comboAttack.IsAttacking;
            if (Input.GetKeyDown(jumpKey) && isGrounded && !isJumping && canJump)
            {
                Jump();
            }
        }

        UpdateAnimator();
    }

    void FixedUpdate()
    {
        // Zemin kontrolü
        if (groundCheck != null)
        {
            wasGrounded = isGrounded;
            isGrounded = Physics.CheckSphere(
                groundCheck.position,
                groundCheckRadius,
                groundLayer
            );

            if (isJumping)
            {
                if (isGrounded && !wasGrounded)
                {
                    OnLanded();
                }
                else if (isGrounded)
                {
#if UNITY_6000_0_OR_NEWER
                    float yVel = rb.linearVelocity.y;
#else
                    float yVel = rb.velocity.y;
#endif
                    if (yVel <= 0.1f)
                    {
                        landingTimer += Time.fixedDeltaTime;
                        if (landingTimer >= landingDelay)
                        {
                            OnLanded();
                        }
                    }
                }
            }
            else
            {
                landingTimer = 0f;
            }
        }

        // Saldırı sırasında hareket yapma
        if (comboAttack != null && comboAttack.IsAttacking)
        {
            return;
        }

        MoveHorizontal(horizontalInput);
    }

    private void MoveHorizontal(float input)
    {
        float currentSpeed = isRunning ? runSpeed : walkSpeed;
        float targetVelocityZ = input * currentSpeed;

#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, targetVelocityZ);
#else
        rb.velocity = new Vector3(0f, rb.velocity.y, targetVelocityZ);
#endif
    }

    private void Jump()
    {
        isJumping = true;
        landingTimer = 0f;

#if UNITY_6000_0_OR_NEWER
        Vector3 vel = rb.linearVelocity;
        rb.linearVelocity = new Vector3(vel.x, 0f, vel.z);
#else
        Vector3 vel = rb.velocity;
        rb.velocity = new Vector3(vel.x, 0f, vel.z);
#endif

        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    private void OnLanded()
    {
        isJumping = false;
        landingTimer = 0f;
    }

    private void UpdateAnimator()
    {
        // Karakterin baktığı yöne göre animasyon değerini ayarla
        float yRotation = transform.eulerAngles.y;
        bool facingNegativeZ = (yRotation > 90f && yRotation < 270f);

        float animSpeed = facingNegativeZ ? -horizontalInput : horizontalInput;

        anim.SetFloat(speedParamName, animSpeed);
        anim.SetBool(isRunningParamName, isRunning);
        anim.SetBool(isJumpingParamName, isJumping);
        anim.SetBool(isGroundedParamName, isGrounded);
    }

    // AI tarafından çağrılacak metod
    public void SetAIInput(float input)
    {
        aiInput = input;
    }

    // Hareket durdurmak için
    public void StopMovement()
    {
        aiInput = 0f;
        horizontalInput = 0f;
        if (rb != null && !rb.isKinematic)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}