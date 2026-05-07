using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class LevelGenerator : MonoBehaviour
{
    [Header("Configuración de Chunks")]
    public GameObject[] chunkPrefabs; // Arrastra tus prefabs aquí
    public float chunkLength = 30f;   // El largo exacto de tu pieza de lego en el eje X
    public int initialChunks = 3;     // Cuántos instanciar al empezar

    [Header("Distancias")]
    public float spawnDistanceAhead = 40f;   // Distancia por delante de la cámara para crear
    public float destroyDistanceBehind = 30f; // Distancia por detrás para destruir

    private float nextSpawnX = 0f;
    private Queue<GameObject> activeChunks = new Queue<GameObject>();
    private Transform camTransform;

    void Start()
    {
        camTransform = Camera.main.transform;

        // Generar los primeros bloques inmediatamente
        for (int i = 0; i < initialChunks; i++)
        {
            SpawnNextChunk();
        }
    }

    void Update()
    {
        // Solo el Host (o tú jugando offline) tiene derecho a generar el mapa
        bool hasAuthority = (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
                            || NetworkManager.Singleton.IsServer;

        if (!hasAuthority) return;

        // 1. ¿Necesitamos crear un chunk nuevo?
        if (camTransform.position.x + spawnDistanceAhead > nextSpawnX)
        {
            SpawnNextChunk();
        }

        // 2. ¿Necesitamos destruir el chunk más viejo?
        if (activeChunks.Count > 0)
        {
            GameObject oldestChunk = activeChunks.Peek(); // Mirar el primero de la fila

            // ---> LA PROTECCIÓN <---
            // Si el objeto ya fue destruido por otra cosa (ej. cambio de escena o red), 
            // lo sacamos de la fila inmediatamente y cancelamos esta lectura para evitar el Crash.
            if (oldestChunk == null)
            {
                activeChunks.Dequeue();
                return;
            }

            // Si llegamos aquí, el objeto sí existe y es seguro leer su transform
            if (oldestChunk.transform.position.x + chunkLength < camTransform.position.x - destroyDistanceBehind)
            {
                DestroyOldestChunk();
            }
        }
        if (activeChunks.Count > 0)
        {
            GameObject oldestChunk = activeChunks.Peek(); // Mirar el primero de la fila
            if (oldestChunk.transform.position.x + chunkLength < camTransform.position.x - destroyDistanceBehind)
            {
                DestroyOldestChunk();
            }
        }
    }

    void SpawnNextChunk()
    {
        // Elegir un chunk aleatorio de tu lista
        int randomIndex = Random.Range(0, chunkPrefabs.Length);
        Vector3 spawnPosition = new Vector3(nextSpawnX, 0, 0);

        // Instanciarlo
        GameObject newChunk = Instantiate(chunkPrefabs[randomIndex], spawnPosition, Quaternion.identity);

        // Sincronizar por red si hay servidor activo
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            newChunk.GetComponent<NetworkObject>().Spawn();
        }

        // Añadir a nuestra lista y mover el punto de spawn para el siguiente
        activeChunks.Enqueue(newChunk);
        nextSpawnX += chunkLength;
    }

    void DestroyOldestChunk()
    {
        GameObject chunkToDestroy = activeChunks.Dequeue(); // Sacarlo de la fila

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            chunkToDestroy.GetComponent<NetworkObject>().Despawn(true);
        }
        else
        {
            Destroy(chunkToDestroy);
        }
    }
}