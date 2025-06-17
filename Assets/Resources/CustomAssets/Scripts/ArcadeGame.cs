using Unity.Netcode;
using UnityEngine;

public class ArcadeGame : NetworkBehaviour
{

    public NetworkVariable<ulong> p1HotKills = new(0);
    public NetworkVariable<ulong> p1ColdKills = new(0);

    public NetworkVariable<ulong> p2HotKills = new(0);
    public NetworkVariable<ulong> p2ColdKills = new(0);

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        p1HotKills.OnValueChanged += OnP1HotKillsChanged;
        p1ColdKills.OnValueChanged += OnP1ColdKillsChanged;
        p2HotKills.OnValueChanged += OnP2HotKillsChanged;
        p2ColdKills.OnValueChanged += OnP2ColdKillsChanged;
    }

    // Update is called once per frame
    void Update()
    {

    }

    [ServerRpc(RequireOwnership = false)]
    public void SetP1HotKillsServerRpc(ulong newVal)
    {
        //if (!IsServer) return;
        p1HotKills.Value = newVal;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetP1ColdKillsServerRpc(ulong newVal)
    {
        //if (!IsServer) return;
        p1ColdKills.Value = newVal;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetP2HotKillsServerRpc(ulong clientId, ulong newVal)
    {
        //if (!IsServer) return;
        Debug.Log("client " + clientId + " is attempting to change P2HotKills");
        p2HotKills.Value = newVal;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetP2ColdKillsServerRpc(ulong newVal)
    {
        //if (!IsServer) return;
        p2ColdKills.Value = newVal;
    }

    private void OnP1HotKillsChanged(ulong oldValue, ulong newValue)
    {

    }

    private void OnP1ColdKillsChanged(ulong oldValue, ulong newValue)
    {

    }

    private void OnP2HotKillsChanged(ulong oldValue, ulong newValue)
    {

    }

    private void OnP2ColdKillsChanged(ulong oldValue, ulong newValue)
    {

    }

    public override void OnDestroy()
    {
        p1HotKills.OnValueChanged -= OnP1HotKillsChanged;
        p1ColdKills.OnValueChanged -= OnP1ColdKillsChanged;
        p2HotKills.OnValueChanged -= OnP2HotKillsChanged;
        p2ColdKills.OnValueChanged -= OnP2ColdKillsChanged;
    }
}