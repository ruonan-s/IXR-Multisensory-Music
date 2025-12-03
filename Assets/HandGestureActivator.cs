using UnityEngine;

public class HandGestureParticleTrigger2 : MonoBehaviour
{
    public Transform leftHandAnchor;
    public Transform rightHandAnchor;
    public ParticleSystem particleEffect;   

    public float speedThreshold = 1.2f;      
    public float distanceThreshold = 0.1f;   
    public float triggerCooldown = 0.5f;     // cooldown time between triggers

    private Vector3 lastLeftPos;
    private Vector3 lastRightPos;
    private float cooldownTimer = 0f;

    void Start()
    {
        if (leftHandAnchor != null) lastLeftPos = leftHandAnchor.position;
        if (rightHandAnchor != null) lastRightPos = rightHandAnchor.position;
    }

    void Update()
    {
        cooldownTimer += Time.deltaTime;

        // --------------------- left hand detection ---------------------
        if (leftHandAnchor != null)
        {
            Vector3 localVelL = OVRInput.GetLocalControllerVelocity(OVRInput.Controller.LTouch);
            Vector3 worldVelL = leftHandAnchor.TransformDirection(localVelL);

            float speedL = worldVelL.magnitude;
            float distL = Vector3.Distance(leftHandAnchor.position, lastLeftPos);

            if (cooldownTimer >= triggerCooldown && 
                speedL > speedThreshold && distL > distanceThreshold)
            {
                TriggerParticle2();
            }

            lastLeftPos = leftHandAnchor.position;
        }

        // --------------------- right hand detection ---------------------
        if (rightHandAnchor != null)
        {
            Vector3 localVelR = OVRInput.GetLocalControllerVelocity(OVRInput.Controller.RTouch);
            Vector3 worldVelR = rightHandAnchor.TransformDirection(localVelR);

            float speedR = worldVelR.magnitude;
            float distR = Vector3.Distance(rightHandAnchor.position, lastRightPos);

            if (cooldownTimer >= triggerCooldown &&
                speedR > speedThreshold && distR > distanceThreshold)
            {
                TriggerParticle2();
            }

            lastRightPos = rightHandAnchor.position;
        }
    }

    void TriggerParticle2()
    {
        Debug.Log("Gesture detected → Particle Play");
        particleEffect.Play();
        cooldownTimer = 0f;   // 重置冷却
    }
}
