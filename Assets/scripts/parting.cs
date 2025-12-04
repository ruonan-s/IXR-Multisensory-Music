using UnityEngine;
using Oculus.Interaction.Input;

public class parting : MonoBehaviour
{
    [Header("Hand Data Source References")]
    public Hand leftHand;
    public Hand rightHand;

    [Header("Overlap Settings")]
    public float initialSpacing = 3f;
    public float overlapAmount = 0.15f;
    public float depthVariation = 0.1f;

    [Header("Parting Settings")]
    public float maxPartingDistance = 15f;
    public float smoothSpeed = 5f;
    public float pinchThreshold = 0.8f;

    private Vector3[] originalPositions = new Vector3[4];
    private MeshRenderer[] corals;
    private float currentPartAmount = 0f;
    private float targetPartAmount = 0f; // Target parting amount to lerp towards
    private bool isInteracting = false;
    private MeshRenderer originalMeshRenderer;

    void Start()
    {
        originalMeshRenderer = GetComponent<MeshRenderer>();
        CreateDuplicates();
        SetupCoralPositions();

        for (int i = 0; i < corals.Length; i++)
        {
            originalPositions[i] = corals[i].transform.localPosition;
        }
    }

    void CreateDuplicates()
    {
        corals = new MeshRenderer[4];
        corals[0] = originalMeshRenderer;

        for (int i = 1; i < 4; i++)
        {
            GameObject duplicate = new GameObject("Coral_" + (i + 1));
            
            // Make duplicates siblings of the original, not children
            duplicate.transform.SetParent(transform.parent);
            duplicate.transform.localPosition = transform.localPosition;
            duplicate.transform.localRotation = transform.localRotation;
            duplicate.transform.localScale = transform.localScale;

            // Copy mesh filter and renderer from the original
            MeshFilter originalFilter = originalMeshRenderer.GetComponent<MeshFilter>();
            if (originalFilter != null)
            {
                MeshFilter duplicateFilter = duplicate.AddComponent<MeshFilter>();
                duplicateFilter.sharedMesh = originalFilter.sharedMesh;
            }

            MeshRenderer duplicateRenderer = duplicate.AddComponent<MeshRenderer>();
            duplicateRenderer.sharedMaterials = originalMeshRenderer.sharedMaterials;

            corals[i] = duplicateRenderer;
        }
    }
    
    void SetupCoralPositions()
    {
        // Get the original position to center everything around
        Vector3 centerPosition = transform.localPosition;
        
        for (int i = 0; i < corals.Length; i++)
        {
            float xPos;
            
            if (i == 0) // Leftmost
            {
                xPos = centerPosition.x - (initialSpacing * 1.5f) - overlapAmount;
            }
            else if (i == 1) // Center-left
            {
                xPos = centerPosition.x - (initialSpacing * 0.5f);
            }
            else if (i == 2) // Center-right
            {
                xPos = centerPosition.x + (initialSpacing * 0.5f);
            }
            else // Rightmost
            {
                xPos = centerPosition.x + (initialSpacing * 1.5f) + overlapAmount;
            }
            
            float zPos = centerPosition.z + ((i % 2 == 0) ? depthVariation : -depthVariation);
            
            corals[i].transform.localPosition = new Vector3(xPos, centerPosition.y, zPos);
        }
    }
    
    void Update()
    {
        CheckPinchGesture();
        AnimateCorals();

        // Debug visualization
        if (leftHand != null && rightHand != null)
        {
            float leftStrength = GetPinchStrength(leftHand);
            float rightStrength = GetPinchStrength(rightHand);

            if (leftStrength > 0.1f || rightStrength > 0.1f)
            {
                Debug.Log($"Left pinch: {leftStrength:F2}, Right pinch: {rightStrength:F2}, Threshold: {pinchThreshold}");
            }
        }
    }
    
    float GetPinchStrength(Hand hand)
    {
        if (hand == null || !hand.IsTrackedDataValid || !hand.IsHighConfidence)
            return 0f;

        if (hand.GetIndexFingerIsPinching())
        {
            return 1f; // Pinching
        }
        return 0f; // Not pinching
    }

    Vector3 GetHandPosition(Hand hand)
    {
        if (hand == null || !hand.IsTrackedDataValid)
            return Vector3.zero;

        if (hand.GetRootPose(out Pose rootPose))
        {
            return rootPose.position;
        }
        return Vector3.zero;
    }

    void CheckPinchGesture()
    {
        // Check if Hand references are assigned
        if (leftHand == null || rightHand == null)
        {
            Debug.LogWarning("Hand references not assigned in " + gameObject.name);
            return;
        }

        // Check if hands are tracked
        if (!leftHand.IsTrackedDataValid || !rightHand.IsTrackedDataValid)
        {
            isInteracting = false;
            // Maintain current position when hands lose tracking
            return;
        }

        bool leftPinch = leftHand.GetIndexFingerIsPinching();
        bool rightPinch = rightHand.GetIndexFingerIsPinching();

        if (leftPinch && rightPinch)
        {
            isInteracting = true;

            Vector3 leftPos = GetHandPosition(leftHand);
            Vector3 rightPos = GetHandPosition(rightHand);

            float spreadDistance = Vector3.Distance(leftPos, rightPos);

            // Directly map hand distance to parting amount
            targetPartAmount = Mathf.Clamp01(spreadDistance / maxPartingDistance);

            // Debug info
            Debug.Log($"Pinching! Spread distance: {spreadDistance:F2}, Part amount: {targetPartAmount:F2}");
        }
        else
        {
            isInteracting = false;
            // Maintain current position when not pinching
        }

        currentPartAmount = Mathf.Lerp(currentPartAmount, targetPartAmount, Time.deltaTime * smoothSpeed);
    }
    
    void AnimateCorals()
    {
        for (int i = 0; i < corals.Length; i++)
        {
            Vector3 targetPos = originalPositions[i];
            
            if (i < 2) // Left side corals (0, 1)
            {
                float movementMultiplier = (i == 0) ? 1.2f : 0.8f;
                // Use Vector3.right for consistent horizontal movement
                targetPos += Vector3.left * maxPartingDistance * currentPartAmount * movementMultiplier;
            }
            else // Right side corals (2, 3)
            {
                float movementMultiplier = (i == 3) ? 1.2f : 0.8f;
                targetPos += Vector3.right * maxPartingDistance * currentPartAmount * movementMultiplier;
            }
            
            corals[i].transform.localPosition = Vector3.Lerp(
                corals[i].transform.localPosition,
                targetPos,
                Time.deltaTime * smoothSpeed
            );
        }
    }
    
    void OnDrawGizmos()
    {
        if (corals == null || corals.Length != 4) return;
        
        Gizmos.color = Color.cyan;
        for (int i = 0; i < corals.Length; i++)
        {
            if (corals[i] != null)
            {
                Gizmos.DrawWireSphere(corals[i].transform.position, 0.05f);
            }
        }
    }
}