using UnityEngine;

public class HandGestureParticleTrigger : MonoBehaviour
{
    public Transform leftHandAnchor;
    public Transform rightHandAnchor;
    public ParticleSystem particleEffect;

    [Header("Emission Rate Settings")]
    public float baseEmissionRate = 5f;        // Minimum bubbles per second (always bubbling)
    public float maxEmissionRate = 50f;        // Maximum bubbles per second (fast movement)
    
    [Header("Speed Mapping")]
    public float minSpeedThreshold = 0.1f;     // Speed below this uses base rate
    public float maxSpeedThreshold = 3.0f;     // Speed at or above this uses max rate

    private Vector3 lastLeftPos;
    private Vector3 lastRightPos;
    private ParticleSystem.EmissionModule emissionModule;

    void Start()
    {
        if (leftHandAnchor != null) lastLeftPos = leftHandAnchor.position;
        if (rightHandAnchor != null) lastRightPos = rightHandAnchor.position;

        // Get the emission module and ensure particle system is playing
        if (particleEffect != null)
        {
            var mainModule = particleEffect.main;
            emissionModule = particleEffect.emission;
            
            // Configure particle system for continuous emission
            mainModule.loop = true;                    // Enable looping
            mainModule.duration = Mathf.Infinity;      // Infinite duration (never stops)
            emissionModule.enabled = true;             // Enable emission
            
            // Start with base emission rate
            emissionModule.rateOverTime = baseEmissionRate;
            
            // Ensure particle system is playing continuously
            if (!particleEffect.isPlaying)
            {
                particleEffect.Play();
            }
        }
    }

    void Update()
    {
        if (particleEffect == null) return;

        // Ensure particle system keeps playing (safeguard)
        if (!particleEffect.isPlaying)
        {
            particleEffect.Play();
        }

        float maxSpeed = 0f;

        // ========== LEFT HAND ==========
        if (leftHandAnchor != null)
        {
            Vector3 currentLeftPos = leftHandAnchor.position;
            Vector3 leftVelocity = (currentLeftPos - lastLeftPos) / Time.deltaTime;
            float speedL = leftVelocity.magnitude;
            
            if (speedL > maxSpeed)
                maxSpeed = speedL;

            lastLeftPos = currentLeftPos;
        }

        // ========== RIGHT HAND ==========
        if (rightHandAnchor != null)
        {
            Vector3 currentRightPos = rightHandAnchor.position;
            Vector3 rightVelocity = (currentRightPos - lastRightPos) / Time.deltaTime;
            float speedR = rightVelocity.magnitude;
            
            if (speedR > maxSpeed)
                maxSpeed = speedR;

            lastRightPos = currentRightPos;
        }

        // Map hand speed to emission rate (proportional scaling)
        float emissionRate = MapSpeedToEmissionRate(maxSpeed);
        emissionModule.rateOverTime = emissionRate;

        // Debug output (optional - uncomment if needed for debugging)
        // Debug.Log($"Hand Speed: {maxSpeed:F2} m/s → Emission Rate: {emissionRate:F1} bubbles/s");
    }

    float MapSpeedToEmissionRate(float speed)
    {
        // Clamp speed to our threshold range
        speed = Mathf.Clamp(speed, minSpeedThreshold, maxSpeedThreshold);

        // Linear interpolation: slow = base rate, fast = max rate
        float normalizedSpeed = (speed - minSpeedThreshold) / (maxSpeedThreshold - minSpeedThreshold);
        float emissionRate = Mathf.Lerp(baseEmissionRate, maxEmissionRate, normalizedSpeed);

        // If speed is below minimum threshold, use base rate
        if (speed < minSpeedThreshold)
        {
            emissionRate = baseEmissionRate;
        }

        return emissionRate;
    }
}
