using System.Collections;
using System.Collections.Generic;
using Meta.XR.MultiplayerBlocks.NGO;
using Unity.Netcode;
using UnityEngine;

public class GameController : NetworkBehaviour
{

    public GameObject cameraRig;
    public GameObject rightGrabInteractor;
    public AvatarSpawnerNGO avatarSpawnerNGO;
    public OrbController currentOrbController;
    public GameObject hotOrbPrefab;
    public GameObject coldOrbPrefab;
    public GameObject beamPrefab;
    public List<GameObject> hotOrbs;
    public List<GameObject> coldOrbs;
    public List<GameObject> beams;
    public Transform playerPoint1;
    public Transform playerPoint2;
    public Transform orbSpawnPoint;
    public Transform orbsSpawnPoint1;
    public Transform orbsSpawnPoint2;
    public GameObject orbSpawner;
    public GameObject hand;
    public CommunicationController communicationController;
    public SignalSender signalSender;
    public LEDAnimationManager lEDAnimationManager;
    public Material neutralMaterial;
    public List<ulong> connectedClients;
    public EnemySpawner enemySpawner;
    public int hotKills;
    public int coldKills;

    int hotChargeMessage = 2;
    int hotDischargeMessage = 3;
    int coldChargeMessage = 0;
    int coldDischargeMessage = 1;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    public void HandleClientConnected(ulong clientId) {
        if (!IsServer) return;
        if (connectedClients.Count < 2) { // if multiplayer, add  "&& clientId != 0" to prevent spawning for server
            Debug.Log("New client: " + clientId);
            connectedClients.Add(clientId);
            Debug.Log("Spawning client objects");
            
            GameObject hotOrb = SpawnOrb("Hot");
            hotOrbs.Add(hotOrb);
            GameObject coldOrb = SpawnOrb("Cold");
            coldOrbs.Add(coldOrb);

            GameObject beam = Instantiate(beamPrefab);
            beam.GetComponent<NetworkObject>().SpawnWithOwnership(clientId);
            beams.Add(beam);

            if (connectedClients.Count == 1) {
                Debug.Log("Assigned new client to 1");
                hotOrb.transform.position = orbsSpawnPoint1.position + new Vector3(-0.5f, 0, 0);
                coldOrb.transform.position = orbsSpawnPoint1.position + new Vector3(0.5f, 0, 0);
            } else if (connectedClients.Count == 2) {
                Debug.Log("Assigned new client to 2");
                hotOrb.transform.position = orbsSpawnPoint2.position + new Vector3(-0.5f, 0, 0);
                coldOrb.transform.position = orbsSpawnPoint2.position + new Vector3(0.5f, 0, 0);
            }
        } else {
            NetworkManager.Singleton.DisconnectClient(clientId);
            Debug.Log("Only 2 players! Disconnecting client " + clientId);
        }
    }

    public void HandleClientDisconnected(ulong clientId) {
        if (!IsServer) return;
        int clientIndex = connectedClients.IndexOf(clientId);
        hotOrbs[clientIndex].GetComponent<NetworkObject>().Despawn();
        hotOrbs.RemoveAt(clientIndex);
        coldOrbs[clientIndex].GetComponent<NetworkObject>().Despawn();
        coldOrbs.RemoveAt(clientIndex);
        beams[clientIndex].GetComponent<NetworkObject>().Despawn();
        beams.RemoveAt(clientIndex);
        connectedClients.Remove(clientId);
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsServer) return;
        if (Input.GetKeyDown(KeyCode.A))
        {
            RequestClientsToSpawnAvatarsClientRpc();
        }
        if (Input.GetKeyDown(KeyCode.Z))
        {
            StartCoroutine(FindLocalHand());
        }
        if (Input.GetKey(KeyCode.E) && Input.GetKeyDown(KeyCode.Alpha1))
        {
            StartCoroutine(enemySpawner.SpawnEnemy());
        }
        if (Input.GetKey(KeyCode.E) && Input.GetKeyDown(KeyCode.Alpha2))
        {
            StopCoroutine(enemySpawner.SpawnEnemy());
            enemySpawner.ClearEnemies();
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        StartCoroutine(FindLocalHand());
        if (!IsServer) return;
        if (NetworkManager.Singleton != null) {
            NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;
            Debug.Log("Server listening for clients");
        } else {
            throw new System.Exception("NetworkManager does not exist yet!");
        }
    }

    [ClientRpc]
    private void RequestClientsToSpawnAvatarsClientRpc() {
        if (!IsOwner) return; // Each client responds only for themselves
        Debug.Log("Client: Received spawn instruction. Sending ServerRpc.");
        if (!avatarSpawnerNGO.IsSpawned) {
            avatarSpawnerNGO.SpawnAvatar();
        }
    }

    public void SpawnOrbs() {
        if (!IsServer) return;
        SpawnOrb("Hot");
        //SpawnOrb("Cold");
    }

    public void ResetOrb(string type) {
        if (!IsServer) return;
        currentOrbController.gameObject.GetComponent<NetworkObject>().Despawn();
        if (type.Contains("Hot")) {
            GameObject newOrb = SpawnOrb("Hot");
            newOrb.transform.position = orbSpawnPoint.position;
            StartCoroutine(MoveOrbToPlayer(newOrb.transform, orbsSpawnPoint1.position + new Vector3(-0.5f, 0, 0)));
        } else {
            GameObject newOrb = SpawnOrb("Cold");
            newOrb.transform.position = orbSpawnPoint.position;
            StartCoroutine(MoveOrbToPlayer(newOrb.transform, orbsSpawnPoint1.position + new Vector3(0.5f, 0, 0)));
        }
    }

    public GameObject SpawnOrb(string type) {
        if (!IsServer) return null;
        GameObject orbObject;
        if (type.Contains("Hot")) {
            orbObject = Instantiate(hotOrbPrefab);
        } else {
            orbObject = Instantiate(coldOrbPrefab);
        }
        orbObject.GetComponent<NetworkObject>().Spawn();
        return orbObject;
    }

    public IEnumerator MoveOrbToPlayer(Transform orb, Vector3 targetPos) {
        float steps = 100;
        for (int i = 0; i < steps; i++) {
            orb.position = Vector3.MoveTowards(orb.position, targetPos, (orb.position - targetPos).magnitude * 5 * Time.deltaTime);
            yield return new WaitForSeconds(3f / steps);
        }
    }

    public void DetectedReleasePose()
    {
        if (!IsOwner) return;
        Debug.Log("Detected release pose");
        if (currentOrbController) {
            currentOrbController.OnRelease();
        }
    }

    public void TriggerChargeMotion(string type) {
        if (signalSender.connected)
        {
            if (type.Contains("Hot")) {
                communicationController.SendMotionInfo(hotChargeMessage);
                StartCoroutine(communicationController.LimitVoltage());
            } else {
                communicationController.SendMotionInfo(coldChargeMessage);
                StartCoroutine(communicationController.LimitVoltage());
            }
        }
    }

    public void TriggerChargeVisuals(string type) {
        // Charge
        if (type.Contains("Hot"))
        {
            lEDAnimationManager.isFire = true;
            lEDAnimationManager.isIce = false;
        }
        else if (type.Contains("Cold"))
        {
            lEDAnimationManager.isIce = true;
            lEDAnimationManager.isFire = false;
        }
        lEDAnimationManager.PlayChargeAnimation();
    }


    public void TriggerDischargeMotion(string type) {
        if (signalSender.connected)
        {
            if (type.Contains("Hot"))
            {
                communicationController.SendMotionInfo(hotDischargeMessage);
                StartCoroutine(communicationController.LimitVoltage());
            }
            else
            {
                communicationController.SendMotionInfo(coldDischargeMessage);
                StartCoroutine(communicationController.LimitVoltage());
            }
        }
    }

    public void TriggerDischargeInitialVisual() {
        GameObject clientBeam = beams[connectedClients.IndexOf(NetworkManager.Singleton.LocalClientId)];
        clientBeam.GetComponent<BeamController>().chargeParticles.Play();
    }

    public void TriggerDischargeVisuals(string type) {
        lEDAnimationManager.isFire = false;
        lEDAnimationManager.isIce = false;
        lEDAnimationManager.activeMaterial = neutralMaterial;
        lEDAnimationManager.lightColor = new Color32(0, 0, 0, 0);
        lEDAnimationManager.PlayDischargeAnimation();

        GameObject clientBeam = beams[connectedClients.IndexOf(NetworkManager.Singleton.LocalClientId)];
        if (type.Contains("Hot")){
            clientBeam.GetComponent<BeamController>().SetHotActiveStateServerRpc(true);
            clientBeam.GetComponent<BeamController>().SetColdActiveStateServerRpc(false);
        } else {
            clientBeam.GetComponent<BeamController>().SetHotActiveStateServerRpc(false);
            clientBeam.GetComponent<BeamController>().SetColdActiveStateServerRpc(true);
        }
    }

    public void FinishInteraction(string type) {
        ResetOrb(type);
        currentOrbController = null;
        GameObject clientBeam = beams[connectedClients.IndexOf(NetworkManager.Singleton.LocalClientId)];
        clientBeam.GetComponent<BeamController>().SetHotActiveStateServerRpc(false);
        clientBeam.GetComponent<BeamController>().SetColdActiveStateServerRpc(false);
    }

    public IEnumerator FindLocalHand()
    {
        int findTargetTries = 10;
        while (hand == null && findTargetTries > 0)
        {
            Debug.Log("Trying to find local right hand...");
            GameObject localAvatar = GameObject.Find("LocalAvatar");
            if (localAvatar)
            {
                Transform rightHandJoint = localAvatar.transform.Find("Joint RightHandWrist");
                if (rightHandJoint)
                {
                    hand = rightHandJoint.gameObject;
                }
            }
            findTargetTries--;
            yield return new WaitForSeconds(1f);
        }
        if (hand != null) {
            Debug.Log("Found local right hand");
        } else {
            StartCoroutine(FindLocalHand());
        }
    }

    public void TeleportPlayer(int player) {
        if (player == 1) {
            cameraRig.transform.position = playerPoint1.transform.position;
        } else {
            cameraRig.transform.position = playerPoint2.transform.position;
        }
    }

    public void ListGameObjects() {
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);

        foreach (GameObject go in allObjects)
        {
            Debug.Log($"[ObjectLister] Found object: {go.name} (active: {go.activeInHierarchy})");
        }
    }
}
