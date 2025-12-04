using UnityEngine;
using UnityEngine.UI;

public class FishGalleryGesture : MonoBehaviour
{
    [Header("Fish Resources")]
    [Tooltip("Name of folder inside Resources/ containing fish sprites")]
    public string resourcesFolder = "Fish";

    [Header("Canvas Settings")]
    [Tooltip("Distance from camera in meters")]
    public float distanceFromCamera = 2f;
    
    [Tooltip("Height offset from camera (0 = eye level)")]
    public float heightOffset = 0f;
    
    [Tooltip("Canvas width in world units")]
    public float canvasWidth = 2f;
    
    [Tooltip("Canvas height in world units")]
    public float canvasHeight = 1.5f;

    [Header("Gesture Detection")]
    [Tooltip("Minimum hand velocity (m/s) to trigger swipe")]
    public float minSwipeVelocity = 1.5f;
    
    [Tooltip("Cooldown time between swipes (seconds)")]
    public float swipeCooldown = 0.5f;

    [Tooltip("Which hand to track: Left, Right, or Both")]
    public HandTracking trackHand = HandTracking.Both;

    [Header("Debug")]
    public bool debugLogs = true;
    public bool showVelocityInUI = false;

    public enum HandTracking { Left, Right, Both }

    // Private references
    private Canvas canvas;
    private Image fishImage;
    private Text debugText;
    private Sprite[] fishSprites;
    private int currentIndex = 0;
    private Camera mainCamera;
    
    // Hand tracking
    private OVRHand leftHand;
    private OVRHand rightHand;
    private Vector3 lastLeftHandPos;
    private Vector3 lastRightHandPos;
    private float lastSwipeTime = 0f;

    void Start()
    {
        // Find the main camera (center eye camera)
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("FishGalleryGesture: No main camera found! Make sure your OVR CenterEyeAnchor has MainCamera tag.");
            return;
        }

        // Create the UI first so we can display debug info
        CreateCanvas();
        LoadFishSprites();
        ShowCurrentFish();

        // Find OVR Hands
        FindOVRHands();

        if (leftHand != null || rightHand != null)
        {
            Debug.Log($"FishGalleryGesture: ✓ Hand tracking initialized. Left: {leftHand != null}, Right: {rightHand != null}");
            
            if (debugText != null)
            {
                debugText.text = $"START OK\nL:{leftHand != null}\nR:{rightHand != null}\nUpdate will start...";
                debugText.color = Color.green;
            }
        }
        else
        {
            Debug.LogError("FishGalleryGesture: ✗ NO OVRHand components found!");
            Debug.LogError("FishGalleryGesture: Please check your OVR Camera Rig setup.");
            
            // Show error on canvas
            if (debugText != null)
            {
                debugText.text = "ERROR: No OVRHand found!\nCheck OVR Camera Rig\n\nMake sure hand tracking\nis enabled in your scene!";
                debugText.color = Color.red;
                debugText.fontSize = 40;
            }
        }
    }

    void FindOVRHands()
    {
        // Find all OVRHand components in the scene
        OVRHand[] hands = FindObjectsByType<OVRHand>(FindObjectsSortMode.None);
        
        Debug.Log($"FishGalleryGesture: Found {hands.Length} OVRHand components in scene");
        
        foreach (OVRHand hand in hands)
        {
            Debug.Log($"FishGalleryGesture: Checking hand on GameObject: {hand.gameObject.name}");
            
            // Check the associated OVRSkeleton to determine which hand it is
            OVRSkeleton skeleton = hand.GetComponent<OVRSkeleton>();
            if (skeleton != null)
            {
                Debug.Log($"FishGalleryGesture: Skeleton type: {skeleton.GetSkeletonType()}");
                
                if (skeleton.GetSkeletonType() == OVRSkeleton.SkeletonType.HandLeft)
                {
                    leftHand = hand;
                    Debug.Log("FishGalleryGesture: ✓ Found LEFT hand (via skeleton)");
                }
                else if (skeleton.GetSkeletonType() == OVRSkeleton.SkeletonType.HandRight)
                {
                    rightHand = hand;
                    Debug.Log("FishGalleryGesture: ✓ Found RIGHT hand (via skeleton)");
                }
            }
            else
            {
                Debug.Log("FishGalleryGesture: No skeleton found, trying name-based detection");
                
                // Fallback: check parent name
                string objName = hand.gameObject.name.ToLower();
                string parentName = hand.transform.parent != null ? hand.transform.parent.name.ToLower() : "";
                
                Debug.Log($"FishGalleryGesture: Object name: '{objName}', Parent name: '{parentName}'");
                
                if (objName.Contains("left") || parentName.Contains("left"))
                {
                    leftHand = hand;
                    Debug.Log("FishGalleryGesture: ✓ Found LEFT hand (by name)");
                }
                else if (objName.Contains("right") || parentName.Contains("right"))
                {
                    rightHand = hand;
                    Debug.Log("FishGalleryGesture: ✓ Found RIGHT hand (by name)");
                }
            }
        }

        // Initialize positions
        if (leftHand != null)
            lastLeftHandPos = leftHand.transform.position;
        if (rightHand != null)
            lastRightHandPos = rightHand.transform.position;
    }

    void CreateCanvas()
    {
        // Create Canvas GameObject
        GameObject canvasObj = new GameObject("FishGalleryCanvas");
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        
        // Add CanvasScaler with better settings for world space
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.dynamicPixelsPerUnit = 10;

        // Set canvas size
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(1920, 1080);

        // Position canvas in front of camera
        PositionCanvasInFrontOfCamera();

        // Create background panel
        GameObject bgPanel = new GameObject("BackgroundPanel");
        bgPanel.transform.SetParent(canvasObj.transform, false);
        Image bgImage = bgPanel.AddComponent<Image>();
        bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        RectTransform bgRect = bgPanel.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        // Create fish image - smaller and at bottom
        GameObject fishImageObj = new GameObject("FishImage");
        fishImageObj.transform.SetParent(bgPanel.transform, false);
        fishImage = fishImageObj.AddComponent<Image>();
        fishImage.preserveAspect = true;
        RectTransform fishRect = fishImage.GetComponent<RectTransform>();
        fishRect.anchorMin = new Vector2(0.15f, 0.05f);  // Bottom half
        fishRect.anchorMax = new Vector2(0.85f, 0.45f);
        fishRect.sizeDelta = Vector2.zero;

        // Always create debug text at top - created AFTER image so it renders on top
        GameObject debugTextObj = new GameObject("DebugText");
        debugTextObj.transform.SetParent(bgPanel.transform, false);
        debugText = debugTextObj.AddComponent<Text>();
        debugText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        debugText.fontSize = 120;  // VERY large!
        debugText.color = Color.yellow;
        debugText.alignment = TextAnchor.UpperLeft;
        debugText.fontStyle = FontStyle.Bold;
        debugText.resizeTextForBestFit = false;
        RectTransform debugRect = debugText.GetComponent<RectTransform>();
        debugRect.anchorMin = new Vector2(0.05f, 0.5f);  // Top half
        debugRect.anchorMax = new Vector2(0.95f, 0.95f);
        debugRect.sizeDelta = Vector2.zero;
        debugRect.offsetMin = new Vector2(20, 0);
        debugRect.offsetMax = new Vector2(-20, 0);
        
        // Add shadow/outline for better visibility
        Outline outline = debugTextObj.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(3, -3);
        
        debugText.text = "Initializing...";

        if (debugLogs)
            Debug.Log("FishGalleryGesture: Canvas created and positioned");
    }

    void PositionCanvasInFrontOfCamera()
    {
        if (mainCamera == null || canvas == null) return;

        // Position in front of camera
        Vector3 targetPos = mainCamera.transform.position + 
                           mainCamera.transform.forward * distanceFromCamera +
                           Vector3.up * heightOffset;
        
        canvas.transform.position = targetPos;
        canvas.transform.rotation = Quaternion.LookRotation(canvas.transform.position - mainCamera.transform.position);

        // Set scale based on desired world size
        RectTransform rect = canvas.GetComponent<RectTransform>();
        float scale = canvasWidth / rect.sizeDelta.x;
        canvas.transform.localScale = new Vector3(scale, scale, scale);
    }

    void LoadFishSprites()
    {
        fishSprites = Resources.LoadAll<Sprite>(resourcesFolder);

        if (fishSprites == null || fishSprites.Length == 0)
        {
            Debug.LogWarning($"FishGalleryGesture: No sprites found in Resources/{resourcesFolder}. Creating test sprite.");
            CreateTestSprite();
        }
        else
        {
            if (debugLogs)
                Debug.Log($"FishGalleryGesture: Loaded {fishSprites.Length} fish sprites from Resources/{resourcesFolder}");
        }
    }

    void CreateTestSprite()
    {
        // Create a simple colored texture as fallback
        Texture2D tex = new Texture2D(512, 512);
        Color[] colors = new Color[512 * 512];
        
        for (int y = 0; y < 512; y++)
        {
            for (int x = 0; x < 512; x++)
            {
                float r = (float)x / 512f;
                float g = (float)y / 512f;
                float b = 0.5f;
                colors[y * 512 + x] = new Color(r, g, b);
            }
        }
        
        tex.SetPixels(colors);
        tex.Apply();

        Sprite testSprite = Sprite.Create(tex, new Rect(0, 0, 512, 512), new Vector2(0.5f, 0.5f));
        fishSprites = new Sprite[] { testSprite };
    }

    void ShowCurrentFish()
    {
        if (fishImage == null || fishSprites == null || fishSprites.Length == 0) return;
        
        fishImage.sprite = fishSprites[currentIndex];
        
        if (debugLogs)
            Debug.Log($"FishGalleryGesture: Showing fish {currentIndex + 1}/{fishSprites.Length}");
    }

    public void NextFish()
    {
        if (fishSprites == null || fishSprites.Length == 0) return;
        
        currentIndex = (currentIndex + 1) % fishSprites.Length;
        ShowCurrentFish();
    }

    public void PreviousFish()
    {
        if (fishSprites == null || fishSprites.Length == 0) return;
        
        currentIndex = (currentIndex - 1 + fishSprites.Length) % fishSprites.Length;
        ShowCurrentFish();
    }

    void Update()
    {
        // DIRECT UPDATE TEST - this should work if Update() is running
        if (debugText != null)
        {
            int frameCount = Time.frameCount;
            debugText.text = $"UPDATE!\nF:{frameCount}";
            debugText.color = Color.red;
        }
        
        // Reposition canvas with R key (for testing)
        if (Input.GetKeyDown(KeyCode.R))
        {
            PositionCanvasInFrontOfCamera();
            if (debugLogs) Debug.Log("FishGalleryGesture: Repositioned canvas");
        }

        // Always detect gestures and update debug display
        DetectSwipeGesture();
    }

    void DetectSwipeGesture()
    {
        // FORCE update debug text first to prove this function is called
        if (debugText != null)
        {
            debugText.text = "DetectSwipeGesture() CALLED!";
            debugText.color = Color.magenta;
        }
        
        Vector3 leftVelocity = Vector3.zero;
        Vector3 rightVelocity = Vector3.zero;
        bool leftTracked = false;
        bool rightTracked = false;
        Vector3 leftPos = Vector3.zero;
        Vector3 rightPos = Vector3.zero;

        // Calculate left hand velocity - just track position always
        if (leftHand != null && (trackHand == HandTracking.Left || trackHand == HandTracking.Both))
        {
            Vector3 currentPos = leftHand.transform.position;
            leftPos = currentPos;
            
            // Calculate velocity if we have a previous position
            if (lastLeftHandPos != Vector3.zero)
            {
                leftVelocity = (currentPos - lastLeftHandPos) / Time.deltaTime;
            }
            
            // Consider tracked if position is changing
            if (currentPos.magnitude > 0.01f)
            {
                leftTracked = true;
            }
            
            lastLeftHandPos = currentPos;
        }

        // Calculate right hand velocity
        if (rightHand != null && (trackHand == HandTracking.Right || trackHand == HandTracking.Both))
        {
            Vector3 currentPos = rightHand.transform.position;
            rightPos = currentPos;
            
            // Calculate velocity if we have a previous position
            if (lastRightHandPos != Vector3.zero)
            {
                rightVelocity = (currentPos - lastRightHandPos) / Time.deltaTime;
            }
            
            // Consider tracked if position is changing
            if (currentPos.magnitude > 0.01f)
            {
                rightTracked = true;
            }
            
            lastRightHandPos = currentPos;
        }

        // Choose the hand with higher velocity
        Vector3 velocity = leftVelocity.magnitude > rightVelocity.magnitude ? leftVelocity : rightVelocity;
        
        // Always update debug display with simplified, larger text
        if (debugText != null)
        {
            string statusText = $"F:{Time.frameCount} FISH:{currentIndex + 1}/{fishSprites.Length}\n\n";
            statusText += $"L: {(leftHand != null ? "OK" : "NO")} ";
            statusText += $"{(leftTracked ? "TRK" : "---")}\n";
            statusText += $"Pos:{leftPos.x:F2}\n";
            statusText += $"Vel:{leftVelocity.x:F1}\n\n";
            statusText += $"R: {(rightHand != null ? "OK" : "NO")} ";
            statusText += $"{(rightTracked ? "TRK" : "---")}\n";
            statusText += $"Pos:{rightPos.x:F2}\n";
            statusText += $"Vel:{rightVelocity.x:F1}\n";
            
            debugText.text = statusText;
        }

        // Check if enough time has passed since last swipe (cooldown)
        if (Time.time - lastSwipeTime < swipeCooldown)
            return;

        // Check if velocity is high enough for a swipe (using X axis for left/right)
        float horizontalVelocity = velocity.x;

        if (Mathf.Abs(horizontalVelocity) >= minSwipeVelocity)
        {
            lastSwipeTime = Time.time;

            if (horizontalVelocity > 0)
            {
                // Swipe right -> Previous fish
                PreviousFish();
                if (debugLogs) 
                    Debug.Log($"FishGalleryGesture: SWIPE RIGHT detected (velocity: {horizontalVelocity:F2} m/s) -> Previous");
            }
            else
            {
                // Swipe left -> Next fish
                NextFish();
                if (debugLogs) 
                    Debug.Log($"FishGalleryGesture: SWIPE LEFT detected (velocity: {horizontalVelocity:F2} m/s) -> Next");
            }
        }
    }
}

