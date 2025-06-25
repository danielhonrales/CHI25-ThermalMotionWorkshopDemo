using System.Collections;
using System.Collections.Generic;
using System.Net;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class SleevePositioner : NetworkBehaviour
{
    [SerializeField]
    Transform startPos; // Reference to the start point
    [SerializeField]
    Transform endPos; // Reference to the end point
    Vector3 midPos;
    public float frames;
    // Start is called before the first frame update

    public Transform hand;
    public float yChange;
    public float yStart;
    public float yEnd;
    public MeshRenderer[] meshes;
    //public GameObject avatarHandler;
    //public VRMap rightHand;
    public Vector3 offset;
    public GameObject LED_tube;

    public GameObject lower;
    public GameObject upper;


    void Start()
    {

    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        name = name.Replace("(Clone)", "") + " " + GetComponent<NetworkObject>().OwnerClientId;
        LED_tube = GameObject.Find("ledTube " + GetComponent<NetworkObject>().OwnerClientId);
        transform.parent = GameObject.FindWithTag("SleevePointContainer").transform;
    }

    // Update is called once per frame
    void Update()
    {
        // tracking both sleeves locally to reduce network load
        string targetClient = IsOwner ? "LocalAvatar" : "RemoteAvatar";

        if (lower == null)
            {
                lower = GameObject.FindWithTag("AvatarContainer").transform.Find(targetClient + "/Joint RightArmLower").gameObject;
            }
        if (upper == null)
        {
            upper = GameObject.FindWithTag("AvatarContainer").transform.Find(targetClient + "/Joint RightHandWrist").gameObject;
        }

        if (lower != null && upper != null)
        {
            endPos = lower.transform;
            startPos = upper.transform;

            // Calculate the midpoint between startPos and endPos
            Vector3 midpoint = (startPos.position + endPos.position) / 2f;

            // Update the position of the GameObject to the midpoint
            // Rotate the GameObject to match the direction from startPos to endPos
            LED_tube.transform.SetPositionAndRotation(midpoint + offset, Quaternion.LookRotation(endPos.position - startPos.position, Vector3.up) * Quaternion.Euler(0f, -90f, 0f));
        }
    
        




        // Calculate the rotation to align the GameObject with the direction vector
        //LED_tube.transform.rotation = Quaternion.FromToRotation(Vector3.up, direction);

        //LED_tube.transform.rotation = rotation;
        if (Input.GetKeyDown(KeyCode.S))
        {
            /*startPos = GameObject.Find("Joint RightHandWrist").transform;
            endPos = GameObject.Find("Joint RightArmLower").transform;
            Vector3 dir = endPos.position - startPos.position;
            float distance = direction.magnitude;
            print(distance);

            // Calculate the position along the line connecting the start and end points
            float t = Mathf.Clamp01(Vector3.Dot(transform.position - startPos.position, dir) / distance);
            Vector3 newPosition = startPos.position + t * direction;

            // Update the position of the sphere
            transform.position = newPosition;*/
            Play();

        }
    }


    public void Play()
    {
        endPos = lower.transform;
        startPos = upper.transform;
        StartCoroutine(PlayVisual());
        //FreezeTracking(true);
    }

    public IEnumerator PlayVisual()
    {

        float overallDis = Vector3.Distance(startPos.position, endPos.position);

        hand.position = startPos.position;

        float zSpeed = (startPos.position.z - endPos.position.z) / frames;
        float xSpeed = (startPos.position.x - endPos.position.x) / frames;
        float ySpeed = (startPos.position.y - endPos.position.y) / frames;
        /*midPos = (endPos.position + startPos.position) / 2.0f;
        LED_tube.transform.position = midPos;*/
        //LED_tube.transform.localScale = new Vector3(overallDis, overallDis, overallDis);

        /*for (int i = 0; i < LED_tube.transform.childCount; i++)
        {
            // Get the current child GameObject
            Transform child = LED_tube.transform.GetChild(i);
            child.transform.localScale = new Vector3(overallDis/ LED_tube.transform.childCount, overallDis/ LED_tube.transform.childCount, overallDis);
           
        }*/

        for (int i = 0; i < frames; i++)
        {
            hand.position = new Vector3(hand.position.x - xSpeed, hand.position.y - ySpeed, hand.position.z - zSpeed);
            yield return new WaitForSeconds(.01f);
        }
        //yield return new WaitForSeconds(.01f);

    }
}






