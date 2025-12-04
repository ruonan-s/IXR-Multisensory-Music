using UnityEngine;

public class HandShakeShakerSound : MonoBehaviour
{
    public Transform leftHand;            // Hand tracking anchor for left hand
    public Transform rightHand;           // Hand tracking anchor for right hand
    public AudioSource audioSource;       // AudioSource playing the shaker sound

    public float shakeSensitivity = 1.2f; // Minimum velocity required to count as shaking
    public float volumeMultiplier = 0.4f; // Controls how loud it gets when shaking fast
    public float pitchMultiplier = 0.2f;  // Pitch rise with shake speed
    public float smoothing = 0.25f;       // Smooth filter to avoid jitter

    private Vector3 lastLeftPos;
    private Vector3 lastRightPos;
    private float leftSpeed;
    private float rightSpeed;

    void Start()
    {
        if (leftHand != null) lastLeftPos = leftHand.position;
        if (rightHand != null) lastRightPos = rightHand.position;

        // Make sure the sound loops (continuous shaker noise)
        if (audioSource != null)
        {
            audioSource.loop = true;
            audioSource.playOnAwake = false;
        }
    }

    void Update()
    {
        // ---------------- LEFT HAND VELOCITY ----------------
        if (leftHand != null)
        {
            Vector3 cur = leftHand.position;
            float rawSpeed = (cur - lastLeftPos).magnitude / Time.deltaTime;

            // Smooth the speed to avoid jitter
            leftSpeed = Mathf.Lerp(leftSpeed, rawSpeed, smoothing);

            lastLeftPos = cur;
        }

        // ---------------- RIGHT HAND VELOCITY ----------------
        if (rightHand != null)
        {
            Vector3 cur = rightHand.position;
            float rawSpeed = (cur - lastRightPos).magnitude / Time.deltaTime;

            rightSpeed = Mathf.Lerp(rightSpeed, rawSpeed, smoothing);

            lastRightPos = cur;
        }

        // ---------------- USE THE HIGHER SPEED ----------------
        float shakeSpeed = Mathf.Max(leftSpeed, rightSpeed);

        // If shaking fast enough → activate continuous sand sound
        if (shakeSpeed > shakeSensitivity)
        {
            if (!audioSource.isPlaying)
                audioSource.Play();

            // Volume proportional to shake speed
            audioSource.volume = Mathf.Clamp(shakeSpeed * volumeMultiplier, 0f, 1f);

            // Pitch slightly increases with speed
            audioSource.pitch = 1f + shakeSpeed * pitchMultiplier;
        }
        else
        {
            // Slowly fade out when not shaking
            audioSource.volume = Mathf.Lerp(audioSource.volume, 0f, Time.deltaTime * 4f);

            // If volume is almost zero → stop
            if (audioSource.volume < 0.01f && audioSource.isPlaying)
                audioSource.Stop();
        }
    }
}
