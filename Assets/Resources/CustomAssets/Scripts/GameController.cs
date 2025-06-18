using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GameController : NetworkBehaviour
{

    [Header("Game Control")]
    public GameObject cameraRig;
    public GameObject rightGrabInteractor;
    public GameObject hand;
    public GameObject remoteHand;
    public Transform playerPoint1;
    public Transform playerPoint2;
    public CommunicationController communicationController;
    public SignalSender signalSender;
    public List<ulong> connectedClients;
    [Space(10)]

    [Header("Orbs")]
    public OrbController currentOrbController;
    public List<GameObject> hotOrbs;
    public List<GameObject> coldOrbs;
    public Transform orbSpawnPoint;
    public Transform orbPlayerPoint;
    public Vector3 orbPlayerOffset;
    public GameObject orbSpawner;
    [Space(10)]

    [Header("Power Sleeves")]
    public NetworkObject beam;
    public LEDAnimationManager lEDAnimationManager;
    public Material neutralMaterial;
    [Space(10)]

    [Header("Prefabs")]
    public NetworkObject powerSleevePrefab;
    public NetworkObject sleevePointPrefab;
    public NetworkObject ledTubePrefab;
    public GameObject hotOrbPrefab;
    public GameObject coldOrbPrefab;
    public NetworkObject beamPrefab;
    [Space(10)]

    [Header("Enemy Vars")]
    public EnemySpawner enemySpawner;
    public int hotKills;
    public int coldKills;
    [Space(10)]

    [Header("Testing")]
    public ulong myClientId;
    public ulong targetClient;
    [Space(10)]

    int hotChargeMessage = 2;
    int hotDischargeMessage = 3;
    int coldChargeMessage = 0;
    int coldDischargeMessage = 1;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOwner) return;
        if (Input.GetKey(KeyCode.A) && Input.GetKeyDown(KeyCode.Alpha1))
        {
            PrepareClients();
        }
        if (Input.GetKey(KeyCode.A) && Input.GetKeyDown(KeyCode.Alpha2))
        {
            foreach (NetworkClient networkClient in NetworkManager.Singleton.ConnectedClientsList)
            {
                SpawnOrbs(networkClient.ClientId);
            }
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
        myClientId = NetworkManager.Singleton.LocalClientId;
    }

    public void PrepareClients()
    {
        if (!IsServer) return;

        foreach (NetworkClient networkClient in NetworkManager.Singleton.ConnectedClientsList)
        {
            ulong clientId = networkClient.ClientId;

            NetworkObject ledTube = Instantiate(ledTubePrefab);
            ledTube.SpawnWithOwnership(clientId);

            NetworkObject sleevePoint = Instantiate(sleevePointPrefab);
            sleevePoint.SpawnWithOwnership(clientId);

            NetworkObject clientBeam = Instantiate(beamPrefab);
            clientBeam.SpawnWithOwnership(clientId);
            TelportPlayerClientRpc();
        }
    }

    [ClientRpc]
    public void TelportPlayerClientRpc()
    {
        if (IsServer)
        {
            cameraRig.transform.SetPositionAndRotation(playerPoint1.position, playerPoint1.rotation);
        }
        else
        {
            cameraRig.transform.SetPositionAndRotation(playerPoint2.position, playerPoint2.rotation);
        }
        StartCoroutine(FindLocalHand());
        StartCoroutine(FindRemoteHand());
        StartCoroutine(FindLocalBeam());
    }

    public void SpawnOrbs(ulong targetClientId)
    {
        if (!IsServer) return;

        Vector3 targetPos = (targetClientId == 0) ? playerPoint1.position : playerPoint2.position;
        Vector3 orbOffset = (targetClientId == 0) ? new Vector3(orbPlayerOffset.x, orbPlayerOffset.y, -orbPlayerOffset.z) : orbPlayerOffset;

        if (GameObject.Find("HotOrb " + targetClientId) == null)
        {
            GameObject hotOrb = SpawnOrb("Hot", targetClientId);
            hotOrb.transform.position = orbSpawnPoint.position;
            StartCoroutine(MoveOrbToPlayer(hotOrb.transform, targetPos + new Vector3(-orbOffset.x, orbOffset.y, orbOffset.z)));
        }
        if (GameObject.Find("ColdOrb " + targetClientId) == null)
        {
            GameObject coldOrb = SpawnOrb("Cold", targetClientId);
            coldOrb.transform.position = orbSpawnPoint.position;
            StartCoroutine(MoveOrbToPlayer(coldOrb.transform, targetPos + new Vector3(orbOffset.x, orbOffset.y, orbOffset.z)));
        }
    }

    public GameObject SpawnOrb(string type, ulong targetClientId)
    {
        if (!IsOwner) return null;
        GameObject orbObject;
        if (type.Contains("Hot"))
        {
            orbObject = Instantiate(hotOrbPrefab);
        }
        else
        {
            orbObject = Instantiate(coldOrbPrefab);
        }
        orbObject.GetComponent<OrbController>().targetClientId.Value = targetClientId;
        orbObject.GetComponent<NetworkObject>().Spawn();
        return orbObject;
    }

    public void HandleClientDisconnected(ulong clientId) {
        if (!IsOwner) return;
        int clientIndex = connectedClients.IndexOf(clientId);
        hotOrbs[clientIndex].GetComponent<NetworkObject>().Despawn();
        hotOrbs.RemoveAt(clientIndex);
        coldOrbs[clientIndex].GetComponent<NetworkObject>().Despawn();
        coldOrbs.RemoveAt(clientIndex);
        beam.GetComponent<NetworkObject>().Despawn();
        beam = null;
        connectedClients.Remove(clientId);
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
        GameObject clientBeam = beam.gameObject;
        clientBeam.GetComponent<BeamController>().chargeParticles.Play();
    }

    public void TriggerDischargeVisuals(string type) {
        lEDAnimationManager.isFire = false;
        lEDAnimationManager.isIce = false;
        lEDAnimationManager.activeMaterial = neutralMaterial;
        lEDAnimationManager.lightColor = new Color32(0, 0, 0, 0);
        lEDAnimationManager.PlayDischargeAnimation();

        GameObject clientBeam = beam.gameObject;
        if (type.Contains("Hot")){
            clientBeam.GetComponent<BeamController>().SetHotActiveStateServerRpc(true);
            clientBeam.GetComponent<BeamController>().SetColdActiveStateServerRpc(false);
        } else {
            clientBeam.GetComponent<BeamController>().SetHotActiveStateServerRpc(false);
            clientBeam.GetComponent<BeamController>().SetColdActiveStateServerRpc(true);
        }
    }

    public void FinishInteraction(ulong orbNetworkId) {
        ResetOrbServerRpc(orbNetworkId);
        currentOrbController = null;
        BeamController localBeamController = beam.gameObject.GetComponent<BeamController>();
        localBeamController.SetHotActiveStateServerRpc(false);
        localBeamController.SetColdActiveStateServerRpc(false);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ResetOrbServerRpc(ulong orbNetworkId)
    {
        NetworkObject orb = NetworkManager.Singleton.SpawnManager.SpawnedObjects[orbNetworkId];
        SpawnOrbs(orb.OwnerClientId);
        orb.Despawn();
    }

    public IEnumerator FindLocalHand()
    {
        int findTargetTries = 2;
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
        if (hand != null)
        {
            Debug.Log("Found local right hand");
        }
        else
        {
            StartCoroutine(FindLocalHand());
        }
    }

    public IEnumerator FindRemoteHand()
    {
        int findTargetTries = 2;
        while (remoteHand == null && findTargetTries > 0)
        {
            Debug.Log("Trying to find remote right hand...");
            GameObject remoteAvatar = GameObject.Find("RemoteAvatar");
            if (remoteAvatar)
            {
                Transform rightHandJoint = remoteAvatar.transform.Find("Joint RightHandWrist");
                if (rightHandJoint)
                {
                    remoteHand = rightHandJoint.gameObject;
                }
            }
            findTargetTries--;
            yield return new WaitForSeconds(1f);
        }
        if (remoteHand != null)
        {
            Debug.Log("Found remote right hand");
        }
        else
        {
            StartCoroutine(FindRemoteHand());
        }
    }

    public IEnumerator FindLocalBeam()
    {
        int findTargetTries = 2;
        while (beam == null && findTargetTries > 0)
        {
            Debug.Log("Trying to find local beam...");
            GameObject beamObject = GameObject.Find("Beam " + NetworkManager.Singleton.LocalClientId);
            if (beamObject)
            {
                beam = beamObject.GetComponent<NetworkObject>();
            }
            findTargetTries--;
            yield return new WaitForSeconds(1f);
        }
        if (beam != null)
        {
            Debug.Log("Found beam");
        }
        else
        {
            StartCoroutine(FindLocalBeam());
        }
    }

    public void ListGameObjects()
    {
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);

        foreach (GameObject go in allObjects)
        {
            Debug.Log($"[ObjectLister] Found object: {go.name} (active: {go.activeInHierarchy})");
        }
    }
}
