using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

public class RayNavigate : MonoBehaviour
{
    [Header("Rig & Controllers")]
    public Transform rigAnchor;       // Player root node
    public Transform headTransform;   // Head determines forward direction (CenterEyeAnchor)
    public Transform rightHandAnchor;
    public Transform leftHandAnchor;
    public Transform rayOrigin;
    public LayerMask teleportMask;
    public LayerMask moveLayerMask;

    [Header("Movement Settings")]
    public float baseSpeed = 0.5f;
    public float maxSpeed = 5f;
    public float accelerationSmoothTime = 0.6f;
    public float verticalSpeed = 0.8f;
    public float stepDistance = 0.5f;     // Fixed step distance
    public float rayLength = 20f;
    public float wallBuffer = 0.2f;

    [Header("Rotation Settings (Anti-Nausea Snap Turn)")]
    public float snapTurnAngle = 45f;
    private bool hasSnappedLeft = false;
    private bool hasSnappedRight = false;

    [Header("Ray Visuals")]
    public Color normalColor = Color.cyan;
    public Color hitColor = Color.green;

    private InputDevice rightHand;
    private InputDevice leftHand;
    private LineRenderer rayRenderer;
    private GameObject teleportMarker;

    private float currentSpeed = 0f;
    private float targetSpeed = 0f;
    private float speedSmoothVelocity = 0f;

    void Start()
    {
        TryInitializeDevices();
        SetupLineRenderer();
        SetupTeleportMarker();
    }

    void Update()
    {
        if (!rightHand.isValid || !leftHand.isValid)
            TryInitializeDevices();

        HandleMove();
        HandleStepMove(); // New: Right hand buttons for forward/backward movement
        HandleTurn();
        HandleRayTeleport();
    }

    // Initialize devices
    void TryInitializeDevices()
    {
        var rightDevices = new List<InputDevice>();
        var leftDevices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller, rightDevices);
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller, leftDevices);

        if (rightDevices.Count > 0)
            rightHand = rightDevices[0];
        if (leftDevices.Count > 0)
            leftHand = leftDevices[0];
    }

    // Collision avoidance logic
    void TryMove(Vector3 vec)
    {
        RaycastHit hit;
        Vector3 origin = rigAnchor.position;
        Vector3 direction = vec.normalized;
        float distance = vec.magnitude;

        if (Physics.Raycast(origin, direction, out hit, distance, moveLayerMask))
        {
            rigAnchor.position = origin + direction * Mathf.Max(0f, hit.distance - wallBuffer);
        }
        else
        {
            rigAnchor.position = origin + vec;
        }
    }

    // Smooth movement (head direction),right hand thumbstick for movement
    void HandleMove()
    {
        if (rightHand.isValid && rightHand.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 axis))
        {
            float inputMag = axis.magnitude;

            if (inputMag > 0.01f)
            {
                targetSpeed = Mathf.Lerp(baseSpeed, maxSpeed, inputMag);
                currentSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed, ref speedSmoothVelocity, accelerationSmoothTime);

                Vector3 forward = new Vector3(headTransform.forward.x, 0, headTransform.forward.z).normalized;
                Vector3 right = new Vector3(headTransform.right.x, 0, headTransform.right.z).normalized;
                Vector3 moveVec = (forward * axis.y + right * axis.x) * currentSpeed * Time.deltaTime;

                TryMove(moveVec);
            }
            else
            {
                currentSpeed = Mathf.SmoothDamp(currentSpeed, 0f, ref speedSmoothVelocity, accelerationSmoothTime);
            }
        }

        // Left hand button : primary=down, secondary=up
        if (leftHand.isValid && leftHand.TryGetFeatureValue(CommonUsages.primaryButton, out bool down) && down)
        {
            TryMove(Vector3.down * verticalSpeed * Time.deltaTime * 20f);
        }

        if (leftHand.isValid && leftHand.TryGetFeatureValue(CommonUsages.secondaryButton, out bool up) && up)
        {
            TryMove(Vector3.up * verticalSpeed * Time.deltaTime * 20f);
        }
    }

    // New: Right hand two buttons for fixed distance forward/backward movement
    void HandleStepMove()
    {
        if (!rightHand.isValid) return;

        // PrimaryButton = step forward
        if (rightHand.TryGetFeatureValue(CommonUsages.secondaryButton, out bool forward) && forward)
        {
            Vector3 forwardDir = new Vector3(headTransform.forward.x, 0, headTransform.forward.z).normalized;
            TryMove(forwardDir * stepDistance);
        }

        // SecondaryButton = step backward
        if (rightHand.TryGetFeatureValue(CommonUsages.primaryButton, out bool back) && back)
        {
            Vector3 backDir = new Vector3(headTransform.forward.x, 0, headTransform.forward.z).normalized;
            TryMove(-backDir * stepDistance);
        }
    }

    // left hand thumbstick for rotation: Anti-nausea Snap Turn
    void HandleTurn()
    {
        if (leftHand.isValid && leftHand.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 axis))
        {
            float turnInput = axis.x;

            if (turnInput > 0.6f && !hasSnappedRight)
            {
                rigAnchor.Rotate(Vector3.up * snapTurnAngle);
                hasSnappedRight = true;
            }
            else if (turnInput < -0.6f && !hasSnappedLeft)
            {
                rigAnchor.Rotate(Vector3.up * -snapTurnAngle);
                hasSnappedLeft = true;
            }
            else if (Mathf.Abs(turnInput) < 0.3f)
            {
                hasSnappedLeft = false;
                hasSnappedRight = false;
            }
        }
    }

    // ray for teleportation: Ray detection + color change on hit
    void HandleRayTeleport()
    {
        if (rayOrigin == null) return;

        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
        bool hitSomething = Physics.Raycast(ray, out RaycastHit hit, rayLength, teleportMask);

        rayRenderer.SetPosition(0, ray.origin);
        rayRenderer.SetPosition(1, hitSomething ? hit.point : ray.origin + ray.direction * rayLength);
        rayRenderer.material.color = hitSomething ? hitColor : normalColor;

        teleportMarker.SetActive(hitSomething);
        if (hitSomething)
        {
            teleportMarker.transform.position = hit.point + Vector3.up * 0.01f;
        }

        if (rightHand.isValid && rightHand.TryGetFeatureValue(CommonUsages.triggerButton, out bool trigger) && trigger && hitSomething)
        {
            Vector3 target = hit.point + Vector3.up * 0.1f;
            rigAnchor.position = target;
            teleportMarker.SetActive(false);
        }
    }

    // Ray visualization
    void SetupLineRenderer()
    {
        rayRenderer = gameObject.AddComponent<LineRenderer>();
        rayRenderer.startWidth = 0.01f;
        rayRenderer.endWidth = 0.002f;
        rayRenderer.material = new Material(Shader.Find("Unlit/Color"));
        rayRenderer.material.color = normalColor;
        rayRenderer.positionCount = 2;
    }

    void SetupTeleportMarker()
    {
        teleportMarker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        teleportMarker.transform.localScale = new Vector3(0.5f, 0.01f, 0.5f);
        teleportMarker.GetComponent<Collider>().enabled = false;
        teleportMarker.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Unlit/Color"));
        teleportMarker.GetComponent<MeshRenderer>().material.color = new Color(0, 1, 0, 0.6f);
        teleportMarker.SetActive(false);
    }
}
