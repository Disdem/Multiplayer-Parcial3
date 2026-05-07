using Unity.Netcode.Components;
public class ClientNetworkTransform : NetworkTransform
{
    // Otorga autoridad al cliente para movimiento sin lag
    protected override bool OnIsServerAuthoritative() => false;
}