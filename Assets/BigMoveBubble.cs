using UnityEngine;

public class HandGestureParticleTrigger : MonoBehaviour
{
    public Transform leftHandAnchor;
    public Transform rightHandAnchor;
    public ParticleSystem particleEffect;

    [Header("Emission Rate Settings")]
    public float minEmissionRate = 5f;      // Minimum bubbles per second (slow movement)
    public float maxEmissionRate = 50f;     // Maximum bubbles per second (fast movement)
    public float minSpeed = 0.1f;           // Speed that maps to min emission rate
    public float maxSpeed = 3.0f;           // Speed that maps to max emission rate
    
    [Header("Speed Calculation")]
    public bool useOVRInput = true;         // Use OVRInput velocity (more accurate) or manual calculation
    public float speedSmoothing = 0.1f;     // Smoothing factor for speed changes (0 = no smoothing, 1 = full smoothing)

    private Vector3 lastLeftPos;
    private Vector3 lastRightPos;
    private float smoothedSpeed = 0f;
    private ParticleSystem.EmissionModule emissionModule;

    void Start()
    {
        // Initialize position tracking
        if (leftHandAnchor != null) lastLeftPos = leftHandAnchor.position;
        if (rightHandAnchor != null) lastRightPos = rightHandAnchor.position;

        // Get emission module and ensure particle system is playing
        if (particleEffect != null)
        {
            emissionModule = particleEffect.emission;
            emissionModule.enabled = true;
            
            // Ensure the particle system is always playing
            if (!particleEffect.isPlaying)
            {
                particleEffect.Play();
            }
        }
    }

    void Update()
    {
        if (particleEffect == null) return;

        float currentSpeed = 0f;
        bool ovrSpeedValid = false;

        if (useOVRInput)
        {
            currentSpeed = CalculateOvrSpeed(out ovrSpeedValid);
        }

        // Fall back to manual speed calc when OVR data is unavailable (e.g. running in-editor without headset)
        if (!useOVRInput || !ovrSpeedValid)
        {
            currentSpeed = Mathf.Max(currentSpeed, CalculateManualSpeed());
        }

        // Smooth the speed to avoid rapid fluctuations
        smoothedSpeed = Mathf.Lerp(smoothedSpeed, currentSpeed, 1f - Mathf.Pow(speedSmoothing, Time.deltaTime * 60f));

        // Map speed to emission rate (proportional scaling)
        float normalizedSpeed = Mathf.Clamp01((smoothedSpeed - minSpeed) / (maxSpeed - minSpeed));
        float targetEmissionRate = Mathf.Lerp(minEmissionRate, maxEmissionRate, normalizedSpeed);

        // Update particle system emission rate
        emissionModule.rateOverTime = targetEmissionRate;

        // Debug output (optional - can be removed)
        Debug.Log($"Hand Speed: {smoothedSpeed:F2} m/s → Emission Rate: {targetEmissionRate:F1} bubbles/sec");
    }

    private float CalculateManualSpeed()
    {
        float leftSpeed = 0f;
        float rightSpeed = 0f;

        if (leftHandAnchor != null)
        {
            Vector3 currentLeftPos = leftHandAnchor.position;
            Vector3 leftVelocity = (currentLeftPos - lastLeftPos) / Mathf.Max(Time.deltaTime, 0.0001f);
            leftSpeed = leftVelocity.magnitude;
            lastLeftPos = currentLeftPos;
        }

        if (rightHandAnchor != null)
        {
            Vector3 currentRightPos = rightHandAnchor.position;
            Vector3 rightVelocity = (currentRightPos - lastRightPos) / Mathf.Max(Time.deltaTime, 0.0001f);
            rightSpeed = rightVelocity.magnitude;
            lastRightPos = currentRightPos;
        }

        return Mathf.Max(leftSpeed, rightSpeed);
    }

    private float CalculateOvrSpeed(out bool ovrSpeedValid)
    {
        ovrSpeedValid = false;

        if (OVRInput.GetConnectedControllers() == OVRInput.Controller.None)
        {
            return 0f;
        }

        float leftSpeed = 0f;
        float rightSpeed = 0f;

        if (leftHandAnchor != null && (OVRInput.GetConnectedControllers() & OVRInput.Controller.LTouch) != 0)
        {
            Vector3 localVel = OVRInput.GetLocalControllerVelocity(OVRInput.Controller.LTouch);
            Vector3 worldVel = leftHandAnchor.TransformDirection(localVel);
            leftSpeed = worldVel.magnitude;
            ovrSpeedValid = true;
        }

        if (rightHandAnchor != null && (OVRInput.GetConnectedControllers() & OVRInput.Controller.RTouch) != 0)
        {
            Vector3 localVel = OVRInput.GetLocalControllerVelocity(OVRInput.Controller.RTouch);
            Vector3 worldVel = rightHandAnchor.TransformDirection(localVel);
            rightSpeed = worldVel.magnitude;
            ovrSpeedValid = true;
        }

        return Mathf.Max(leftSpeed, rightSpeed);
    }
}
