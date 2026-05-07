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
        string cleanCode = codeInput.text.Trim();
        await StartClient_(cleanCode);
    }

    async Task<string> StartHost_(int maxConnections)
    {
        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
        string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

        // ---> LA SINTAXIS ACTUALIZADA PARA UNITY 6 <---
        RelayServerData relayServerData = AllocationUtils.ToRelayServerData(allocation, "dtls");

        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
        NetworkManager.Singleton.StartHost();
        return joinCode;
    }

    async Task<bool> StartClient_(string joinCode)
    {
        JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

        // ---> LA SINTAXIS ACTUALIZADA PARA UNITY 6 <---
        RelayServerData relayServerData = AllocationUtils.ToRelayServerData(allocation, "dtls");

        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
        return NetworkManager.Singleton.StartClient();
    }
}