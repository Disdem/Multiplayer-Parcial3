using UnityEngine;

public class DeathCamera : MonoBehaviour
{
    public float cameraSpeed = 8f;
    private bool gameStarted = false;

    void Update()
    {
        if (!gameStarted) return;
        // La cámara avanza constantemente sin esperar a nadie
        transform.position += Vector3.right * cameraSpeed * Time.deltaTime;
    }

    public void StartCamera() => gameStarted = true;
    public void StopCamera() => gameStarted = false;
}