using UnityEngine;
using Unity.Netcode;

public class GroupCameraTracker : MonoBehaviour
{
    [Header("Configuración de Cámara")]
    public Camera cam;
    public float smoothTime = 0.3f;
    public Vector3 offset = new Vector3(0, 5, -15);

    [Header("Mecánica Constructiva")]
    public GameObject helpfulPlatformPrefab; // Asigna tu prop azul/blanco aquí

    [Header("Game Feel")]
    public float shakeMagnitude = 0.4f;
    private float currentShakeTime = 0f;
    private Vector3 basePosition;

    private Vector3 velocity = Vector3.zero;



    void Start()
    {
        if (cam == null) cam = Camera.main;
        basePosition = transform.position; // Guardar la posición inicial
    }

    void LateUpdate()
    {


        // 1. Buscar a los jugadores vivos en la escena
        RunnerController[] alivePlayers = FindObjectsByType<RunnerController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        if (alivePlayers.Length == 0) return;

        // 2. Calcular el centro de la acción
        Vector3 targetPosition = GetCenterPoint(alivePlayers) + offset;

        // Regla estricta: La cámara NUNCA retrocede. Si el grupo se retrasa, la cámara no perdona.
        if (targetPosition.x < transform.position.x)
        {
            targetPosition.x = transform.position.x;
        }

        // 3. Calcular la posición base (Lógica pura, sin temblores)
        basePosition = Vector3.SmoothDamp(basePosition, targetPosition, ref velocity, smoothTime);

        // 4. Aplicar el temblor SOLO al aspecto visual (transform)
        if (currentShakeTime > 0)
        {
            transform.position = basePosition + Random.insideUnitSphere * shakeMagnitude;
            currentShakeTime -= Time.deltaTime;
        }
        else
        {
            transform.position = basePosition; // Posición normal y suave
        }

        // 5. Ejecutar las eliminaciones (Solo el servidor tiene permiso de matar)
        bool canKill = (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                       ? NetworkManager.Singleton.IsServer
                       : true;

        if (canKill)
        {
            CheckEliminations(alivePlayers);
        }
    }


    Vector3 GetCenterPoint(RunnerController[] players)
    {
        if (players.Length == 1) return players[0].transform.position;

        var bounds = new Bounds(players[0].transform.position, Vector3.zero);
        for (int i = 1; i < players.Length; i++)
        {
            bounds.Encapsulate(players[i].transform.position);
        }
        return bounds.center;
    }

    void CheckEliminations(RunnerController[] players)
    {
        foreach (var player in players)
        {
            Vector3 viewportPos = cam.WorldToViewportPoint(player.transform.position);

            // AÑADIDO: Ahora también mueres si te sales por abajo de la pantalla (viewportPos.y)
            if (viewportPos.x < -0.05f || viewportPos.y < -0.05f)
            {
                ExecuteConstructiveDeath(player);
            }
        }
    }
    void ExecuteConstructiveDeath(RunnerController player)
    {
        // 1. Generar la plataforma
        if (helpfulPlatformPrefab != null)
        {
            GameObject platform = Instantiate(helpfulPlatformPrefab, player.transform.position, Quaternion.identity);

            // Si hay red, sincronizamos la plataforma
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                platform.GetComponent<NetworkObject>().Spawn();
            }
        }

        // 2. Eliminar al jugador
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            player.GetComponent<NetworkObject>().Despawn(true);
        }
        else
        {
            // Modo offline: simplemente lo destruimos
            Destroy(player.gameObject);
        }
    }
}