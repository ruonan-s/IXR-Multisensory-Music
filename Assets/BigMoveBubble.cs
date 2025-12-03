using UnityEngine;

public class HandGestureParticleTrigger : MonoBehaviour
{
    public Transform leftHandAnchor;
    public Transform rightHandAnchor;
    public ParticleSystem particleEffect;

    public float speedThreshold = 1.2f;
    public float distanceThreshold = 0.1f;
    public float triggerCooldown = 0.5f;

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

        // ========== LEFT HAND ==========
        if (leftHandAnchor != null)
        {
            Vector3 currentLeftPos = leftHandAnchor.position;
            Vector3 leftVelocity = (currentLeftPos - lastLeftPos) / Time.deltaTime;

            float speedL = leftVelocity.magnitude;
            float distL = Vector3.Distance(currentLeftPos, lastLeftPos);

            if (cooldownTimer >= triggerCooldown &&
                speedL > speedThreshold && distL > distanceThreshold)
            {
                TriggerParticle();
            }

            lastLeftPos = currentLeftPos;
        }

        // ========== RIGHT HAND ==========
        if (rightHandAnchor != null)
        {
            Vector3 currentRightPos = rightHandAnchor.position;
            Vector3 rightVelocity = (currentRightPos - lastRightPos) / Time.deltaTime;

            float speedR = rightVelocity.magnitude;
            float distR = Vector3.Distance(currentRightPos, lastRightPos);

            if (cooldownTimer >= triggerCooldown &&
                speedR > speedThreshold && distR > distanceThreshold)
            {
                TriggerParticle();
            }

            lastRightPos = currentRightPos;
        }
    }

    void TriggerParticle()
    {
        Debug.Log("HAND gesture → Particle Play()");
        particleEffect.Play();
        cooldownTimer = 0f;
    }
}
