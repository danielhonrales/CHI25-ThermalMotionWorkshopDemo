using System.Collections;
using System.Collections.Generic;
using Meta.XR.MultiplayerBlocks.Shared;
using Oculus.Platform;
using Unity.Netcode;
using Unity.VisualScripting;
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
    public ArcadeGame arcadeGame;
    public Transform avatarContainer;
    public HandGuideController handGuideController;
    [Space(10)]

    [Header("Orbs")]
    public OrbController currentOrbController;
    public List<NetworkObject> orbs;
    public Transform orbSpawnPoint;
    public Transform orbPlayerPoint;
    public Vector3 orbPlayerOffset;
    public GameObject orbSpawner;
    public bool releasePoseActive;
    [Space(10)]

    [Header("Power Sleeves")]
    public NetworkObject beam;
    public LEDAnimationManager lEDAnimationManager;
    public Material neutralMaterial;
    [Space(10)]

    [Header("Prefabs")]
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

    [Header("Containers")]
    public Transform ledTubeContainer;
    public Transform sleevePointContainer;
    public Transform beamContainer;
    public Transform orbContainer;
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
            enemySpawner.StartSpawning();
        }
        if (Input.GetKey(KeyCode.E) && Input.GetKeyDown(KeyCode.Alpha2))
        {
            enemySpawner.StopSpawning();
            enemySpawner.ClearEnemies();
        }
        if (Input.GetKey(KeyCode.E) && Input.GetKeyDown(KeyCode.Alpha3))
        {
            TriggerChargeVisuals("Hot");
        }
        if (Input.GetKey(KeyCode.E) && Input.GetKeyDown(KeyCode.Alpha4))
        {
            TriggerChargeVisuals("Cold");
        }
        if (Input.GetKey(KeyCode.E) && Input.GetKeyDown(KeyCode.Alpha5))
        {
            FinishInteraction();
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        myClientId = NetworkManager.Singleton.LocalClientId;
        releasePoseActive = false;
    }

    public void PrepareClients()
    {
        if (!IsServer) return;

        foreach (NetworkClient networkClient in NetworkManager.Singleton.ConnectedClientsList)
        {
            ulong clientId = networkClient.ClientId;

            if (ledTubeContainer.Find("ledTube " + clientId) == null)
            {
                NetworkObject ledTube = Instantiate(ledTubePrefab, ledTubeContainer);
                ledTube.SpawnWithOwnership(clientId);
            }

            if (sleevePointContainer.Find("SleevePoint " + clientId) == null)
            {
                NetworkObject sleevePoint = Instantiate(sleevePointPrefab, sleevePointContainer);
                sleevePoint.SpawnWithOwnership(clientId);
            }

            if (beamContainer.Find("Beam " + clientId) == null)
            {
                NetworkObject clientBeam = Instantiate(beamPrefab, beamContainer);
                clientBeam.SpawnWithOwnership(clientId);
                TelportPlayerClientRpc();
            }
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
        handGuideController.currentGuide = StartCoroutine(handGuideController.GrabGuide());

        StartCoroutine(SpinOrbSpawner());
        Vector3 targetPos = (targetClientId == 0) ? playerPoint1.position : playerPoint2.position;

        if (orbContainer.Find("HotOrb " + targetClientId) == null)
        {
            GameObject hotOrb = SpawnOrb("Hot", targetClientId);
            hotOrb.transform.position = orbSpawnPoint.position;
            StartCoroutine(MoveOrbToPlayer(hotOrb.transform, targetPos + new Vector3(-orbPlayerOffset.x, orbPlayerOffset.y, orbPlayerOffset.z)));
        }
        if (orbContainer.Find("ColdOrb " + targetClientId) == null)
        {
            GameObject coldOrb = SpawnOrb("Cold", targetClientId);
            coldOrb.transform.position = orbSpawnPoint.position;
            StartCoroutine(MoveOrbToPlayer(coldOrb.transform, targetPos + new Vector3(orbPlayerOffset.x, orbPlayerOffset.y, orbPlayerOffset.z)));
        }
    }

    private IEnumerator SpinOrbSpawner()
    {
        orbSpawner.GetComponent<Spin>().enabled = true;
        yield return new WaitForSeconds(1.3f);
        orbSpawner.GetComponent<Spin>().enabled = false;
    }

    public GameObject SpawnOrb(string type, ulong targetClientId)
    {
        if (!IsOwner) return null;
        GameObject orbObject;
        if (type.Contains("Hot"))
        {
            orbObject = Instantiate(hotOrbPrefab, orbContainer);
        }
        else
        {
            orbObject = Instantiate(coldOrbPrefab, orbContainer);
        }
        orbObject.GetComponent<OrbController>().targetClientId.Value = targetClientId;
        orbObject.GetComponent<NetworkObject>().Spawn();
        orbs.Add(orbObject.GetComponent<NetworkObject>());
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
        releasePoseActive = true;
    }

    public void UnselectReleasePose()
    {
        releasePoseActive = false;
    }

    [ServerRpc(RequireOwnership = false)]
    public void TriggerChargeMotionServerRpc(string type, ulong clientId)
    {
        if (type.Contains("Hot"))
        {
            int message = (clientId == 0) ? hotChargeMessage : hotChargeMessage + 4;
            communicationController.SendMotionInfo(message);
            //StartCoroutine(communicationController.LimitVoltage());
        }
        else
        {
            int message = (clientId == 0) ? coldChargeMessage : coldChargeMessage + 4;
            communicationController.SendMotionInfo(message);
            //StartCoroutine(communicationController.LimitVoltage());
        }
    }

    public void TriggerChargeVisuals(string type) {
        handGuideController.currentGuide = StartCoroutine(handGuideController.HoldGuide());
        // Charge
        if (type.Contains("Hot"))
        {
            lEDAnimationManager.isFire = true;
            lEDAnimationManager.isIce = false;

            foreach (GameObject enemy in enemySpawner.enemyInstances) {
                if (enemy.name.Contains("Hot")) {
                    enemy.GetComponent<EnemyController>().target.SetActive(true);
                }
            }
        }
        else if (type.Contains("Cold"))
        {
            lEDAnimationManager.isIce = true;
            lEDAnimationManager.isFire = false;
            
            foreach (GameObject enemy in enemySpawner.enemyInstances) {
                if (enemy.name.Contains("Cold")) {
                    enemy.GetComponent<EnemyController>().target.SetActive(true);
                }
            }
        }
        lEDAnimationManager.PlayChargeAnimation();
    }

    [ServerRpc(RequireOwnership = false)]
    public void TriggerDischargeMotionServerRpc(string type, ulong clientId) {
        if (type.Contains("Hot"))
        {
            int message = (clientId == 0) ? hotDischargeMessage : hotDischargeMessage + 4;
            communicationController.SendMotionInfo(message);
            //StartCoroutine(communicationController.LimitVoltage());
        }
        else
        {
            int message = (clientId == 0) ? coldDischargeMessage : coldDischargeMessage + 4;
            communicationController.SendMotionInfo(message);
            //StartCoroutine(communicationController.LimitVoltage());
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

    public void FinishOrbInteraction(ulong orbNetworkId)
    {
        ResetOrbServerRpc(orbNetworkId);
        FinishInteraction();
    }

    public void FinishInteraction()
    {
        currentOrbController = null;
        BeamController localBeamController = beam.gameObject.GetComponent<BeamController>();
        localBeamController.SetHotActiveStateServerRpc(false);
        localBeamController.SetColdActiveStateServerRpc(false);

        foreach (GameObject enemy in enemySpawner.enemyInstances) {
            enemy.GetComponent<EnemyController>().target.SetActive(false);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ResetOrbServerRpc(ulong orbNetworkId)
    {
        NetworkObject orb = NetworkManager.Singleton.SpawnManager.SpawnedObjects[orbNetworkId];
        ulong targetClientId = orb.OwnerClientId;
        orb.gameObject.name = "deadorb";
        orbs.Remove(orb);
        orb.Despawn();
        if (arcadeGame.state.Value == ArcadeGame.GameState.Active)
        {
            SpawnOrbs(targetClientId);
        }
    }

    public void ResetPlayerInteractions()
    {
        DespawnAllOrbs();
        ResetPlayerClientRpc();
    }

    [ClientRpc]
    public void ResetPlayerClientRpc()
    {
        FinishInteraction();
        rightGrabInteractor.SetActive(true);
        lEDAnimationManager.isFire = false;
        lEDAnimationManager.isIce = false;
        lEDAnimationManager.activeMaterial = neutralMaterial;
        lEDAnimationManager.lightColor = new Color32(0, 0, 0, 0);
        lEDAnimationManager.PlayDischargeAnimation();
    }

    public void DespawnAllOrbs()
    {
        foreach (NetworkObject orb in orbs)
        {
            orb.Despawn();
        }
        orbs = new List<NetworkObject>();
    }

    public IEnumerator FindLocalHand()
    {
        int findTargetTries = 2;
        while (hand == null && findTargetTries > 0)
        {
            Debug.Log("Trying to find local right hand...");
            GameObject localAvatar = avatarContainer.Find("LocalAvatar").gameObject;
            if (localAvatar)
            {
                Transform rightHandJoint = localAvatar.GetComponent<AvatarEntity>().GetSkeletonTransform(Oculus.Avatar2.CAPI.ovrAvatar2JointType.RightHandWrist);
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
            SphereCollider collider = hand.AddComponent<SphereCollider>();
            collider.center = new Vector3(-0.05f, -0.03f, 0);
            collider.radius = 0.03f;
            hand.layer = LayerMask.NameToLayer("Hand");
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
            GameObject remoteAvatar = avatarContainer.Find("RemoteAvatar").gameObject;
            if (remoteAvatar)
            {
                Transform rightHandJoint = remoteAvatar.GetComponent<AvatarEntity>().GetSkeletonTransform(Oculus.Avatar2.CAPI.ovrAvatar2JointType.RightHandWrist);
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
            GameObject beamObject = beamContainer.Find("Beam " + NetworkManager.Singleton.LocalClientId).gameObject;
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
