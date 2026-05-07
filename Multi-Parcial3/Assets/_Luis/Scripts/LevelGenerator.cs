using UnityEngine;
using Unity.Netcode;

public class LevelGenerator : NetworkBehaviour
{
    public GameObject[] chunkPrefabs; // Tus bloques modulares armados
    public Transform cameraTransform;

    private float spawnX = 0f;
    private float chunkLength = 20f; // Lo largo que es cada bloque

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        // Generar los primeros 3 chunks
        for (int i = 0; i < 3; i++) SpawnChunk();
    }

    void Update()
    {
        if (!IsServer) return;

        // Si la cámara se acerca al final del último chunk generado, crea uno nuevo
        if (cameraTransform.position.x + (chunkLength * 2) > spawnX)
        {
            SpawnChunk();
        }
    }

    private void SpawnChunk()
    {
        int randomIndex = Random.Range(0, chunkPrefabs.Length);
        GameObject chunk = Instantiate(chunkPrefabs[randomIndex], new Vector3(spawnX, 0, 0), Quaternion.identity);

        // Sincronizar el bloque para todos los clientes
        chunk.GetComponent<NetworkObject>().Spawn();

        spawnX += chunkLength;

        // Destruir el chunk después de 10 segundos para no saturar la memoria
        Destroy(chunk, 10f);
    }
}