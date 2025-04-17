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

    private int findTargetTries = 10;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        StartCoroutine(FindLocalHand());
        state = OrbState.Idle;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Z)) {
            StartCoroutine(FindLocalHand());
        }

        if (state == OrbState.Grabbed) {
            Vector3 targetPos = hand.transform.position + (followOffset.y * hand.transform.up) + (followOffset.z * hand.transform.right);
            transform.position = Vector3.MoveTowards(transform.position, targetPos, (transform.position - targetPos).magnitude * followSpeed * Time.deltaTime);
        }
    }

    public void OnGrab() {
        if (IsOwner) {
            state = OrbState.Grabbed;
            touchHandGrabInteractable.enabled = false;
            //LED animation
        }
    }

    public void OnRelease() {
        
    }

    public IEnumerator FindLocalHand()
    {
        while (hand == null && findTargetTries > 0)
        {
            Debug.Log("Trying to find local right hand...");
            GameObject localAvatar = GameObject.Find("LocalAvatar");
            if (localAvatar) {
                Transform rightHandJoint = localAvatar.transform.Find("Joint RightHandWrist");
                if (rightHandJoint) {
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
        Grabbed,
        Absorbed,
        Launching,
        Launched
    }
}
