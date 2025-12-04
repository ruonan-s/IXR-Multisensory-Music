using UnityEngine;
using Text = TMPro.TMP_Text;

public class OneHandTransform : MonoBehaviour
{
    [Header("References")]
    public TransformationEvaluator evaluator;
    public OVRHand leftHand;
    public OVRHand rightHand;
    public Text guideText;
    
    [Header("Edge Highlighting")]
    public Material edgeHighlightMaterial;
    public float edgeThickness = 0.02f;
    
    [Header("Settings")]
    public float pinchThreshold = 0.7f;
    public float releaseThreshold = 0.5f;
    public float autoConfirmDelay = 0.5f;
    
    [Header("Auto-Confirm Thresholds")]
    public float thresholdDistance = 0.2f;
    public float thresholdScale = 0.2f;
    public float thresholdRotation = 20f;
    
    private Transform sourceTransform;
    
    // Interaction modes
    private enum InteractionMode { None, LeftHandPosition, RightHandRotation, BothHands }
    private InteractionMode currentInteractionMode = InteractionMode.None;
    private InteractionMode previousInteractionMode = InteractionMode.None;
    
    // Hand tracking state
    private bool isLeftPinching = false;
    private bool isRightPinching = false;
    private Vector3 leftPinchPos;
    private Vector3 rightPinchPos;
    private Quaternion leftHandRotation;
    private Quaternion rightHandRotation;
    
    // Position grab state
    private Vector3 initialGrabOffset;
    private Vector3 initialGrabHandPos;
    private Vector3 initialObjectPos;
    private bool usingLeftHand;  // Which hand is being used
    private float movementMultiplier = 15.0f;  // Amplify hand movement for larger object movement
    
    // Rotation state
    private Quaternion initialHandRotation;
    private Quaternion initialCubeRotation;
    private float rotationMultiplier = 2.5f;  // Amplify rotation for more responsive control
    
    // Scale state
    private Vector3 initialHandSeparation;
    private Vector3 initialScale;
    private Vector3 scaleGrabCenter;
    private float scaleSensitivity = 0.5f;  // Reduced sensitivity for stretching
    private int lockedScaleAxis = -1;  // 0=X, 1=Y, 2=Z, -1=none (locked when scaling starts)
    
    // Edge highlighting
    private GameObject edgeVisualizer;
    private LineRenderer[] edgeLines;
    
    // Space rotation (clap detection)
    private bool wasClapDetected = false;
    private float clapCooldown = 0f;
    
    // Space rotation pivot
    private GameObject spacePivot;
    
    // Auto-confirm
    private float timeAtThreshold = 0f;
    private bool wasWithinThreshold = false;
    
    void Awake()
    {
        sourceTransform = evaluator.GetSourceTransform();
        
        // Create space pivot
        spacePivot = new GameObject("SpacePivot");
        spacePivot.transform.position = evaluator.spawnBoxCenter;
        
        // Create edge visualizer
        CreateEdgeVisualizer();
        
        UpdateGuideText();
    }
    
    void CreateEdgeVisualizer()
    {
        edgeVisualizer = new GameObject("EdgeVisualizer");
        edgeVisualizer.transform.SetParent(sourceTransform);
        edgeVisualizer.transform.localPosition = Vector3.zero;
        edgeVisualizer.transform.localRotation = Quaternion.identity;
        
        edgeLines = new LineRenderer[12];
        
        for (int i = 0; i < 12; i++)
        {
            GameObject lineObj = new GameObject($"Edge_{i}");
            lineObj.transform.SetParent(edgeVisualizer.transform);
            
            LineRenderer lr = lineObj.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.startWidth = edgeThickness;
            lr.endWidth = edgeThickness;
            lr.material = edgeHighlightMaterial != null ? edgeHighlightMaterial : new Material(Shader.Find("Sprites/Default"));
            lr.startColor = Color.yellow;
            lr.endColor = Color.yellow;
            lr.enabled = false;
            
            edgeLines[i] = lr;
        }
    }
    
    void Update()
    {
        if (!ValidateHandTracking())
        {
            // Reset auto-confirm if hands lose tracking
            timeAtThreshold = 0f;
            wasWithinThreshold = false;
            return;
        }
        
        UpdateHandTracking();
        DetectClapGesture();
        DetermineInteractionMode();
        ExecuteInteraction();
        UpdateVisuals();
        CheckAutoConfirm();
        
        if (clapCooldown > 0f)
            clapCooldown -= Time.deltaTime;
    }
    
    bool ValidateHandTracking()
    {
        if (leftHand == null || rightHand == null)
        {
            Debug.LogError("OVRHand references not set!");
            return false;
        }
        
        if (!leftHand.IsTracked && !rightHand.IsTracked)
        {
            currentInteractionMode = InteractionMode.None;
            return false;
        }
        
        return true;
    }
    
    void UpdateHandTracking()
    {
        // Left hand
        if (leftHand.IsTracked)
        {
            float leftPinchStrength = leftHand.GetFingerPinchStrength(OVRHand.HandFinger.Index);
            
            if (!isLeftPinching && leftPinchStrength > pinchThreshold)
                isLeftPinching = true;
            else if (isLeftPinching && leftPinchStrength < releaseThreshold)
                isLeftPinching = false;
            
            leftPinchPos = leftHand.PointerPose.position;
            leftHandRotation = leftHand.PointerPose.rotation;
        }
        else
        {
            // Reset state if hand loses tracking
            isLeftPinching = false;
        }
        
        // Right hand
        if (rightHand.IsTracked)
        {
            float rightPinchStrength = rightHand.GetFingerPinchStrength(OVRHand.HandFinger.Index);
            
            if (!isRightPinching && rightPinchStrength > pinchThreshold)
                isRightPinching = true;
            else if (isRightPinching && rightPinchStrength < releaseThreshold)
                isRightPinching = false;
            
            rightPinchPos = rightHand.PointerPose.position;
            rightHandRotation = rightHand.PointerPose.rotation;
        }
        else
        {
            // Reset state if hand loses tracking
            isRightPinching = false;
        }
    }
    
    void DetectClapGesture()
    {
        if (clapCooldown > 0f) return;
        
        // Both hands must be tracked
        if (leftHand.IsTracked && rightHand.IsTracked && !isLeftPinching && !isRightPinching)
        {
            // Get hand positions
            Vector3 leftHandPos = leftHand.PointerPose.position;
            Vector3 rightHandPos = rightHand.PointerPose.position;
            
            // Calculate distance between hands
            float handDistance = Vector3.Distance(leftHandPos, rightHandPos);
            
            // Clap detected when hands are close together (less sensitive - smaller threshold)
            if (handDistance < 0.08f && !wasClapDetected)
            {
                RotateSpace();
                wasClapDetected = true;
                clapCooldown = 0.5f;
                Debug.Log("*** CLAP DETECTED - Rotating space! ***");
            }
            else if (handDistance > 0.15f)
            {
                wasClapDetected = false;
            }
        }
    }
    
    void DetermineInteractionMode()
    {
        previousInteractionMode = currentInteractionMode;
        
        // Priority 1: Both pinching = Scale (directional stretching)
        if (isLeftPinching && isRightPinching)
        {
            if (previousInteractionMode != InteractionMode.BothHands)
            {
                InitializeScale();
            }
            currentInteractionMode = InteractionMode.BothHands;
        }
        // Priority 2: Left hand only = Position
        else if (isLeftPinching)
        {
            if (previousInteractionMode != InteractionMode.LeftHandPosition)
            {
                InitializeGrabPosition();
            }
            currentInteractionMode = InteractionMode.LeftHandPosition;
        }
        // Priority 3: Right hand only = Rotation
        else if (isRightPinching)
        {
            if (previousInteractionMode != InteractionMode.RightHandRotation)
            {
                InitializeRotation();
            }
            currentInteractionMode = InteractionMode.RightHandRotation;
        }
        else
        {
            currentInteractionMode = InteractionMode.None;
        }
        
        if (currentInteractionMode != previousInteractionMode)
        {
            UpdateGuideText();
            
            // Reset auto-confirm timer when interaction mode changes
            timeAtThreshold = 0f;
            wasWithinThreshold = false;
        }
    }
    
    void InitializeGrabPosition()
    {
        usingLeftHand = true;  // Always left hand for position
        initialGrabHandPos = leftPinchPos;
        initialObjectPos = sourceTransform.position;
        initialGrabOffset = initialObjectPos - initialGrabHandPos;
    }
    
    void InitializeRotation()
    {
        usingLeftHand = false;  // Always right hand for rotation
        initialHandRotation = rightHandRotation;
        initialCubeRotation = sourceTransform.rotation;
    }
    
    void InitializeScale()
    {
        initialHandSeparation = rightPinchPos - leftPinchPos;
        initialScale = sourceTransform.localScale;
        scaleGrabCenter = sourceTransform.position;
        
        // Determine and LOCK the scale axis based on hand configuration
        DetermineScaleAxis();
    }
    
    void DetermineScaleAxis()
    {
        Vector3 handSeparation = rightPinchPos - leftPinchPos;
        
        // Transform world space hand separation to object's LOCAL space
        Vector3 localSeparation = sourceTransform.InverseTransformDirection(handSeparation);
        
        float localX = Mathf.Abs(localSeparation.x);
        float localY = Mathf.Abs(localSeparation.y);
        float localZ = Mathf.Abs(localSeparation.z);
        
        // Determine which LOCAL axis has the most separation
        if (localX > localY && localX > localZ)
        {
            lockedScaleAxis = 0; // Local X
            Debug.Log($"Scale locked to LOCAL X (separation: x={localX:F2}, y={localY:F2}, z={localZ:F2})");
        }
        else if (localY > localX && localY > localZ)
        {
            lockedScaleAxis = 1; // Local Y
            Debug.Log($"Scale locked to LOCAL Y (separation: x={localX:F2}, y={localY:F2}, z={localZ:F2})");
        }
        else
        {
            lockedScaleAxis = 2; // Local Z
            Debug.Log($"Scale locked to LOCAL Z (separation: x={localX:F2}, y={localY:F2}, z={localZ:F2})");
        }
    }
    
    void ExecuteInteraction()
    {
        if (currentInteractionMode == InteractionMode.BothHands)
        {
            HandleScale();
        }
        else if (currentInteractionMode == InteractionMode.LeftHandPosition)
        {
            HandleGrabPosition();
        }
        else if (currentInteractionMode == InteractionMode.RightHandRotation)
        {
            HandleRotation();
        }
    }
    
    void HandleGrabPosition()
    {
        // Always use left hand for position
        Vector3 currentHandPos = leftPinchPos;
        
        // Calculate hand movement delta and amplify it
        Vector3 handMovement = currentHandPos - initialGrabHandPos;
        Vector3 amplifiedMovement = handMovement * movementMultiplier;
        
        // Apply amplified movement to initial object position
        sourceTransform.position = initialObjectPos + amplifiedMovement;
    }
    
    void HandleRotation()
    {
        // Always use right hand for rotation
        Quaternion currentHandRotation = rightHandRotation;
        Quaternion rotationDelta = currentHandRotation * Quaternion.Inverse(initialHandRotation);
        
        // Amplify rotation for more responsive control
        rotationDelta.ToAngleAxis(out float angle, out Vector3 axis);
        angle *= rotationMultiplier;  // Amplify the rotation angle
        rotationDelta = Quaternion.AngleAxis(angle, axis);
        
        sourceTransform.rotation = rotationDelta * initialCubeRotation;
    }
    
    void HandleScale()
    {
        Vector3 currentSeparation = rightPinchPos - leftPinchPos;
        
        // Transform both separations to LOCAL space
        Vector3 initialSepLocal = sourceTransform.InverseTransformDirection(initialHandSeparation);
        Vector3 currentSepLocal = sourceTransform.InverseTransformDirection(currentSeparation);
        
        // Calculate scale ratio on the locked LOCAL axis
        float scaleRatio = 1.0f;
        
        if (lockedScaleAxis == 0) // Local X
        {
            scaleRatio = Mathf.Abs(currentSepLocal.x) / Mathf.Max(Mathf.Abs(initialSepLocal.x), 0.001f);
        }
        else if (lockedScaleAxis == 1) // Local Y
        {
            scaleRatio = Mathf.Abs(currentSepLocal.y) / Mathf.Max(Mathf.Abs(initialSepLocal.y), 0.001f);
        }
        else if (lockedScaleAxis == 2) // Local Z
        {
            scaleRatio = Mathf.Abs(currentSepLocal.z) / Mathf.Max(Mathf.Abs(initialSepLocal.z), 0.001f);
        }
        
        // Apply reduced sensitivity
        scaleRatio = Mathf.Lerp(1.0f, scaleRatio, scaleSensitivity);
        
        // Apply scale ONLY to locked axis, keep others at initial values
        Vector3 newScale = initialScale;
        if (lockedScaleAxis == 0)
            newScale.x = Mathf.Clamp(initialScale.x * scaleRatio, 0.1f, 5.0f);
        else if (lockedScaleAxis == 1)
            newScale.y = Mathf.Clamp(initialScale.y * scaleRatio, 0.1f, 5.0f);
        else if (lockedScaleAxis == 2)
            newScale.z = Mathf.Clamp(initialScale.z * scaleRatio, 0.1f, 5.0f);
        
        sourceTransform.localScale = newScale;
        
        // Keep object at original position
        sourceTransform.position = scaleGrabCenter;
    }
    
    void UpdateVisuals()
    {
        // Update edge highlighting
        UpdateEdgeVisualization();
    }
    
    void UpdateEdgeVisualization()
    {
        if (currentInteractionMode != InteractionMode.BothHands)
        {
            foreach (var line in edgeLines)
                line.enabled = false;
            return;
        }
        
        // Get cube bounds in local space
        Vector3 scale = sourceTransform.localScale;
        Vector3 halfScale = scale * 0.5f;
        
        Vector3[] cornersLocal = new Vector3[8]
        {
            new Vector3(-halfScale.x, -halfScale.y, -halfScale.z),
            new Vector3( halfScale.x, -halfScale.y, -halfScale.z),
            new Vector3( halfScale.x,  halfScale.y, -halfScale.z),
            new Vector3(-halfScale.x,  halfScale.y, -halfScale.z),
            new Vector3(-halfScale.x, -halfScale.y,  halfScale.z),
            new Vector3( halfScale.x, -halfScale.y,  halfScale.z),
            new Vector3( halfScale.x,  halfScale.y,  halfScale.z),
            new Vector3(-halfScale.x,  halfScale.y,  halfScale.z)
        };
        
        // Convert corners to world space
        Vector3[] cornersWorld = new Vector3[8];
        for (int i = 0; i < 8; i++)
        {
            cornersWorld[i] = sourceTransform.TransformPoint(cornersLocal[i]);
        }
        
        int[,] edges = new int[12, 2]
        {
            {0, 1}, {1, 2}, {2, 3}, {3, 0},
            {4, 5}, {5, 6}, {6, 7}, {7, 4},
            {0, 4}, {1, 5}, {2, 6}, {3, 7}
        };
        
        // Use the LOCKED scale axis (determined when scaling started)
        bool stretchingX = lockedScaleAxis == 0;
        bool stretchingY = lockedScaleAxis == 1;
        bool stretchingZ = lockedScaleAxis == 2;
        
        // Determine color based on LOCKED stretch direction
        Color edgeColor = Color.yellow;
        if (stretchingX) edgeColor = Color.red;
        else if (stretchingY) edgeColor = Color.green;
        else if (stretchingZ) edgeColor = Color.blue;
        
        // Find the closest edge to each hand aligned with the locked axis
        int closestLeftEdge = -1;
        int closestRightEdge = -1;
        float minLeftDist = float.MaxValue;
        float minRightDist = float.MaxValue;
        
        for (int i = 0; i < 12; i++)
        {
            // Get edge direction in LOCAL space (for axis alignment check)
            Vector3 p1Local = cornersLocal[edges[i, 0]];
            Vector3 p2Local = cornersLocal[edges[i, 1]];
            Vector3 localEdgeDir = (p2Local - p1Local).normalized;
            
            // Check if edge is aligned with the locked LOCAL axis
            bool isAligned = false;
            if (stretchingX && Mathf.Abs(localEdgeDir.x) > 0.9f) isAligned = true;
            else if (stretchingY && Mathf.Abs(localEdgeDir.y) > 0.9f) isAligned = true;
            else if (stretchingZ && Mathf.Abs(localEdgeDir.z) > 0.9f) isAligned = true;
            
            if (isAligned)
            {
                // Use WORLD space positions for distance calculations
                Vector3 p1World = cornersWorld[edges[i, 0]];
                Vector3 p2World = cornersWorld[edges[i, 1]];
                Vector3 edgeCenterWorld = (p1World + p2World) * 0.5f;
                
                // Distance to left hand
                float leftDist = Vector3.Distance(edgeCenterWorld, leftPinchPos);
                if (leftDist < minLeftDist)
                {
                    minLeftDist = leftDist;
                    closestLeftEdge = i;
                }
                
                // Distance to right hand
                float rightDist = Vector3.Distance(edgeCenterWorld, rightPinchPos);
                if (rightDist < minRightDist)
                {
                    minRightDist = rightDist;
                    closestRightEdge = i;
                }
            }
        }
        
        // Highlight only the two edges closest to each hand
        for (int i = 0; i < 12; i++)
        {
            LineRenderer lr = edgeLines[i];
            
            if (i == closestLeftEdge || i == closestRightEdge)
            {
                Vector3 p1World = cornersWorld[edges[i, 0]];
                Vector3 p2World = cornersWorld[edges[i, 1]];
                
                lr.enabled = true;
                lr.useWorldSpace = true;
                lr.SetPosition(0, p1World);
                lr.SetPosition(1, p2World);
                lr.startColor = edgeColor;
                lr.endColor = edgeColor;
            }
            else
            {
                lr.enabled = false;
            }
        }
    }
    
    void RotateSpace()
    {
        Transform originalSourceParent = sourceTransform.parent;
        Transform originalTargetParent = evaluator.targetGameObject.transform.parent;
        
        sourceTransform.SetParent(spacePivot.transform);
        evaluator.targetGameObject.transform.SetParent(spacePivot.transform);
        
        spacePivot.transform.Rotate(0f, 90f, 0f, Space.World);
        
        sourceTransform.SetParent(originalSourceParent);
        evaluator.targetGameObject.transform.SetParent(originalTargetParent);
        
        Debug.Log("Space rotated 90 degrees");
    }
    
    void UpdateGuideText()
    {
        string interactionText = currentInteractionMode switch
        {
            InteractionMode.None => "Left: Move | Right: Rotate | Both: Stretch",
            InteractionMode.LeftHandPosition => "Moving",
            InteractionMode.RightHandRotation => "Rotating",
            InteractionMode.BothHands => "Stretching",
            _ => ""
        };
        
        guideText.text = $"{interactionText}\nClap: Rotate Space";
    }
    
    void CheckAutoConfirm()
    {
        // ONLY check auto-confirm when NOT actively interacting
        if (currentInteractionMode != InteractionMode.None)
        {
            // User is actively manipulating - reset timer
            timeAtThreshold = 0f;
            wasWithinThreshold = false;
            return;
        }
        
        float distance = (evaluator.targetGameObject.transform.position - sourceTransform.position).magnitude;
        float diffRotation = Quaternion.Angle(evaluator.targetGameObject.transform.rotation, sourceTransform.rotation);
        float diffScale = (evaluator.targetGameObject.transform.localScale - sourceTransform.localScale).magnitude;
        
        // Check each condition individually
        bool distanceOK = distance < thresholdDistance;
        bool rotationOK = diffRotation < thresholdRotation;
        bool scaleOK = diffScale < thresholdScale;
        
        // ALL three must be satisfied
        bool withinThreshold = distanceOK && rotationOK && scaleOK;
        
        if (withinThreshold)
        {
            if (!wasWithinThreshold)
            {
                // Just entered threshold zone
                timeAtThreshold = 0f;
                Debug.Log($"Entered threshold zone - Distance: {distance:F3}/{thresholdDistance}, Rotation: {diffRotation:F1}/{thresholdRotation}, Scale: {diffScale:F3}/{thresholdScale}");
            }
            else
            {
                timeAtThreshold += Time.deltaTime;
                
                if (timeAtThreshold >= autoConfirmDelay)
                {
                    Debug.Log($"AUTO-CONFIRM! Distance: {distance:F3}/{thresholdDistance}, Rotation: {diffRotation:F1}/{thresholdRotation}, Scale: {diffScale:F3}/{thresholdScale}");
                    evaluator.ConfirmSelection();
                    timeAtThreshold = 0f;
                }
            }
        }
        else
        {
            // Not within threshold - log which condition(s) failed
            if (wasWithinThreshold)
            {
                Debug.Log($"Exited threshold - Dist OK: {distanceOK} ({distance:F3}/{thresholdDistance}), Rot OK: {rotationOK} ({diffRotation:F1}/{thresholdRotation}), Scale OK: {scaleOK} ({diffScale:F3}/{thresholdScale})");
            }
            timeAtThreshold = 0f;
        }
        
        wasWithinThreshold = withinThreshold;
    }
    
    void OnDestroy()
    {
        if (spacePivot != null) Destroy(spacePivot);
        if (edgeVisualizer != null) Destroy(edgeVisualizer);
    }
}