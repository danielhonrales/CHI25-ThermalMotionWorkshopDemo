using System.Collections;
using Oculus.Interaction;
using Unity.Netcode;
using UnityEngine;

public class OrbController : NetworkBehaviour
{

    public NetworkVariable<OrbState> state = new(OrbState.Idle);

    public GameObject hand;
    public Vector3 followOffset;
    public float followSpeed;
    public TouchHandGrabInteractable touchHandGrabInteractable;
    public GameController gameController;
    public AudioSource chargeAudio;
    public AudioSource dischargeAudio;
    public AudioSource hotAudio;
    public AudioSource coldAudio;
    public AudioSource hotBeamAudio;
    public AudioSource coldBeamAudio;
    public AudioSource beamChargeAudio;
    public GameObject visuals;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        state.OnValueChanged += OnStateChanged;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        SetStateServerRpc(OrbState.Idle);
        gameController = GameObject.Find("GameController").GetComponent<GameController>();
        StartCoroutine(GetGameControllerHand());
    }

    // Update is called once per frame
    void Update()
    {
        if (state.Value == OrbState.Charging) {
            Vector3 targetPos = hand.transform.position + (followOffset.y * hand.transform.up) + (followOffset.z * hand.transform.right);
            transform.position = Vector3.MoveTowards(transform.position, targetPos, (transform.position - targetPos).magnitude * followSpeed * Time.deltaTime);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetStateServerRpc(OrbState newState)
    {
        state.Value = newState;
    }

    private void OnStateChanged(OrbState oldValue, OrbState newValue)
    {
        
    }

    public void OnGrab()
    {
        //if (!IsOwner || gameController.currentOrbController != null) return;
        Debug.Log("Grabbed by client " + NetworkManager.Singleton.LocalClientId);
        TransferOwnershipServerRpc(NetworkManager.Singleton.LocalClientId);
        SetStateServerRpc(OrbState.Charging);
        touchHandGrabInteractable.enabled = false;
        gameController.rightGrabInteractor.SetActive(false);
        gameController.currentOrbController = this;

        StartCoroutine(ChargeSequence());
    }

    [ServerRpc(RequireOwnership = false)]
    public void TransferOwnershipServerRpc(ulong newOwnerId)
    {
        Debug.Log("transferring ownership to " + newOwnerId);
        GetComponent<NetworkObject>().RemoveOwnership();
        GetComponent<NetworkObject>().ChangeOwnership(newOwnerId);   
    }

    public IEnumerator ChargeSequence()
    {
        Debug.Log("charge sequence");
        gameController.TriggerChargeMotion(gameObject.name);

        // Warmup
        chargeAudio.Play();

        gameController.TriggerChargeVisuals(gameObject.name);

        float steps = 100f;
        float shrinkStep = transform.localScale.x / steps;
        Debug.Log("Shrink " + shrinkStep);
        for (int i = 0; i < steps; i++)
        {
            transform.localScale = transform.localScale - new Vector3(shrinkStep, shrinkStep, shrinkStep);
            yield return new WaitForSeconds(3f / steps);
        }

        // Finish charge
        visuals.SetActive(false);
        SetStateServerRpc(OrbState.Charged);
    }

    public void OnRelease() {
        if (state.Value == OrbState.Charged) {
            SetStateServerRpc(OrbState.Discharging);
            StartCoroutine(DischargeSequence());
        }
    }

    public IEnumerator DischargeSequence() {
        beamChargeAudio.Play();
        gameController.TriggerDischargeInitialVisual();
        yield return new WaitForSeconds(1f);
        gameController.TriggerDischargeMotion(gameObject.name);
        gameController.TriggerDischargeVisuals(gameObject.name);
        if (gameObject.name.Contains("Hot"))
        {
            hotAudio.Play();
            hotBeamAudio.Play();
        }
        else if (gameObject.name.Contains("Cold"))
        {
            coldAudio.Play();
            coldBeamAudio.Play();
        }
        yield return new WaitForSeconds(5.5f);
        gameController.rightGrabInteractor.SetActive(true);

        gameController.FinishInteraction(gameObject.name);
    }

    public IEnumerator GetGameControllerHand() {
        while (hand == null) {
            yield return new WaitForSeconds(.1f);
            hand = gameController.hand;
        }
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

    public enum OrbState
    {
        Idle,
        Charging,
        Charged,
        Discharging,
        Discharged
    }
}
