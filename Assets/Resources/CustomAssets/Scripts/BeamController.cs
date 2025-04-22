using System.Collections;
using Unity.Netcode;
using UnityEngine;



public class BeamController : NetworkBehaviour 
{

    public NetworkVariable<bool> isHotActive = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> isColdActive = new NetworkVariable<bool>(false);
    public GameObject hand;
    public Vector3 followOffset;
    public Vector3 directionOffset;
    public float followSpeed;
    public GameObject hotBeam;
    public GameObject coldBeam;
    public ParticleSystem chargeParticles;
    public GameObject beamCollider;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        isHotActive.OnValueChanged += OnHotActiveStateChanged;
        isColdActive.OnValueChanged += OnColdActiveStateChanged;

        OnHotActiveStateChanged(true, false);
        OnColdActiveStateChanged(true, false);
    }

    // Update is called once per frame
    void Update()
    {
        if (hand != null) {
            Vector3 targetPos = hand.transform.position + (followOffset.x * hand.transform.right) + (followOffset.y * hand.transform.up) + (followOffset.z * hand.transform.forward);
            transform.position = Vector3.MoveTowards(transform.position, targetPos, (transform.position - targetPos).magnitude * followSpeed * Time.deltaTime);
            transform.rotation = Quaternion.LookRotation(hand.transform.up);
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        StartCoroutine(FindLocalHand());
    }

    public override void OnDestroy()
    {
        isHotActive.OnValueChanged -= OnHotActiveStateChanged;
        isColdActive.OnValueChanged -= OnColdActiveStateChanged;
    } 

    private void OnHotActiveStateChanged(bool oldValue, bool newValue)
    {
        hotBeam.SetActive(newValue);
        beamCollider.SetActive(newValue);
    }

    private void OnColdActiveStateChanged(bool oldValue, bool newValue)
    {
        coldBeam.SetActive(newValue);
        beamCollider.SetActive(newValue);
    }

    [ServerRpc]
    public void SetHotActiveStateServerRpc(bool newState)
    {
        //if (!IsServer) return;
        isHotActive.Value = newState;
    }

    [ServerRpc]
    public void SetColdActiveStateServerRpc(bool newState)
    {
        //if (!IsServer) return;

        isColdActive.Value = newState;
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
}
