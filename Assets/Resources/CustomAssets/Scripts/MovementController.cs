using System.Collections;
using Meta.XR.MultiplayerBlocks.Shared;
using UnityEngine;

public class MovementController : MonoBehaviour
{
    [Header("References")]
    public Transform head;
    public Transform player;
    public Transform platform;
    public Transform avatarContainer;
    public Transform forwardRef;
    public Transform localAvatar;

    [Header("Movement")]
    public float maxTiltAngle = 30f;
    public float moveSpeed = 5f;
    public float deadZone = 2f;
    public float tiltPercent = 0;

    [Header("Platform Visual Tilt")]
    public float platformMaxTilt = 10f;
    public float tiltSmoothSpeed = 6f;

    Quaternion neutralHeadRotation;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            Calibrate();
        }

        if (head != null && neutralHeadRotation != Quaternion.identity)
        {
            float roll = GetHeadRoll();
            float tiltPercent = Mathf.Clamp(roll / maxTiltAngle, -1f, 1f);
            if (Mathf.Abs(roll) < deadZone)
                tiltPercent = 0f;

            // Movement (flip sign if needed)
            Vector3 move = -forwardRef.right * tiltPercent * moveSpeed * Time.deltaTime;
            player.position += move;
            platform.position += move;

            // Platform tilt
            float targetTilt = -tiltPercent * platformMaxTilt;
            Quaternion targetRot =
            Quaternion.AngleAxis(targetTilt, forwardRef.forward);

            platform.rotation = Quaternion.Lerp(
            platform.rotation,
            targetRot,
            tiltSmoothSpeed * Time.deltaTime
            );
        }
        
    }

    void LateUpdate()
    {

    }

    void Calibrate()
    {
        neutralHeadRotation = head.rotation;
        Debug.Log("Head calibrated");
    }

    float GetHeadRoll()
{
    Quaternion relative =
        Quaternion.Inverse(neutralHeadRotation) * head.rotation;

    // Extract roll directly
    Vector3 euler = relative.eulerAngles;

    float roll = Mathf.DeltaAngle(0f, euler.y);
    return roll;
}

    public IEnumerator FindLocal()
    {
        int findTargetTries = 2;
        while (head == null && findTargetTries > 0)
        {
            Debug.Log("Trying to find local head...");
            localAvatar = avatarContainer.Find("LocalAvatar");
            if (localAvatar)
            {
                Transform headJoint = localAvatar.gameObject.GetComponent<AvatarEntity>().GetSkeletonTransform(Oculus.Avatar2.CAPI.ovrAvatar2JointType.Head);
                if (headJoint)
                {
                    head = headJoint;
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
