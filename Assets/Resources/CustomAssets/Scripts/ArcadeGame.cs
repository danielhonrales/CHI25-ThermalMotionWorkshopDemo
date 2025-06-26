using System;
using System.Collections;
using Meta.XR.ImmersiveDebugger.UserInterface.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class ArcadeGame : NetworkBehaviour
{

    [Header("GameInfo")]
    public NetworkVariable<GameState> state = new(GameState.Inactive);
    public NetworkVariable<int> timer = new(-1);
    public int roundTime;
    public NetworkVariable<ulong> p1HotKills = new(100);
    public NetworkVariable<ulong> p1ColdKills = new(100);

    public NetworkVariable<ulong> p2HotKills = new(100);
    public NetworkVariable<ulong> p2ColdKills = new(100);
    public Coroutine runningStartup;
    public Coroutine runningGame;
    [Space(10)]

    [Header("Refs")]
    public GameController gameController;
    public EnemySpawner enemySpawner;
    public AudioSource countdownSound;
    public TMP_Text localHotScore;
    public TMP_Text localColdScore;
    public TMP_Text remoteHotScore;
    public TMP_Text remoteColdScore;
    public TMP_Text timer1;
    public TMP_Text timer2;
    public GameObject panel1;
    public GameObject panel2;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        p1HotKills.OnValueChanged += OnP1HotKillsChanged;
        p1ColdKills.OnValueChanged += OnP1ColdKillsChanged;
        p2HotKills.OnValueChanged += OnP2HotKillsChanged;
        p2ColdKills.OnValueChanged += OnP2ColdKillsChanged;

        timer.OnValueChanged += OnTimerChanged;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.A) && Input.GetKeyDown(KeyCode.Alpha3))
        {
            state.Value = GameState.Startup;
            if (runningStartup != null) StopCoroutine(runningStartup);
            if (runningGame != null) StopCoroutine(runningGame);
            ResetAndStart();
        }
        if (Input.GetKey(KeyCode.A) && Input.GetKeyDown(KeyCode.Alpha4))
        {
            state.Value = GameState.Winddown;
            Cleanup();
            state.Value = GameState.Inactive;
        }
    }

    public override void OnNetworkSpawn()
    {
        localHotScore.text = p1HotKills.Value.ToString();
        localColdScore.text = p1ColdKills.Value.ToString();
        remoteHotScore.text = p2HotKills.Value.ToString();
        remoteColdScore.text = p2ColdKills.Value.ToString();
    }

    public void ResetAndStart()
    {
        enemySpawner.StopSpawning();
        enemySpawner.ClearEnemies();
        gameController.ResetPlayerInteractions();

        HandlePanelsClientRpc(0);
        SetP1HotKillsServerRpc(0);
        SetP1ColdKillsServerRpc(0);
        SetP2HotKillsServerRpc(0);
        SetP2ColdKillsServerRpc(0);

        state.Value = GameState.Inactive;

        runningStartup = StartCoroutine(Startup());
    }

    public IEnumerator Startup()
    {
        timer.Value = 3;
        yield return new WaitForSeconds(1f);
        timer.Value = 2;
        foreach (NetworkClient networkClient in NetworkManager.Singleton.ConnectedClientsList)
        {
            gameController.SpawnOrbs(networkClient.ClientId);
        }
        enemySpawner.StartSpawning();
        yield return new WaitForSeconds(1f);
        timer.Value = 1;
        yield return new WaitForSeconds(1f);
        timer.Value = 0;

        runningGame = StartCoroutine(RunGame());
    }

    public IEnumerator RunGame()
    {
        yield return new WaitForSeconds(1f);
        state.Value = GameState.Active;

        timer.Value = roundTime;
        while (timer.Value > 0)
        {
            yield return new WaitForSecondsRealtime(1f);
            timer.Value--;

            if (timer.Value == 10)
            {
                countdownSound.pitch = 1f;
                countdownSound.Play();
            }
        }

        countdownSound.pitch = 1.15f;
        countdownSound.Play();

        HandlePanelsClientRpc(1);

        state.Value = GameState.Winddown;
        Cleanup();
    }

    [ClientRpc]
    public void HandlePanelsClientRpc(int mode)
    {
        if (mode == 0)
        {
            panel1.SetActive(false);
            panel2.SetActive(false);
        }
        if (mode == 1)
            {
            if (p1HotKills.Value > p2HotKills.Value && p1ColdKills.Value > p2ColdKills.Value)
            {
                panel1.SetActive(true);
            }
            if (p2HotKills.Value > p1HotKills.Value && p2ColdKills.Value > p1ColdKills.Value)
            {
                panel2.SetActive(true);
            }
        }
    }

    public void Cleanup()
    {
        enemySpawner.StopSpawning();
        enemySpawner.ClearEnemies();
        gameController.ResetPlayerInteractions();

        StartCoroutine(ResetGame());
    }

    public IEnumerator ResetGame()
    {
        yield return new WaitForSeconds(5);

        HandlePanelsClientRpc(0);
        SetP1HotKillsServerRpc(0);
        SetP1ColdKillsServerRpc(0);
        SetP2HotKillsServerRpc(0);
        SetP2ColdKillsServerRpc(0);

        state.Value = GameState.Inactive;
    }

    private void OnTimerChanged(int oldValue, int newValue)
    {
        if (state.Value == GameState.Startup)
        {
            timer1.color = Color.yellow;
            timer2.color = Color.yellow;
            if (newValue == 0)
            {
                countdownSound.pitch = 1.15f;
            }
            else
            {
                countdownSound.pitch = 1;
            }
            countdownSound.Play();
        }

        if (state.Value == GameState.Active)
        {
            timer1.color = Color.white;
            timer2.color = Color.white;
        }

        timer1.text = timer.Value.ToString();
        timer2.text = timer.Value.ToString();
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
        localHotScore.text = newValue.ToString();
    }

    private void OnP1ColdKillsChanged(ulong oldValue, ulong newValue)
    {
        localColdScore.text = newValue.ToString();
    }

    private void OnP2HotKillsChanged(ulong oldValue, ulong newValue)
    {
        remoteHotScore.text = newValue.ToString();
    }

    private void OnP2ColdKillsChanged(ulong oldValue, ulong newValue)
    {
        remoteColdScore.text = newValue.ToString();
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