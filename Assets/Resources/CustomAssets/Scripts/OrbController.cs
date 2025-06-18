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
        hand = gameController.hand;
    }

    // Update is called once per frame
    void Update()
    {
        if (hand == null)
        {
            hand = gameController.hand;
        }
        if (IsOwner && state.Value == OrbState.Charging)
        {
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
        if (state.Value == OrbState.Charging)
        {
            StartCoroutine(ChargeSequence());
        }

        if (state.Value == OrbState.Discharging)
        {
            StartCoroutine(DischargeSequence());
        }
    }

    public void OnGrab()
    {
        TransferOwnershipServerRpc(NetworkManager.Singleton.LocalClientId);
        SetStateServerRpc(OrbState.Charging);
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
        if (IsOwner)
        {
            touchHandGrabInteractable.enabled = false;
            gameController.rightGrabInteractor.SetActive(false);
            gameController.currentOrbController = this;
            gameController.TriggerChargeMotion(gameObject.name);
            gameController.TriggerChargeVisuals(gameObject.name);
        }

        chargeAudio.Play();

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
        if (IsOwner) SetStateServerRpc(OrbState.Charged);
    }

    public void OnRelease() {
        if (state.Value == OrbState.Charged) {
            SetStateServerRpc(OrbState.Discharging);
        }
    }

    public IEnumerator DischargeSequence() {
        beamChargeAudio.Play();
        if (IsOwner) gameController.TriggerDischargeInitialVisual();
        yield return new WaitForSeconds(1f);

        if (IsOwner)
        {
            gameController.TriggerDischargeMotion(gameObject.name);
            gameController.TriggerDischargeVisuals(gameObject.name);
        }
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

        if (IsOwner)
        {
            gameController.rightGrabInteractor.SetActive(true);
            gameController.FinishInteraction(gameObject.name);
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
