using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody))]
public class RunnerController : NetworkBehaviour
{
    [Header("Movement")]
    public float baseSpeed = 8f;
    private float currentSpeed;

    [Header("Jump & Gravity")]
    public float jumpForce = 12f;
    public float fallMultiplier = 2.5f;
    public float lowJumpMultiplier = 2f;

    [Header("Coyote Time")]
    public float coyoteTimeDuration = 0.2f;
    private float coyoteTimeCounter;

    [Header("Dash (Riesgo/Recompensa)")]
    public float dashForce = 15f;
    public float dashPenaltyDuration = 3f;
    public float speedPenaltyPercentage = 0.7f;
    private bool isDashed = false;
    private float penaltyTimer;

    [Header("Ground Detection")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.3f;
    public LayerMask groundLayer;
    private bool isGrounded;

    [Header("Visual Feedback")]
    public MeshRenderer playerRenderer;
    public Color normalColor = Color.white;
    public Color exhaustedColor = Color.red;

    // AÑADIDO: Variable de red para que todos vean quién está penalizado
    public NetworkVariable<bool> isExhausted = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private Rigidbody rb;

    public override void OnNetworkSpawn()
    {
        // Al nacer en el menú, lo CONGELAMOS para que no caiga al infinito
        if (TryGetComponent(out Rigidbody rbLocal))
        {
            rbLocal.isKinematic = true;
        }
    }

    // Estas funciones le avisan al jugador cuando Unity cambia de pantalla
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // IMPORTANTE: Pon aquí el nombre exacto de tu escena de juego
        if (scene.name == "Level")
        {
            // 1. Lo ponemos en la línea de salida
            transform.position = new Vector3(-2f, 2f, transform.position.z);

            // 2. Le quitamos la velocidad acumulada
            if (TryGetComponent(out Rigidbody rbLocal))
            {
                rbLocal.linearVelocity = Vector3.zero;

                // 3. Si soy el dueño de este personaje, lo descongelo para jugar
                if (HasControl())
                {
                    rbLocal.isKinematic = false;
                }
            }
        }
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        currentSpeed = baseSpeed;

        if (!HasControl())
        {
            rb.isKinematic = true;
        }
    }

    void Update()
    {
        if (!HasControl() || rb.isKinematic) return;

        // Actualizar el color visual para TODOS los clientes
        if (playerRenderer != null)
        {
            playerRenderer.material.color = isExhausted.Value ? exhaustedColor : normalColor;
        }

        // Botón de reinicio manual (Suicidio táctico si se atora)
        if (HasControl() && Input.GetKeyDown(KeyCode.R))
        {
            RequestSuicideServerRpc();
        }

        CheckGrounded();
        HandleJump();
        HandleDash();
        ApplyCustomGravity();
    }

    void FixedUpdate()
    {
        // AÑADIDO: Si no tengo control, O si estoy congelado, no aplico velocidad
        if (!HasControl() || rb.isKinematic) return;

        Vector3 movement = new Vector3(currentSpeed, rb.linearVelocity.y, 0f);
        rb.linearVelocity = movement;
    }

    private bool HasControl()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            return IsSpawned && IsOwner;
        }
        return true;
    }

    private void CheckGrounded()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);

        if (isGrounded) coyoteTimeCounter = coyoteTimeDuration;
        else coyoteTimeCounter -= Time.deltaTime;
    }

    private void HandleJump()
    {
        if (Input.GetButtonDown("Jump") && coyoteTimeCounter > 0f)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            coyoteTimeCounter = 0f;
        }
    }

    private void HandleDash()
    {
        if (isDashed)
        {
            penaltyTimer -= Time.deltaTime;

            if (penaltyTimer <= 0)
            {
                isDashed = false;
                currentSpeed = baseSpeed;
                isExhausted.Value = false; // Avisar a la red que ya me recuperé
            }
        }

        if (Input.GetKeyDown(KeyCode.LeftShift) && !isDashed)
        {
            rb.AddForce(Vector3.right * dashForce, ForceMode.Impulse);
            isDashed = true;
            penaltyTimer = dashPenaltyDuration;
            currentSpeed = baseSpeed * speedPenaltyPercentage;
            isExhausted.Value = true; // Avisar a la red que pagué el precio del dash
        }
    }

    private void ApplyCustomGravity()
    {
        if (rb.linearVelocity.y < 0)
        {
            rb.linearVelocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1) * Time.deltaTime;
        }
        else if (rb.linearVelocity.y > 0 && !Input.GetButton("Jump"))
        {
            rb.linearVelocity += Vector3.up * Physics.gravity.y * (lowJumpMultiplier - 1) * Time.deltaTime;
        }
    }

    [ServerRpc]
    public void RequestSuicideServerRpc()
    {
        // El jugador le pide al servidor que lo mate para dejar una plataforma
        GroupCameraTracker tracker = FindFirstObjectByType<GroupCameraTracker>();
        if (tracker != null)
        {
            tracker.ExecuteConstructiveDeath(this);
        }
    }

    [ClientRpc]
    public void RespawnClientRpc(Vector3 newPosition)
    {
        // 1. Apagamos físicas un milisegundo para un teletransporte limpio
        rb.isKinematic = true;

        // 2. Lo movemos al cielo y le quitamos la inercia de caída vieja
        transform.position = newPosition;
        rb.linearVelocity = Vector3.zero;

        // 3. Reiniciar penalizaciones del Dash si murió estando cansado
        currentSpeed = baseSpeed;
        isDashed = false;
        if (IsOwner && isExhausted.Value) isExhausted.Value = false;

        // 4. Lo volvemos a encender para que caiga a la pista
        if (HasControl())
        {
            rb.isKinematic = false;
        }
    }
}