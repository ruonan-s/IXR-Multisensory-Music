using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Video;
using System.Text;
using System;
using Oculus.Interaction.Input;

[System.Serializable]
public class ScaleData
{
    public string scale_category;
    public float scale_multiplier;
}

[System.Serializable]
public class ElementData
{
    public string image_name;
    public int layer;
    public string interaction;
    public ScaleData scale;
}

[System.Serializable]
public class ElementList
{
    public int paragraph;
    public string start_tms;
    public string end_tms;
    public ElementData[] elements;
}
[System.Serializable]
public class ElementListWrapper
{
    public ElementList[] paragraphs; 
}

public class ElementActivator : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public TextMeshPro timestampText;
    public TextAsset elementJsonFile; 
    public GameObject[] elementPrefabs;

    [Header("Hand References For 'Part' Interaction")]
    public Hand leftHand;
    public Hand rightHand;
    
    [Header("Anchor References For 'Shake' Interaction")]
    public Transform leftHandAnchor;
    public Transform rightHandAnchor;
    
    private GameObject spawnedParent;
    private ElementList[] allParagraphs;
    private ElementList activeParagraph;

    private double startTimeInSeconds = 0.0;
    private double endTimeInSeconds = 0.0;
    
    private bool hasLoggedStart = false; 
    private bool hasLoggedEnd = false;    

    private const float SpiralIncrement = 1.5f; 
    private const float CheckRadiusMultiplier = 0.6f;

    void Awake()
    {
        LoadElementData(); 
        ResolveHandReferences();
    }

    void Update()
    {
        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            double currentTime = videoPlayer.time;

            if (timestampText != null)
            {
                timestampText.text = $"{currentTime:F2} s";
            }
            
            Debug.Log("Video Time: " + currentTime.ToString("F3") + "s");
            ManageActiveParagraph(currentTime);

            if (activeParagraph != null)
            {
                if (currentTime >= startTimeInSeconds && !hasLoggedStart) 
                {
                    Debug.Log("start");
                    LogElements(); 
                    SpawnElements();
                    hasLoggedStart = true; // **SET THE FLAG AFTER EXECUTING**
                }
                
                if (currentTime >= endTimeInSeconds && !hasLoggedEnd)
                {
                    Debug.Log("end");
                    DestroyElements();
                    hasLoggedEnd = true;
                }
            }
        }
    }

    private void LoadElementData()
    {
        if (elementJsonFile != null)
        {
            string jsonString = "{\"paragraphs\":" + elementJsonFile.text + "}";
            ElementListWrapper wrapper = JsonUtility.FromJson<ElementListWrapper>(jsonString);
            allParagraphs = wrapper.paragraphs;
            if (allParagraphs != null && allParagraphs.Length > 0)
            {
                activeParagraph = null; 
                Debug.Log($"Successfully loaded {allParagraphs.Length} paragraphs from JSON.");
            }
            else
            {
                Debug.LogError("JSON parsing failed or no paragraphs found.");
            }            
            Debug.Log($"Successfully loaded data. Start Time: {startTimeInSeconds:F3}s, End Time: {endTimeInSeconds:F3}s");
        }
        else
        {
            Debug.LogError("The 'Element Json File' field is empty! Please assign your 'elements_matched.json' file in the Inspector.");
        }
    }

    private void LogElements()
    {
        if (activeParagraph != null && activeParagraph.elements != null)
        {
            var sb = new StringBuilder(); 
            sb.AppendLine("--- All Interactive Elements Activated at " + startTimeInSeconds.ToString("F3") + "s ---");
            
            foreach (var element in activeParagraph.elements)
            {
                string detail = string.Format(
                    "Name: {0,-20} | Layer: {1} | Interaction: {2,-11} | Scale: {3,-12} (x{4:F2})",
                    element.image_name,
                    element.layer,
                    element.interaction,
                    element.scale.scale_category,
                    element.scale.scale_multiplier
                );
                sb.AppendLine(detail);
            }
            
            sb.AppendLine("--- End of Dynamically Loaded Element List ---");

            Debug.Log(sb.ToString());
        }
    }
    
    private double TimeSpanToSeconds(string timeString)
    {
        string reliableString = timeString.Replace(',', '.');

        if (System.TimeSpan.TryParseExact(
                reliableString, 
                "hh\\:mm\\:ss\\.fff", 
                System.Globalization.CultureInfo.InvariantCulture, 
                out System.TimeSpan result))
        {
            return result.TotalSeconds; 
        }

        Debug.LogError("Failed to parse timestamp: " + timeString);
        return 0.0;
    }

    private void SpawnElements()
    {
        if (activeParagraph == null || activeParagraph.elements == null || elementPrefabs == null || elementPrefabs.Length == 0)
        {
            Debug.LogError("Active paragraph data is missing or prefabs array is empty.");
            return;
        }

        Debug.Log($"--- Attempting to spawn {activeParagraph.elements.Length} elements at {startTimeInSeconds:F3}s ---");

        spawnedParent = new GameObject("Spawned_Elements");

        foreach (var element in activeParagraph.elements)
        {
            string targetPrefabName = element.image_name.Replace(".png", "");
            GameObject prefab = elementPrefabs.FirstOrDefault(p => p.name == targetPrefabName);

            if (prefab != null)
            {
                float zPosition = 0f;
                if (element.layer == 1) zPosition = 3f;
                else if (element.layer == 2) zPosition = 6f;
                else if (element.layer == 3) zPosition = 0f; 
                
                // 2. Calculate initial position and scale
                Vector3 initialPosition = new Vector3(0f, 0f, zPosition);
                float checkRadius = SpiralIncrement * element.scale.scale_multiplier * CheckRadiusMultiplier;

                // 3. Find a safe position (using the simplified method)
                Vector3 finalSpawnPosition = FindSafePosition(initialPosition, checkRadius);
                
                // 4. Instantiate at the safe position
                GameObject newObject = Instantiate(prefab, finalSpawnPosition, Quaternion.identity, spawnedParent.transform);
                
                // Apply scale
                //Vector3 scale = Vector3.one * element.scale.scale_multiplier;
                //newObject.transform.localScale = scale;

                newObject.name = element.image_name;
                EnableInteractionScript(newObject, element.interaction);

                Debug.Log($"[SPAWN SUCCESS] {element.image_name} (Z: {zPosition:F0}, Scale: {element.scale.scale_multiplier:F2}) @ {finalSpawnPosition}");
            }
            else
            {
                Debug.LogWarning($"[SPAWN FAILED] Could not find prefab named '{targetPrefabName}' in the assigned list.");
            }
        }
        Debug.Log("--- Finished attempting element spawning ---");
    }

    private Vector3 FindSafePosition(Vector3 initialPos, float checkRadius)
    {
        if (!Physics.CheckSphere(initialPos, checkRadius))
        {
            return initialPos;
        }

        Vector3 currentPos = initialPos;
        float angle = 0f;
        float radius = SpiralIncrement;
        int maxAttempts = 100;

        for (int i = 0; i < maxAttempts; i++)
        {
            float x = Mathf.Cos(angle) * radius;
            float y = Mathf.Sin(angle) * radius;
            currentPos = initialPos + new Vector3(x, y, 0f);

            if (!Physics.CheckSphere(currentPos, checkRadius))
            {
                return currentPos; // Found a safe position
            }
            angle += 30f * Mathf.Deg2Rad; 
            if (angle > 360f * Mathf.Deg2Rad) {
                radius += SpiralIncrement; 
                angle = 0f;
            }
            
            if (radius > 50f) 
            {
                break;
            }
        }
        return initialPos; 
    }

    private void EnableInteractionScript(GameObject targetObject, string interactionName)
    {
        if (string.IsNullOrEmpty(interactionName)) return;

        // Find child GameObject with the interaction name
        Transform childTransform = targetObject.transform.Find(interactionName);
        
        if (childTransform != null)
        {
            childTransform.gameObject.SetActive(true);
            Debug.Log($"   -> Activated child GameObject: {interactionName}");

            // If this is the "part" interaction, wire up the Parting script's hand references
            if (interactionName == "part")
            {
                parting partingComponent = childTransform.GetComponent<parting>();
                if (partingComponent != null)
                {
                    partingComponent.leftHand = leftHand;
                    partingComponent.rightHand = rightHand;
                    Debug.Log("   -> Assigned LeftHand and RightHand to Parting component.");
                }
                else
                {
                    Debug.LogWarning("   -> 'part' child is active but no Parting component was found on it.");
                }
            }
            else if (interactionName == "shake")
            {
                // Navigate to shake > Audio > MovingResponse
                Transform audioTransform = childTransform.Find("Audio");
                if (audioTransform == null)
                {
                    Debug.LogWarning("   -> 'shake' child does not contain an 'Audio' child.");
                }
                else
                {
                    // 1) MovingResponse: controls environment movement based on hand velocity
                    Transform movingResponseTransform = audioTransform.Find("MovingResponse");
                    if (movingResponseTransform != null)
                    {
                        handVelocityMoving movement =
                            movingResponseTransform.GetComponent<handVelocityMoving>();
                        if (movement != null)
                        {
                            // Ensure we have anchors resolved from the scene
                            ResolveHandAnchorReferences();

                            movement.leftHandAnchor = leftHandAnchor;
                            movement.rightHandAnchor = rightHandAnchor;

                            Debug.Log("   -> Assigned anchors to handVelocityMoving on 'MovingResponse'.");
                        }
                        else
                        {
                            Debug.LogWarning("   -> No handVelocityMoving component found on 'MovingResponse'.");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("   -> 'Audio' child does not contain a 'MovingResponse' child.");
                    }

                    // 2) ShakeSound: shaker audio driven by hand velocity
                    Transform shakeSoundTransform = FindChildByTrimmedName(audioTransform, "ShakeSound");
                    // 3) ShakeDrumSound: drum shaker audio driven by hand velocity
                    Transform shakeDrumSoundTransform = FindChildByTrimmedName(audioTransform, "ShakeDrumSound");

                    // Wire up anchors if the components exist
                    if (shakeSoundTransform != null)
                    {
                        shakeSound shaker = shakeSoundTransform.GetComponent<shakeSound>();
                        if (shaker != null)
                        {
                            ResolveHandAnchorReferences();

                            shaker.leftHand = leftHandAnchor;
                            shaker.rightHand = rightHandAnchor;

                            Debug.Log("   -> Assigned anchors to shakeSound on 'ShakeSound'.");
                        }
                        else
                        {
                            Debug.LogWarning("   -> No shakeSound component found on 'ShakeSound'.");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("   -> 'Audio' child does not contain a 'ShakeSound' child (by trimmed name).");
                    }

                    if (shakeDrumSoundTransform != null)
                    {
                        shakeSound drumShaker = shakeDrumSoundTransform.GetComponent<shakeSound>();
                        if (drumShaker != null)
                        {
                            ResolveHandAnchorReferences();

                            drumShaker.leftHand = leftHandAnchor;
                            drumShaker.rightHand = rightHandAnchor;

                            Debug.Log("   -> Assigned anchors to shakeSound on 'ShakeDrumSound'.");
                        }
                        else
                        {
                            Debug.LogWarning("   -> No shakeSound component found on 'ShakeDrumSound'.");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("   -> 'Audio' child does not contain a 'ShakeDrumSound' child (by trimmed name).");
                    }

                    // Randomly enable either ShakeSound or ShakeDrumSound (but not both)
                    if (shakeSoundTransform != null || shakeDrumSoundTransform != null)
                    {
                        bool useDrum = UnityEngine.Random.value > 0.5f;

                        if (useDrum && shakeDrumSoundTransform != null)
                        {
                            shakeDrumSoundTransform.gameObject.SetActive(true);
                            if (shakeSoundTransform != null) shakeSoundTransform.gameObject.SetActive(false);
                            Debug.Log("   -> Random choice: enabled 'ShakeDrumSound', disabled 'ShakeSound'.");
                        }
                        else if (shakeSoundTransform != null)
                        {
                            shakeSoundTransform.gameObject.SetActive(true);
                            if (shakeDrumSoundTransform != null) shakeDrumSoundTransform.gameObject.SetActive(false);
                            Debug.Log("   -> Random choice: enabled 'ShakeSound', disabled 'ShakeDrumSound'.");
                        }
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning($"   -> Could not find child GameObject '{interactionName}' in '{targetObject.name}'.");
        }
    }

    /// <summary>
    /// Automatically resolves hand references at runtime so this works even
    /// when ElementActivator is on a prefab that gets spawned into the scene.
    /// It looks for scene objects named "OVRLeftHandDataSource" and
    /// "OVRRightHandDataSource" and grabs a Hand component from them (or their children).
    /// </summary>
    private void ResolveHandReferences()
    {
        if (leftHand == null)
        {
            GameObject leftGO = GameObject.Find("OVRLeftHandDataSource");
            if (leftGO != null)
            {
                leftHand = leftGO.GetComponentInChildren<Hand>();
                if (leftHand != null)
                {
                    Debug.Log("Resolved Left Hand from OVRLeftHandDataSource.");
                }
                else
                {
                    Debug.LogWarning("Found OVRLeftHandDataSource but no Hand component on it or its children.");
                }
            }
            else
            {
                Debug.LogWarning("Could not find GameObject named 'OVRLeftHandDataSource' in the scene.");
            }
        }

        if (rightHand == null)
        {
            GameObject rightGO = GameObject.Find("OVRRightHandDataSource");
            if (rightGO != null)
            {
                rightHand = rightGO.GetComponentInChildren<Hand>();
                if (rightHand != null)
                {
                    Debug.Log("Resolved Right Hand from OVRRightHandDataSource.");
                }
                else
                {
                    Debug.LogWarning("Found OVRRightHandDataSource but no Hand component on it or its children.");
                }
            }
            else
            {
                Debug.LogWarning("Could not find GameObject named 'OVRRightHandDataSource' in the scene.");
            }
        }
    }
    
    private void ResolveHandAnchorReferences()
    {
        if (leftHandAnchor == null)
        {
            GameObject leftAnchorGO = GameObject.Find("LeftHandAnchor");
            if (leftAnchorGO != null)
            {
                leftHandAnchor = leftAnchorGO.transform;
                Debug.Log("Resolved LeftHandAnchor from scene.");
            }
            else
            {
                Debug.LogWarning("Could not find GameObject named 'LeftHandAnchor' in the scene.");
            }
        }

        if (rightHandAnchor == null)
        {
            GameObject rightAnchorGO = GameObject.Find("RightHandAnchor");
            if (rightAnchorGO != null)
            {
                rightHandAnchor = rightAnchorGO.transform;
                Debug.Log("Resolved RightHandAnchor from scene.");
            }
            else
            {
                Debug.LogWarning("Could not find GameObject named 'RightHandAnchor' in the scene.");
            }
        }
    }
    
    /// <summary>
    /// Helper to find a direct child by name, ignoring leading/trailing spaces.
    /// This is useful when scene / prefab objects accidentally have a space suffix.
    /// </summary>
    private Transform FindChildByTrimmedName(Transform parent, string targetName)
    {
        if (parent == null) return null;

        foreach (Transform child in parent)
        {
            if (child.name.Trim() == targetName)
            {
                return child;
            }
        }

        return null;
    }
    private void DestroyElements()
    {
        if (spawnedParent != null)
        {
            Destroy(spawnedParent);
            spawnedParent = null; // Clear the reference for good measure
            Debug.Log("--- All Spawned Elements Destroyed! ---");
        }
    }

    private void ManageActiveParagraph(double currentTime)
    {
        if (allParagraphs == null) return;
        
        // Find the paragraph whose time window contains the current video time
        ElementList nextActive = allParagraphs.FirstOrDefault(p => 
            currentTime >= TimeSpanToSeconds(p.start_tms) && 
            currentTime < TimeSpanToSeconds(p.end_tms));

        if (nextActive != null && nextActive != activeParagraph)
        {
            Debug.Log($"--- New Paragraph Activated: {nextActive.paragraph} ---");

            DestroyElements(); 
            
            activeParagraph = nextActive;
            startTimeInSeconds = TimeSpanToSeconds(activeParagraph.start_tms);
            endTimeInSeconds = TimeSpanToSeconds(activeParagraph.end_tms);

            // Reset flags for the new paragraph
            hasLoggedStart = false;
            hasLoggedEnd = false;
        }
    }
}