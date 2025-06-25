using System.Collections;
using Unity.Netcode;
using UnityEngine;



public class BeamController : NetworkBehaviour 
{

    public NetworkVariable<bool> isHotActive = new(false);
    public NetworkVariable<bool> isColdActive = new(false);
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
        // tracking both sleeves locally to reduce network load
        string targetClient = IsOwner ? "LocalAvatar" : "RemoteAvatar";

        if (hand == null)
        {
            hand = GameObject.FindWithTag("AvatarContainer").transform.Find(targetClient + "/Joint RightHandWrist").gameObject;
        }
        else
        {
            Vector3 targetPos = hand.transform.position + (followOffset.x * hand.transform.right) + (followOffset.y * hand.transform.up) + (followOffset.z * hand.transform.forward);
            transform.SetPositionAndRotation(
                Vector3.MoveTowards(transform.position, targetPos, (transform.position - targetPos).magnitude * followSpeed * Time.deltaTime),
                Quaternion.LookRotation(hand.transform.up + directionOffset)
            );
        }
        /* if (Input.GetKey(KeyCode.A) && Input.GetKeyDown(KeyCode.Alpha3))
        {
            SetColdActiveStateServerRpc(false);
            SetHotActiveStateServerRpc(true);
        }
        if (Input.GetKey(KeyCode.A) && Input.GetKeyDown(KeyCode.Alpha4))
        {
            SetHotActiveStateServerRpc(false);
            SetColdActiveStateServerRpc(true);
        } */
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        name = name.Replace("(Clone)", "") + " " + GetComponent<NetworkObject>().OwnerClientId;
        transform.parent = GameObject.FindWithTag("BeamContainer").transform;
    }

    public override void OnDestroy()
    {
        isHotActive.OnValueChanged -= OnHotActiveStateChanged;
        isColdActive.OnValueChanged -= OnColdActiveStateChanged;
    } 

    private void OnHotActiveStateChanged(bool oldValue, bool newValue)
    {
        hotBeam.SetActive(newValue);
        if (IsOwner)
        {
            beamCollider.SetActive(newValue);
        }
    }

    private void OnColdActiveStateChanged(bool oldValue, bool newValue)
    {
        coldBeam.SetActive(newValue);
        if (IsOwner)
        {
            beamCollider.SetActive(newValue);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetHotActiveStateServerRpc(bool newState)
    {
        //if (!IsServer) return;
        isHotActive.Value = newState;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetColdActiveStateServerRpc(bool newState)
    {
        //if (!IsServer) return;

        isColdActive.Value = newState;
    }

}
