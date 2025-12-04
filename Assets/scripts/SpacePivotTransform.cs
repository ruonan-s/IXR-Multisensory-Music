using UnityEngine;
using Text = TMPro.TMP_Text;

public class SpacePivotTransform : MonoBehaviour
{
    [Header("References")]
    public TransformationEvaluator evaluator;
    public Text guideText;
    public Transform rightControllerTransform;
    public Transform leftControllerTransform;
    
    [Header("Settings")]
    public float positionSpeed = 1.0f;
    public float autoConfirmDelay = 0.5f;
    
    private Transform sourceTransform;
    
    // Track grip states
    private bool wasLeftGripPressed = false;
    private bool wasBothGripPressed = false;
    private bool wasButtonAPressed = false;
    
    // Store initial rotation state
    private Quaternion initialControllerRotation;
    private Quaternion initialCubeRotation;
    
    // For scaling - track which axis to scale based on space rotation
    private float initialHandDistance;
    private Vector3 initialScale;
    private int spaceRotationCount = 0; // 0-5 for 6 different viewing angles
    
    // Space rotation pivot
    private GameObject spacePivot;
    
    // Auto-confirm tracking
    private float timeAtThreshold = 0f;
    private bool wasWithinThreshold = false;
    
    void Awake()
    {
        sourceTransform = evaluator.GetSourceTransform();
        
        spacePivot = new GameObject("SpacePivot");
        spacePivot.transform.position = evaluator.spawnBoxCenter;
        
        UpdateGuideText();
    }
    
    void UpdateGuideText()
    {
        guideText.text = "Left Grip: Rotate | Both Grips: Scale Depth | Button A: Cycle View (6 angles)";
    }
    
    void Update()
    {
        bool isRightGripPressed = OVRInput.Get(OVRInput.Button.SecondaryHandTrigger);
        bool isLeftGripPressed = OVRInput.Get(OVRInput.Button.PrimaryHandTrigger);
        bool bothGripsPressed = isRightGripPressed && isLeftGripPressed;
        
        // Handle Button A for space rotation
        bool isButtonAPressed = OVRInput.Get(OVRInput.Button.One);
        if (isButtonAPressed && !wasButtonAPressed)
        {
            RotateSpace();
        }
        wasButtonAPressed = isButtonAPressed;
        
        // Handle both grips (scaling along world Z-axis ONLY)
        if (bothGripsPressed)
        {
            if (!wasBothGripPressed)
            {
                initialHandDistance = Vector3.Distance(leftControllerTransform.position, rightControllerTransform.position);
                initialScale = sourceTransform.localScale;
            }
            
            HandleScaling();
            wasBothGripPressed = true;
        }
        // Handle left grip only (rotation)
        else if (isLeftGripPressed)
        {
            if (!wasLeftGripPressed || wasBothGripPressed)
            {
                initialControllerRotation = rightControllerTransform.rotation;
                initialCubeRotation = sourceTransform.rotation;
            }
            
            HandleRotation();
            wasLeftGripPressed = true;
            wasBothGripPressed = false;
        }
        else
        {
            wasLeftGripPressed = false;
            wasBothGripPressed = false;
        }
        
        wasLeftGripPressed = isLeftGripPressed;
        
        // Always handle position with joysticks
        HandlePosition();
        
        // Auto-confirm check
        CheckAutoConfirm();
    }
    
    void RotateSpace()
    {
        Transform originalSourceParent = sourceTransform.parent;
        Transform originalTargetParent = evaluator.targetGameObject.transform.parent;
        
        sourceTransform.SetParent(spacePivot.transform);
        evaluator.targetGameObject.transform.SetParent(spacePivot.transform);
        
        // Track rotation count (0-5 for 6 different angles)
        spaceRotationCount = (spaceRotationCount + 1) % 6;
        
        // Set absolute rotation for each of the 6 positions
        Vector3 targetRotation = Vector3.zero;
        switch (spaceRotationCount)
        {
            case 0: targetRotation = new Vector3(0, 0, 0); break;      // Front - scale X
            case 1: targetRotation = new Vector3(0, 90, 0); break;     // Right - scale Z
            case 2: targetRotation = new Vector3(90, 0, 0); break;     // Top - scale Y
            case 3: targetRotation = new Vector3(0, 180, 0); break;    // Back - scale X
            case 4: targetRotation = new Vector3(0, 270, 0); break;    // Left - scale Z
            case 5: targetRotation = new Vector3(-90, 0, 0); break;    // Bottom - scale Y
        }
        
        spacePivot.transform.rotation = Quaternion.Euler(targetRotation);
        
        sourceTransform.SetParent(originalSourceParent);
        evaluator.targetGameObject.transform.SetParent(originalTargetParent);
    }
    
    void HandlePosition()
    {
        Vector2 rightStick = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);
        Vector2 leftStick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
        
        Vector3 movement = new Vector3(
            rightStick.x * positionSpeed * Time.deltaTime,
            rightStick.y * positionSpeed * Time.deltaTime,
            leftStick.y * positionSpeed * Time.deltaTime
        );
        
        sourceTransform.position += movement;
    }
    
    void HandleRotation()
    {
        Quaternion controllerRotationDelta = rightControllerTransform.rotation * Quaternion.Inverse(initialControllerRotation);
        sourceTransform.rotation = controllerRotationDelta * initialCubeRotation;
    }
    
    void HandleScaling()
    {
        // Scale along world Z-axis (depth direction)
        // Based on space rotation, this corresponds to different local axes
        float currentHandDistance = Vector3.Distance(leftControllerTransform.position, rightControllerTransform.position);
        float scaleRatio = currentHandDistance / initialHandDistance;
        
        Vector3 newScale = sourceTransform.localScale;
        
        // Determine which local axis to scale based on space rotation (6 angles)
        switch (spaceRotationCount)
        {
            case 0: // Front view - scale local X
                newScale.x = initialScale.x * scaleRatio;
                break;
            case 1: // Right view (90° Y) - scale local Z
                newScale.z = initialScale.z * scaleRatio;
                break;
            case 2: // Top view (90° X) - scale local Y
                newScale.y = initialScale.y * scaleRatio;
                break;
            case 3: // Back view (180° Y) - scale local X
                newScale.x = initialScale.x * scaleRatio;
                break;
            case 4: // Left view (270° Y) - scale local Z
                newScale.z = initialScale.z * scaleRatio;
                break;
            case 5: // Bottom view (-90° X) - scale local Y
                newScale.y = initialScale.y * scaleRatio;
                break;
        }
        
        newScale.x = Mathf.Clamp(newScale.x, 0.1f, 5.0f);
        newScale.y = Mathf.Clamp(newScale.y, 0.1f, 5.0f);
        newScale.z = Mathf.Clamp(newScale.z, 0.1f, 5.0f);
        
        sourceTransform.localScale = newScale;
    }
    
    void CheckAutoConfirm()
    {
        float distance = (evaluator.targetGameObject.transform.position - sourceTransform.position).magnitude;
        float diffRotation = Quaternion.Angle(evaluator.targetGameObject.transform.rotation, sourceTransform.rotation);
        float diffScale = (evaluator.targetGameObject.transform.localScale - sourceTransform.localScale).magnitude;
        
        bool withinThreshold = (distance < 0.2f && diffRotation < 20f && diffScale < 0.2f);
        
        if (withinThreshold)
        {
            if (!wasWithinThreshold)
            {
                timeAtThreshold = 0f;
            }
            else
            {
                timeAtThreshold += Time.deltaTime;
                
                if (timeAtThreshold >= autoConfirmDelay)
                {
                    evaluator.ConfirmSelection();
                    timeAtThreshold = 0f;
                }
            }
        }
        else
        {
            timeAtThreshold = 0f;
        }
        
        wasWithinThreshold = withinThreshold;
    }
    
    void OnDestroy()
    {
        if (spacePivot != null)
        {
            Destroy(spacePivot);
        }
    }
}