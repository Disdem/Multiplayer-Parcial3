using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class RunnerGameManager : NetworkBehaviour
{
    public static RunnerGameManager Instance;
    private List<PlayerRunner> activePlayers = new List<PlayerRunner>();
    public DeathCamera deathCamera;
    public Transform deathZoneLine;

    private int playersAliveCount = 0;

    private void Awake() => Instance = this;

    public void RegisterPlayer(PlayerRunner player)
    {
        if (IsServer && !activePlayers.Contains(player))
        {
            activePlayers.Add(player);
            playersAliveCount++;

            // Inicia la cámara si ya hay jugadores
            if (playersAliveCount > 0 && deathCamera != null) deathCamera.StartCamera();
        }
    }

    void Update()
    {
        if (!IsServer || playersAliveCount == 0 || deathZoneLine == null) return;

        for (int i = 0; i < activePlayers.Count; i++)
        {
            PlayerRunner p = activePlayers[i];
            if (p.isAlive.Value && p.transform.position.x < deathZoneLine.position.x)
            {
                p.isAlive.Value = false;
                p.DieClientRpc();
                playersAliveCount--;

                if (playersAliveCount <= 1)
                {
                    Debug.Log("¡Ronda Terminada! Ganó el último en pie.");
                    if (deathCamera != null) deathCamera.StopCamera();
                }
            }
        }
    }
}