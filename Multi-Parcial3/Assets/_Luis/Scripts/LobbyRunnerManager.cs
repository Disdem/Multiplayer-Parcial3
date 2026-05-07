using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class LobbyRunnerManager : MonoBehaviour // <-- Lo regresamos a MonoBehaviour normal
{
    [SerializeField] private string levelName;
    public void StartRunnerGame()
    {
        // En lugar de usar la variable local 'IsServer', le preguntamos directamente al jefe (el Singleton)
        if (NetworkManager.Singleton.IsServer)
        {
            // Carga la escena de la carrera para todos los conectados
            NetworkManager.Singleton.SceneManager.LoadScene(levelName, LoadSceneMode.Single);
        }
        else
        {
            Debug.LogWarning("Solo el Host puede iniciar la partida.");
        }
    }
}