using System.Collections;
using Oculus.Interaction;
using Unity.Netcode;
using UnityEngine;

public class OrbController : NetworkBehaviour
{

    public GameObject hand;
    public OrbState state;
    public Vector3 followOffset;
    public float followSpeed;
    public TouchHandGrabInteractable touchHandGrabInteractable;
    public GameController gameController;
    public AudioSource chargeAudio;
    public AudioSource dischargeAudio;
    public AudioSource hotAudio;
    public AudioSource coldAudio;
    public GameObject visuals;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        state = OrbState.Idle;
        StartCoroutine(FindLocalHand());
        gameController = GameObject.Find("GameController").GetComponent<GameController>();
    }

    // Update is called once per frame
    void Update()
    {
        if (state == OrbState.Charging) {
            Vector3 targetPos = hand.transform.position + (followOffset.y * hand.transform.up) + (followOffset.z * hand.transform.right);
            transform.position = Vector3.MoveTowards(transform.position, targetPos, (transform.position - targetPos).magnitude * followSpeed * Time.deltaTime);
        }
    }

    public void OnGrab() {
        if (!IsOwner || gameController.currentOrbController != null) return;

        state = OrbState.Charging;
        touchHandGrabInteractable.enabled = false;
        gameController.rightGrabInteractor.SetActive(false);
        gameController.currentOrbController = this;

        StartCoroutine(ChargeSequence());
    }

    public IEnumerator ChargeSequence() {
        Debug.Log("charge sequence");
        gameController.TriggerChargeMotion(gameObject.name);

        // Warmup
        chargeAudio.Play();

        gameController.TriggerChargeVisuals(gameObject.name);

        float shrinkStep = transform.localScale.x / 1000f;
        for (int i = 0; i < 100; i++) {
            transform.localScale = transform.localScale - new Vector3(shrinkStep, shrinkStep, shrinkStep);
            yield return new WaitForSeconds(3f / 100f);
        }
        
        // Finish charge
        visuals.SetActive(false);
        state = OrbState.Charged;
    }

    public void OnRelease() {
        if (state == OrbState.Charged) {
            state = OrbState.Discharging;
            StartCoroutine(DischargeSequence());
        }
    }

    public IEnumerator DischargeSequence() {
        gameController.TriggerDischargeMotion(gameObject.name);
        gameController.TriggerDischargeVisuals(gameObject.name);
        if (gameObject.name.Contains("Hot"))
        {
            hotAudio.Play();
        }
        else if (gameObject.name.Contains("Cold"))
        {
            coldAudio.Play();
        }
        yield return new WaitForSeconds(8f + 2f);

        gameController.rightGrabInteractor.SetActive(true);
        gameController.FinishInteraction(gameObject.name);
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

    public enum OrbState
    {
        Idle,
        Charging,
        Charged,
        Discharging,
        Discharged
    }
}
