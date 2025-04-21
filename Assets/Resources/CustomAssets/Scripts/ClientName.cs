using Unity.Netcode;
using UnityEngine;

public class ClientName : NetworkBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        gameObject.name = "Client " + NetworkManager.Singleton.LocalClientId;
    }
}
