using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class GroupCameraTracker : MonoBehaviour
{
    [Header("Configuración de Cámara")]
    public Camera cam;
    public float smoothTime = 0.3f;
    public Vector3 offset = new Vector3(0, 5, -15);

    [Header("Mecánica Constructiva")]
    public GameObject helpfulPlatformPrefab;

    [Header("Game Feel")]
    public float shakeMagnitude = 0.4f;
    private float currentShakeTime = 0f;
    private Vector3 basePosition;
    private Vector3 velocity = Vector3.zero;

    void Start()
    {
        if (cam == null) cam = Camera.main;
        basePosition = transform.position;
    }

    void LateUpdate()
    {
        RunnerController[] rawPlayers = FindObjectsByType<RunnerController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        // AÑADIDO: Limpiar jugadores nulos o destruidos por la red
        List<RunnerController> alivePlayers = new List<RunnerController>();
        foreach (var p in rawPlayers)
        {
            if (p != null && p.gameObject.activeInHierarchy) alivePlayers.Add(p);
        }

        if (alivePlayers.Count == 0) return;

        Vector3 targetPosition = GetCenterPoint(alivePlayers) + offset;

        if (targetPosition.x < transform.position.x)
        {
            targetPosition.x = transform.position.x;
        }

        basePosition = Vector3.SmoothDamp(basePosition, targetPosition, ref velocity, smoothTime);

        if (currentShakeTime > 0)
        {
            transform.position = basePosition + Random.insideUnitSphere * shakeMagnitude;
            currentShakeTime -= Time.deltaTime;
        }
        else
        {
            transform.position = basePosition;
        }

        bool canKill = (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                       ? NetworkManager.Singleton.IsServer
                       : true;

        if (canKill)
        {
            CheckEliminations(alivePlayers);
        }
    }

    Vector3 GetCenterPoint(List<RunnerController> players)
    {
        if (players.Count == 1) return players[0].transform.position;

        var bounds = new Bounds(players[0].transform.position, Vector3.zero);
        for (int i = 1; i < players.Count; i++)
        {
            bounds.Encapsulate(players[i].transform.position);
        }
        return bounds.center;
    }

    void CheckEliminations(List<RunnerController> players)
    {
        // Se itera en reversa por si modificamos la lista al matar
        for (int i = players.Count - 1; i >= 0; i--)
        {
            RunnerController player = players[i];
            if (player == null) continue;

            Vector3 viewportPos = cam.WorldToViewportPoint(player.transform.position);

            if (viewportPos.x < -0.05f || viewportPos.y < -0.05f)
            {
                ExecuteConstructiveDeath(player);
            }
        }
    }

    // IMPORTANTE: Ahora es "public" para que el jugador pueda llamarla con su botón
    public void ExecuteConstructiveDeath(RunnerController player)
    {
        // 1. Generar la plataforma donde murió
        if (helpfulPlatformPrefab != null)
        {
            GameObject platform = Instantiate(helpfulPlatformPrefab, player.transform.position, Quaternion.identity);

            // Si hay red, sincronizamos la plataforma
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                platform.GetComponent<NetworkObject>().Spawn();
            }
        }

        // 2. Calcular la posición de Respawn (Al centro de la cámara y muy arriba en Y)
        Vector3 respawnPos = new Vector3(cam.transform.position.x, cam.transform.position.y + 8f, player.transform.position.z);

        // 3. Ejecutar el Respawn por red en lugar de destruirlo
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            player.RespawnClientRpc(respawnPos);
        }
        else
        {
            // Para cuando pruebes el juego sin internet
            player.transform.position = respawnPos;
        }
    }
}