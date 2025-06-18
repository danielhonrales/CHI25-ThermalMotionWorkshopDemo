using System;
using System.Collections;
using System.Linq;
using Meta.XR.MultiplayerBlocks.NGO;
using Meta.XR.MultiplayerBlocks.Shared;
using Unity.Netcode;
using UnityEngine;

public class AvatarController : NetworkBehaviour
{

    public int avatarIndex; //10 = female, 21 = male
    public int[] goodAvatarIndices = {10, 21};
    public GameController gameController;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(SetAvatar());
    }

    // Update is called once per frame
    void Update()
    {
        /* if (Input.GetKeyDown(KeyCode.A))
        {
            AvatarBehaviourNGO avatarBehaviourNGO = GetComponent<AvatarBehaviourNGO>();
            avatarBehaviourNGO.LocalAvatarIndex = avatarIndex;
        } */
    }

    IEnumerator SetAvatar()
    {
        yield return new WaitForSeconds(2);

        bool avatarIndexSet = false;
        AvatarBehaviourNGO avatarBehaviourNGO = GetComponent<AvatarBehaviourNGO>();
        if (avatarBehaviourNGO != null && !goodAvatarIndices.Contains(avatarBehaviourNGO.LocalAvatarIndex))
        {
            avatarBehaviourNGO.LocalAvatarIndex = goodAvatarIndices[UnityEngine.Random.Range(0, goodAvatarIndices.Count())];
            avatarIndexSet = true;
            StartCoroutine(gameController.FindLocalHand());
            StartCoroutine(gameController.FindRemoteHand());
        }

        if (!avatarIndexSet)
        {
            StartCoroutine(SetAvatar());
        }
    }
}
