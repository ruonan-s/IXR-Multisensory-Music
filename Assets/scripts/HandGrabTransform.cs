using UnityEngine;
using Text = TMPro.TMP_Text;

public class GripTransform : MonoBehaviour
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
    private bool wasRightGripPressed = false;
    private bool wasLeftGripPressed = false;
    private bool wasBothGripPressed = false;
    private bool wasButtonAPressed = false;
    
    // Store initial rotation state
    private Quaternion initialControllerRotation;
    private Quaternion initialCubeRotation;
    
    // For scaling
    private enum ScaleAxis { X, Y, Z }
    private ScaleAxis currentScaleAxis = ScaleAxis.X;
    private float initialHandDistance;
    private Vector3 initialScale;
    
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
        string axisName = currentScaleAxis switch
        {
            ScaleAxis.X => "red",
            ScaleAxis.Y => "green",
            ScaleAxis.Z => "blue",
            _ => "red"
        };
        
        guideText.text = $"{axisName}";
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
        
        // Handle right grip to switch scale axis (only when left grip is NOT pressed)
        if (isRightGripPressed && !wasRightGripPressed && !isLeftGripPressed)
        {
            CycleScaleAxis();
        }
        
        // Handle both grips (scaling)
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
                initialControllerRotation = rightControllerTransform.rotation;  // FIXED: use right controller
                initialCubeRotation = sourceTransform.rotation;
            }
            
            HandleRotation();
            wasLeftGripPressed = true;
            wasBothGripPressed = false;
        }
        else
        {
            wasLeftGripPressed = false;
            wasRightGripPressed = false;  // FIXED: reset both
            wasBothGripPressed = false;
        }
        
        wasRightGripPressed = isRightGripPressed;  // FIXED: moved outside else block
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
        
        spacePivot.transform.Rotate(0f, 90f, 0f, Space.World);
        
        sourceTransform.SetParent(originalSourceParent);
        evaluator.targetGameObject.transform.SetParent(originalTargetParent);
    }
    
    void CycleScaleAxis()
    {
        currentScaleAxis = currentScaleAxis switch
        {
            ScaleAxis.X => ScaleAxis.Y,
            ScaleAxis.Y => ScaleAxis.Z,
            ScaleAxis.Z => ScaleAxis.X,
            _ => ScaleAxis.X
        };
        UpdateGuideText();
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
        float currentHandDistance = Vector3.Distance(leftControllerTransform.position, rightControllerTransform.position);
        float scaleRatio = currentHandDistance / initialHandDistance;
        
        Vector3 newScale = sourceTransform.localScale;
        
        switch (currentScaleAxis)
        {
            case ScaleAxis.X:
                newScale.x = initialScale.x * scaleRatio;
                break;
            case ScaleAxis.Y:
                newScale.y = initialScale.y * scaleRatio;
                break;
            case ScaleAxis.Z:
                newScale.z = initialScale.z * scaleRatio;
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