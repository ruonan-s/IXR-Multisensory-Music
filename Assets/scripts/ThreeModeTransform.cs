using UnityEngine;
using Text = TMPro.TMP_Text;

public class ThreeModeTransform : MonoBehaviour
{
    [Header("References")]
    public TransformationEvaluator evaluator;
    public Text guideText;
    
    [Header("Speed Settings")]
    public float positionSpeed = 1.0f;
    public float rotationSpeed = 60.0f;  // degrees per second
    public float scaleSpeed = 0.5f;
    
    private Transform sourceTransform;
    private enum Mode { Position, Rotation, Scale }
    private Mode currentMode = Mode.Position;
    
    // Track button states to detect single presses
    private bool wasRightHandTriggerPressed = false;
    private bool wasButtonAPressed = false;
    
    void Awake()
    {
        // Get the source transform
        sourceTransform = evaluator.GetSourceTransform();
        
        // Initialize UI
        UpdateGuideText();
    }
    
    void UpdateGuideText()
    {
        string modeText = currentMode switch
        {
            Mode.Position => "POSITION",
            Mode.Rotation => "ROTATION",
            Mode.Scale => "SCALE",
            _ => "UNKNOWN"
        };
        
        guideText.text = $"Mode: {modeText}";
    }
    
    void Update()
    {
        // Handle mode cycling with right hand trigger (grip button)
        bool isRightHandTriggerPressed = OVRInput.Get(OVRInput.Button.PrimaryHandTrigger);
        if (isRightHandTriggerPressed && !wasRightHandTriggerPressed)
        {
            // Hand trigger just pressed - cycle mode
            currentMode = (Mode)(((int)currentMode + 1) % 3);
            UpdateGuideText();
        }
        wasRightHandTriggerPressed = isRightHandTriggerPressed;
        
        // Handle confirmation with button A
        bool isButtonAPressed = OVRInput.Get(OVRInput.Button.One); // Button A on right controller
        if (isButtonAPressed && !wasButtonAPressed)
        {
            // Button A just pressed - confirm selection
            evaluator.ConfirmSelection();
        }
        wasButtonAPressed = isButtonAPressed;
        
        // Read joystick inputs - SWAPPED TO FIX THE ISSUE
        Vector2 leftStick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);   // This is actually LEFT controller
        Vector2 rightStick = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick); // This is actually RIGHT controller
        
        // Apply transformations based on current mode
        switch (currentMode)
        {
            case Mode.Position:
                HandlePosition(rightStick, leftStick.y);
                break;
                
            case Mode.Rotation:
                HandleRotation(rightStick, leftStick.y);
                break;
                
            case Mode.Scale:
                HandleScaleDeadzone(rightStick, leftStick.y);
                break;
        }
    }
    
    void HandlePosition(Vector2 rightStick, float leftStickY)
    {
        // Right stick: X and Y position
        // Left stick Y: Z position
        Vector3 movement = new Vector3(
            rightStick.x * positionSpeed * Time.deltaTime,
            rightStick.y * positionSpeed * Time.deltaTime,
            leftStickY * positionSpeed * Time.deltaTime
        );
        
        sourceTransform.position += movement;
    }
    
    void HandleRotation(Vector2 rightStick, float leftStickY)
    {
        // Right stick X: Yaw (Y-axis rotation)
        // Right stick Y: Pitch (X-axis rotation)
        // Left stick Y: Roll (Z-axis rotation)
        Vector3 rotation = new Vector3(
            rightStick.y * rotationSpeed * Time.deltaTime,   // Pitch
            rightStick.x * rotationSpeed * Time.deltaTime,   // Yaw
            leftStickY * rotationSpeed * Time.deltaTime      // Roll
        );
        
        sourceTransform.Rotate(rotation, Space.World);
    }
    
    void HandleScale(Vector2 rightStick, float leftStickY)
    {
        // Non-uniform scaling - each axis independently controlled
        // Right stick X: Length (X-axis scale)
        // Right stick Y: Height (Y-axis scale)
        // Left stick Y: Width (Z-axis scale)
        
        float scaleDeltaX = rightStick.x * scaleSpeed * Time.deltaTime;  // Length
        float scaleDeltaY = rightStick.y * scaleSpeed * Time.deltaTime;  // Height
        float scaleDeltaZ = leftStickY * scaleSpeed * Time.deltaTime;    // Width
        
        Vector3 newScale = sourceTransform.localScale + new Vector3(scaleDeltaX, scaleDeltaY, scaleDeltaZ);
        
        // Prevent negative or zero scale
        newScale.x = Mathf.Max(0.1f, newScale.x);
        newScale.y = Mathf.Max(0.1f, newScale.y);
        newScale.z = Mathf.Max(0.1f, newScale.z);
        
        sourceTransform.localScale = newScale;
    }

    void HandleScaleDeadzone(Vector2 rightStick, float leftStickY)
    {
        // Non-uniform scaling with deadzone
        float deadzone = 0.3f; // Adjust this value (0.2-0.4 works well)
        
        float scaleDeltaX = 0f;
        float scaleDeltaY = 0f;
        float scaleDeltaZ = 0f;
        
        // Only apply X scaling if X movement is dominant
        if (Mathf.Abs(rightStick.x) > deadzone && Mathf.Abs(rightStick.x) > Mathf.Abs(rightStick.y))
        {
            scaleDeltaX = rightStick.x * scaleSpeed * Time.deltaTime;
        }
        
        // Only apply Y scaling if Y movement is dominant
        if (Mathf.Abs(rightStick.y) > deadzone && Mathf.Abs(rightStick.y) > Mathf.Abs(rightStick.x))
        {
            scaleDeltaY = rightStick.y * scaleSpeed * Time.deltaTime;
        }
        
        // Z axis is simpler since it's on a different stick
        if (Mathf.Abs(leftStickY) > deadzone)
        {
            scaleDeltaZ = leftStickY * scaleSpeed * Time.deltaTime;
        }
        
        Vector3 newScale = sourceTransform.localScale + new Vector3(scaleDeltaX, scaleDeltaY, scaleDeltaZ);
        
        // Prevent negative or zero scale
        newScale.x = Mathf.Max(0.1f, newScale.x);
        newScale.y = Mathf.Max(0.1f, newScale.y);
        newScale.z = Mathf.Max(0.1f, newScale.z);
        
        sourceTransform.localScale = newScale;
    }
}