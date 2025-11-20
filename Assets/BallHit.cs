using UnityEngine;

public class BallHit : MonoBehaviour
{
    public AudioSource audioSource;

    [Header("Assign your anchors here")]
    public Transform leftHandAnchor;
    public Transform rightHandAnchor;

    private void OnTriggerEnter(Collider other)
    {
        // 左手 Anchor 碰到
        if (other.transform == leftHandAnchor)
        {
            Debug.Log("Left Hand Anchor Hit");
            audioSource.Play();
            Vibrate(true);
        }

        // 右手 Anchor 碰到
        else if (other.transform == rightHandAnchor)
        {
            Debug.Log("Right Hand Anchor Hit");
            audioSource.Play();
            Vibrate(false);
        }
    }

    void Vibrate(bool isLeft)
    {
        // 振动一次 0.2 秒，强度 0.8
        OVRInput.SetControllerVibration(
            1f, 0.8f,
            isLeft ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch
        );

        // 0.2 秒后关掉震动
        Invoke(nameof(StopVibration), 0.2f);
    }

    void StopVibration()
    {
        OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.LTouch);
        OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.RTouch);
    }
}
