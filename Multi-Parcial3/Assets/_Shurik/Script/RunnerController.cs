using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Rigidbody))]
public class RunnerController : NetworkBehaviour
{
    [Header("Movement")]
    public float baseSpeed = 8f;
    private float currentSpeed;

    [Header("Jump & Gravity")]
    public float jumpForce = 12f;
    public float fallMultiplier = 2.5f; // Hace que caiga más rápido
    public float lowJumpMultiplier = 2f; // Permite saltos cortos si sueltas el botón

    [Header("Coyote Time")]
    public float coyoteTimeDuration = 0.2f;
    private float coyoteTimeCounter;

    [Header("Dash (Riesgo/Recompensa)")]
    public float dashForce = 15f;
    public float dashPenaltyDuration = 3f;
    public float speedPenaltyPercentage = 0.7f; // 30% más lento tras usar dash
    private bool isDashed = false;
    private float penaltyTimer;

    [Header("Ground Detection")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.3f;
    public LayerMask groundLayer;
    private bool isGrounded;

    [Header("Visual Feedback")]
    public MeshRenderer playerRenderer; // Arrastra aquí el modelo 3D de tu personaje
    public Color normalColor = Color.white;
    public Color exhaustedColor = Color.red; // Color de penalización

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        currentSpeed = baseSpeed;

        // Usamos el Bypass
        if (!HasControl())
        {
            rb.isKinematic = true;
        }
    }


    void Update()
    {
        if (!HasControl()) return;

        CheckGrounded();
        HandleJump();
        HandleDash();
        ApplyCustomGravity();
    }

    void FixedUpdate()
    {
        if (!HasControl()) return;

        // CORRECCIÓN: ¡Sin multiplicar por el tiempo! 
        Vector3 movement = new Vector3(currentSpeed, rb.linearVelocity.y, 0f);
        rb.linearVelocity = movement;

    }
    private bool HasControl()
    {
        // Si el NetworkManager está activo, respetamos las reglas de red
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            return IsSpawned && IsOwner;
        }
        // Si no hay red (estás probando tú solo), asumimos control total
        return true;
    }

    private void CheckGrounded()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);

        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTimeDuration; // Resetear Coyote Time
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime; // Restar tiempo al estar en el aire
        }
    }

    private void HandleJump()
    {
        if (Input.GetButtonDown("Jump") && coyoteTimeCounter > 0f)
        {
            // Resetear velocidad en Y para evitar sumas raras si resalta muy rápido
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            coyoteTimeCounter = 0f; // Consumir el salto
        }
    }

    private void HandleDash()
    {
        // Lógica de Penalización (Recuperando velocidad)
        if (isDashed)
        {
            penaltyTimer -= Time.deltaTime;

            // EFECTO VISUAL: Pintarlo de color cansado
            if (playerRenderer != null) playerRenderer.material.color = exhaustedColor;

            if (penaltyTimer <= 0)
            {
                isDashed = false;
                currentSpeed = baseSpeed;

                // EFECTO VISUAL: Regresar al color normal
                if (playerRenderer != null) playerRenderer.material.color = normalColor;
            }
        }

        // Ejecutar Dash
        if (Input.GetKeyDown(KeyCode.LeftShift) && !isDashed)
        {
            rb.AddForce(Vector3.right * dashForce, ForceMode.Impulse);
            isDashed = true;
            penaltyTimer = dashPenaltyDuration;
            currentSpeed = baseSpeed * speedPenaltyPercentage;
        }
    }

    private void ApplyCustomGravity()
    {
        // Mejorar el "Game Feel" del salto
        if (rb.linearVelocity.y < 0)
        {
            rb.linearVelocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1) * Time.deltaTime;
        }
        else if (rb.linearVelocity.y > 0 && !Input.GetButton("Jump"))
        {
            rb.linearVelocity += Vector3.up * Physics.gravity.y * (lowJumpMultiplier - 1) * Time.deltaTime;
        }
    }
}