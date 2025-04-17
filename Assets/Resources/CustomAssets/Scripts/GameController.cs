using Meta.XR.MultiplayerBlocks.NGO;
using Unity.Netcode;
using UnityEngine;

public class GameController : NetworkBehaviour
{

    public OrbController orbController;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void DetectedReleasePose()
    {
        Debug.Log("Detected release pose");
        orbController.OnRelease();
    }
}
