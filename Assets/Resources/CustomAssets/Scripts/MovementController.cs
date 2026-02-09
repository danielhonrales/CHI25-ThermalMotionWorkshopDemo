using System.Collections;
using Meta.XR.MultiplayerBlocks.Shared;
using UnityEngine;

public class MovementController : MonoBehaviour
{

    [Header("References")]
    public GameObject head;
    public Transform player;
    public Transform platform;
    public Transform avatarContainer;

    [Header("Movement Settings")]
    public float maxTiltAngle = 30f;     // Head tilt for full speed
    public float moveSpeed = 5f;
    public float deadZone = 2f;

    [Header("Platform Tilt")]
    public float platformMaxTilt = 10f; // Visual tilt of platform
    public float tiltSmoothSpeed = 5f;

    void Update()
    {
        float headTilt = GetHeadTilt();

        // Normalize head tilt (-1 to 1)
        float tiltPercent = Mathf.Clamp(headTilt / maxTiltAngle, -1f, 1f);

        // Dead zone
        if (Mathf.Abs(headTilt) < deadZone)
            tiltPercent = 0f;

        // Movement
        float movement = tiltPercent * moveSpeed * Time.deltaTime;
        Vector3 moveVector = Vector3.right * movement;

        player.position += moveVector;
        platform.position += moveVector;

        // Platform tilt (around Z axis)
        float targetTilt = -tiltPercent * platformMaxTilt;
        Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetTilt);

        platform.rotation = Quaternion.Lerp(
            platform.rotation,
            targetRotation,
            tiltSmoothSpeed * Time.deltaTime
        );
    }

    float GetHeadTilt()
    {
        float zAngle = head.transform.localEulerAngles.z;
        if (zAngle > 180f)
            zAngle -= 360f;

        return zAngle;
    }

    public IEnumerator FindLocal()
    {
        int findTargetTries = 2;
        while (head == null && findTargetTries > 0)
        {
            Debug.Log("Trying to find local head...");
            GameObject localAvatar = avatarContainer.Find("LocalAvatar").gameObject;
            if (localAvatar)
            {
                Transform headJoint = localAvatar.GetComponent<AvatarEntity>().GetSkeletonTransform(Oculus.Avatar2.CAPI.ovrAvatar2JointType.Head);
                if (headJoint)
                {
                    head = headJoint.gameObject;
                }
            }
            findTargetTries--;
            yield return new WaitForSeconds(1f);
        }
        if (head == null)
        {
            StartCoroutine(FindLocal());
        }
    }
}
