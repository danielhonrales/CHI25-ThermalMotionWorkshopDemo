using System.Collections;
using Meta.XR.MultiplayerBlocks.NGO;
using Unity.Netcode;
using UnityEngine;

public class GameController : NetworkBehaviour
{

    public GameObject rightGrabInteractor;
    public AvatarSpawnerNGO avatarSpawnerNGO;
    public OrbController currentOrbController;
    public GameObject hotOrbPrefab;
    public GameObject coldOrbPrefab;
    public GameObject beam;
    public GameObject hotBeam;
    public GameObject coldBeam;
    public GameObject hand;
    public CommunicationController communicationController;
    public SignalSender signalSender;
    public LEDAnimationManager lEDAnimationManager;
    public Material neutralMaterial;

    int hotChargeMessage = 3;
    int hotDischargeMessage = 2;
    int coldChargeMessage = 1;
    int coldDischargeMessage = 0;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (IsServer) {
            if (Input.GetKeyDown(KeyCode.A))
            {
                RequestClientsToSpawnAvatarsClientRpc();
            }
            if (Input.GetKeyDown(KeyCode.Z))
            {
                StartCoroutine(FindLocalHand());
            }
            if (Input.GetKeyDown(KeyCode.M))
            {
                SpawnOrbs();
            }
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        StartCoroutine(FindLocalHand());

        if (!IsServer) {
            currentOrbController.gameObject.SetActive(true);
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
        SpawnOrb("Hot");
        //SpawnOrb("Cold");
    }

    public void ResetOrb(string type) {
        currentOrbController.gameObject.GetComponent<NetworkObject>().Despawn();
        if (type.Contains("Hot")) {
            SpawnOrb("Hot");
        } else {
            SpawnOrb("Cold");
        }
    }

    public void SpawnOrb(string type) {
        GameObject orbObject;
        if (type.Contains("Hot")) {
            orbObject = Instantiate(hotOrbPrefab);
        } else {
            orbObject = Instantiate(coldOrbPrefab);
        }
        orbObject.GetComponent<NetworkObject>().Spawn();
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

    public void TriggerDischargeVisuals(string type) {
        lEDAnimationManager.isFire = false;
        lEDAnimationManager.isIce = false;
        lEDAnimationManager.activeMaterial = neutralMaterial;
        lEDAnimationManager.lightColor = new Color32(0, 0, 0, 0);
        lEDAnimationManager.PlayDischargeAnimation();
        if (type.Contains("Hot")){
            hotBeam.SetActive(true);
            coldBeam.SetActive(false);
        } else {
            hotBeam.SetActive(false);
            coldBeam.SetActive(true);
        }
    }

    public void FinishInteraction(string type) {
        ResetOrb(type);
        currentOrbController = null;
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
        Debug.Log("Found local right hand");
    }
}
