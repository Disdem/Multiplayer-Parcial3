using UnityEngine;
using Unity.Netcode;

public class MathObstacle : MonoBehaviour
{
    [Header("Movimiento (Péndulo)")]
    public Vector3 moveAxis = new Vector3(0, 1, 0);
    public float moveDistance = 0f;
    public float moveSpeed = 2f;

    [Header("Rotación (Constante)")]
    public Vector3 rotationAxis = new Vector3(0, 0, 0);
    public float rotationSpeed = 0f;

    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.position;
    }

    void Update()
    {
        // AÑADIDO: Si hay red, usamos el reloj del servidor para sincronización perfecta
        float timeToUse = (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                          ? (float)NetworkManager.Singleton.ServerTime.Time
                          : Time.time;

        if (moveDistance > 0)
        {
            float offset = Mathf.Sin(timeToUse * moveSpeed) * moveDistance;
            transform.position = startPosition + moveAxis * offset;
        }

        if (rotationSpeed > 0 || rotationSpeed < 0)
        {
            transform.Rotate(rotationAxis * rotationSpeed * Time.deltaTime); // La rotación constante se mantiene igual
        }
    }
}