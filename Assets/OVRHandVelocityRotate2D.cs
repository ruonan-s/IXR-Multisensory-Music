using UnityEngine;

public class OVRHandVelocityRotate2D : MonoBehaviour
{
    public Transform leftHandAnchor;       // reference to the left hand anchor
    public Transform targetObject2D;       // reference to the 2D object to rotate

    public float rotateScale = 200f;       // hand speed to rotation scale

    void Update()
    {
        if (leftHandAnchor == null || targetObject2D == null)
            return;

        // -------------------- 1.left hand speed（local） --------------------
        Vector3 localVel = OVRInput.GetLocalControllerVelocity(OVRInput.Controller.LTouch);

        // transform to world speed
        Vector3 worldVel = leftHandAnchor.TransformDirection(localVel);

        // print speed for debugging
        Debug.Log("Left Hand velocity (world): " + worldVel);

        // -------------------- 2. use x/y speed to activate 2d rotate -----------
        float rotationAmount = worldVel.x * rotateScale * Time.deltaTime;

        // rotate z axis of 2d  pic
        targetObject2D.Rotate(0, 0, -rotationAmount);
    }
}
