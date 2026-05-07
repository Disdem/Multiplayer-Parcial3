using UnityEngine;
using Unity.Netcode;

public class PlayerRunner : NetworkBehaviour
{
    public NetworkVariable<bool> isAlive = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private CharacterController controller;
    private Vector3 velocity;
    private float gravity = -20f;

    [Header("Estadísticas")]
    public float baseSpeed = 8f;
    public float jumpHeight = 2.5f;
    private float currentSpeed;

    public override void OnNetworkSpawn()
    {
        controller = GetComponent<CharacterController>();

        // --- LA SOLUCIÓN DEL SPAWN ---
        // Apagamos el controller un milisegundo para poder mover las coordenadas manuales
        controller.enabled = false;
        // Elevamos al jugador a Y=3 (o más alto si tus plataformas miden más)
        transform.position = new Vector3(transform.position.x, 3f, transform.position.z);
        // Lo volvemos a prender para que la gravedad haga su trabajo
        controller.enabled = true;
        // -----------------------------

        currentSpeed = baseSpeed;

        if (IsServer && RunnerGameManager.Instance != null)
        {
            RunnerGameManager.Instance.RegisterPlayer(this);
        }

        if (!IsOwner)
        {
            // Opcional: Aquí cambias el material de los fantasmas
        }
    }

    void Update()
    {
        if (!IsOwner || !isAlive.Value) return;

        // Movimiento automático hacia adelante (Eje X)
        Vector3 move = new Vector3(1, 0, 0) * currentSpeed;

        // Gravedad y Salto
        if (controller.isGrounded && velocity.y < 0) velocity.y = -2f;

        if (Input.GetButtonDown("Jump") && controller.isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // Habilidad Dash (El Precio: Más rápido ahora, más lento después)
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            ApplyDashPenaltyServerRpc();
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move((move + velocity) * Time.deltaTime);
    }

    [ServerRpc]
    private void ApplyDashPenaltyServerRpc()
    {
        ApplyDashPenaltyClientRpc();
    }

    [ClientRpc]
    private void ApplyDashPenaltyClientRpc()
    {
        StartCoroutine(DashRoutine());
    }

    System.Collections.IEnumerator DashRoutine()
    {
        // Dash rápido
        currentSpeed = baseSpeed * 2f;
        yield return new WaitForSeconds(0.2f);
        // Castigo
        currentSpeed = baseSpeed * 0.7f;
        yield return new WaitForSeconds(2f);
        // Normalidad
        currentSpeed = baseSpeed;
    }

    [ClientRpc]
    public void DieClientRpc()
    {
        GetComponentInChildren<MeshRenderer>().enabled = false;
        // Aquí reproduces partículas y sonido de explosión
        Debug.Log("Jugador eliminado por la cámara");
    }
}