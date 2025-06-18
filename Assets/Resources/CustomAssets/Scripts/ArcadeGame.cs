using System;
using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class ArcadeGame : NetworkBehaviour
{

    [Header("GameInfo")]
    public NetworkVariable<GameState> state = new(GameState.Inactive);
    public NetworkVariable<int> countdown = new(-1);
    public NetworkVariable<ulong> p1HotKills = new(0);
    public NetworkVariable<ulong> p1ColdKills = new(0);

    public NetworkVariable<ulong> p2HotKills = new(0);
    public NetworkVariable<ulong> p2ColdKills = new(0);
    [Space(10)]

    [Header("Refs")]
    public GameController gameController;
    public EnemySpawner enemySpawner;
    public TMP_Text countdownText;
    public AudioSource countdownSound;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        p1HotKills.OnValueChanged += OnP1HotKillsChanged;
        p1ColdKills.OnValueChanged += OnP1ColdKillsChanged;
        p2HotKills.OnValueChanged += OnP2HotKillsChanged;
        p2ColdKills.OnValueChanged += OnP2ColdKillsChanged;

        countdown.OnValueChanged += OnCountdownChanged;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.A) && Input.GetKeyDown(KeyCode.Alpha3))
        {
            state.Value = GameState.Startup;

            Cleanup();
            StartCoroutine(Startup());

            state.Value = GameState.Active;
        }
        if (Input.GetKey(KeyCode.A) && Input.GetKeyDown(KeyCode.Alpha4))
        {
            state.Value = GameState.Winddown;
            Cleanup();
        }
    }

    public IEnumerator Startup()
    {
        countdown.Value = 3;
        yield return new WaitForSeconds(1f);
        countdown.Value = 2;
        foreach (NetworkClient networkClient in NetworkManager.Singleton.ConnectedClientsList)
        {
            gameController.SpawnOrbs(networkClient.ClientId);
        }
        StartCoroutine(enemySpawner.SpawnEnemy());
        yield return new WaitForSeconds(1f);
        countdown.Value = 1;
        yield return new WaitForSeconds(1f);
        countdown.Value = 0;
    }

    public void Cleanup()
    {
        SetP1HotKillsServerRpc(0);
        SetP1ColdKillsServerRpc(0);
        SetP2HotKillsServerRpc(0);
        SetP2ColdKillsServerRpc(0);

        StopCoroutine(enemySpawner.SpawnEnemy());
        enemySpawner.ClearEnemies();
        gameController.DespawnAllOrbs();
    }

    private void OnCountdownChanged(int oldValue, int newValue)
    {
        countdownSound.Play();
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
    public void SetP2HotKillsServerRpc(ulong newVal)
    {
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

    public enum GameState
    {
        Inactive,
        Startup,
        Active,
        Winddown
    }
}