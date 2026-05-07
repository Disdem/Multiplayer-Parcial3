using UnityEngine;

public class MathObstacle : MonoBehaviour
{
    [Header("Movimiento (Péndulo)")]
    [Tooltip("Eje en el que se moverá. Ej: Y=1 para Arriba/Abajo")]
    public Vector3 moveAxis = new Vector3(0, 1, 0);
    public float moveDistance = 0f; // Ponlo en 0 si no quieres que se mueva
    public float moveSpeed = 2f;

    [Header("Rotación (Constante)")]
    [Tooltip("Eje de rotación. Ej: Z=1 para girar como manecilla de reloj")]
    public Vector3 rotationAxis = new Vector3(0, 0, 0);
    public float rotationSpeed = 0f; // Ponlo en 0 si no quieres que rote

    private Vector3 startPosition;

    void Start()
    {
        // Guardamos la posición inicial para que no se vaya volando al infinito
        startPosition = transform.position;
    }

    void Update()
    {
        // 1. Movimiento Suave (Seno)
        if (moveDistance > 0)
        {
            // Mathf.Sin crea una onda que va de -1 a 1 de forma infinita
            float offset = Mathf.Sin(Time.time * moveSpeed) * moveDistance;
            transform.position = startPosition + moveAxis * offset;
        }

        // 2. Rotación Continua
        if (rotationSpeed > 0 || rotationSpeed < 0)
        {
            transform.Rotate(rotationAxis * rotationSpeed * Time.deltaTime);
        }
    }
}