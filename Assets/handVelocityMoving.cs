using UnityEngine;

public class HandTrackingVelocityEnvironment : MonoBehaviour
{
    public Transform leftHandAnchor;      // Left hand tracking anchor
    public Transform rightHandAnchor;     // Right hand tracking anchor
    public Transform environmentObject;   // Object moved by hand motion
    public float movementScale = 0.3f;    // Scale factor for movement

    private Vector3 lastLeftPos;
    private Vector3 lastRightPos;

    void Start()
    {
        // Store initial positions
        if (leftHandAnchor != null) 
            lastLeftPos = leftHandAnchor.position;

        if (rightHandAnchor != null) 
            lastRightPos = rightHandAnchor.position;
    }

    void Update()
    {
        if (environmentObject == null)
            return;

        Vector3 finalVelocity = Vector3.zero; // Combined velocity from both hands

        // ----------------- Left Hand -----------------
        if (leftHandAnchor != null)
        {
            Vector3 currentLeftPos = leftHandAnchor.position;

            // Velocity = (currentPos - lastPos) / deltaTime
            Vector3 leftVelocity = (currentLeftPos - lastLeftPos) / Time.deltaTime;
            lastLeftPos = currentLeftPos;

            // Add to final velocity
            finalVelocity += leftVelocity;

            Debug.Log("Left Hand Velocity: " + leftVelocity.ToString("F3"));
        }

        // ----------------- Right Hand -----------------
        if (rightHandAnchor != null)
        {
            Vector3 currentRightPos = rightHandAnchor.position;

            // Velocity = (currentPos - lastPos) / deltaTime
            Vector3 rightVelocity = (currentRightPos - lastRightPos) / Time.deltaTime;
            lastRightPos = currentRightPos;

            // Add to final velocity
            finalVelocity += rightVelocity;

            Debug.Log("Right Hand Velocity: " + rightVelocity.ToString("F3"));
        }

        // ----------------- Apply Movement -----------------
        // Move environment object based on combined hand velocity
        environmentObject.position += finalVelocity * movementScale * Time.deltaTime;
    }
}
