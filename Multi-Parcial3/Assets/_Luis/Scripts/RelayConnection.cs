using UnityEngine;
using TMPro;
using Unity.Netcode;
using Unity.Services.Relay;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Relay.Models;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;

public class RelayConnection : MonoBehaviour
{
    public TMP_Text code;
    public TMP_InputField codeInput; // Cambiado a InputField para que puedan escribir

    async void Start()
    {
        await Unity.Services.Core.UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    public async void StartRelay()
    {
        string joinCode = await StartHost_(4);
        code.text = joinCode;
    }

    public async void JoinRelay()
    {
        // Forzamos a que todo sea mayúscula y quitamos cualquier basura invisible
        string cleanCode = codeInput.text.Trim().ToUpper();

        // Esta línea te dirá en la consola EXACTAMENTE qué estás enviando
        Debug.Log($"[RED] Intentando conectar con el código exacto: '{cleanCode}'");

        await StartClient_(cleanCode);
    }

    async Task<string> StartHost_(int maxConnections)
    {
        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
        string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

        // ---> CONSTRUCTOR MANUAL (A prueba de balas) <---
        RelayServerData relayServerData = new RelayServerData(
            allocation.RelayServer.IpV4,
            (ushort)allocation.RelayServer.Port,
            allocation.AllocationIdBytes,
            allocation.ConnectionData,
            allocation.ConnectionData, // El Host repite su ConnectionData aquí
            allocation.Key,
            true // True = dtls (Conexión segura)
        );

        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
        NetworkManager.Singleton.StartHost();
        return joinCode;
    }

    async Task<bool> StartClient_(string joinCode)
    {
        JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

        // ---> CONSTRUCTOR MANUAL (A prueba de balas) <---
        RelayServerData relayServerData = new RelayServerData(
            allocation.RelayServer.IpV4,
            (ushort)allocation.RelayServer.Port,
            allocation.AllocationIdBytes,
            allocation.ConnectionData,
            allocation.HostConnectionData, // El Cliente sí tiene un HostConnectionData diferente
            allocation.Key,
            true // True = dtls (Conexión segura)
        );

        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
        return NetworkManager.Singleton.StartClient();
    }

}