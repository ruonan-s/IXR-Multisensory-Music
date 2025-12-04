using System.Linq;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Video;
using System.Text;
using System;
using Oculus.Interaction.Input;

[System.Serializable]
public class ElementData
{
    public string image_name;
    public int layer;
    public int section;
    public string interaction;
    public int scale;
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

public struct SpawnedAssetInfo
{
    public Vector3 position;
    public float radius;
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

    // Zone-based placement constants
    private const float SizeThreshold = 2f;
    private const float PrimaryZoneChance = 0.25f;
    private const int MaxPlacementAttempts = 100;
    private const float CollisionPadding = 0.3f;

    // Primary zone boundaries (center area)
    private const float PrimaryMinX = -1f;
    private const float PrimaryMaxX = 1f;
    private const float PrimaryMinY = 0.5f;
    private const float PrimaryMaxY = 2f;

    // Full X range
    private const float FullMinX = -2.5f;
    private const float FullMaxX = 2.5f;
    //
    private const float FullMinXLayer1 = -4f;
    private const float FullMaxXLayer1 = 4f;

    // Track spawned assets per layer for collision detection
    private Dictionary<int, List<SpawnedAssetInfo>> spawnedAssetsByLayer;

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
                    hasLoggedStart = true;
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
                    "Name: {0,-20} | Layer: {1} | Interaction: {2,-11} | Scale: {3}",
                    element.image_name,
                    element.layer,
                    element.interaction,
                    element.scale
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

        // Initialize collision tracking dictionary
        spawnedAssetsByLayer = new Dictionary<int, List<SpawnedAssetInfo>>();

        foreach (var element in activeParagraph.elements)
        {
            string targetPrefabName = element.image_name.Replace(".png", "");
            GameObject prefab = elementPrefabs.FirstOrDefault(p => p.name == targetPrefabName);

            if (prefab != null)
            {
                // 1. Determine Z position and layer scaler based on layer
                float zPosition = 0f;
                float layerScaler = 1f;
                if (element.layer == 1)
                {
                    zPosition = 5f;
                    layerScaler = 1.5f;
                }
                else if (element.layer == 2)
                {
                    zPosition = 2f;
                    layerScaler = 0.8f;
                }
                else if (element.layer == 3)
                {
                    zPosition = 1f;
                    layerScaler = 0.5f;
                }

                // 2. Get Y boundaries based on section
                float sectionMinY, sectionMaxY;
                GetSectionBounds(element.section, element.layer, out sectionMinY, out sectionMaxY);

                // 3. Calculate final scale and collision radius
                float finalScaleMultiplier = element.scale * layerScaler;
                float assetRadius = finalScaleMultiplier * 0.5f;

                // 4. Find spawn position using zone-based placement
                Vector3 spawnPosition = FindZoneBasedPosition(
                    element.layer,
                    finalScaleMultiplier,
                    assetRadius,
                    sectionMinY,
                    sectionMaxY,
                    zPosition
                );

                // 5. Instantiate and apply scale
                Vector3 originalScale = prefab.transform.localScale;
                GameObject newObject = Instantiate(prefab, spawnPosition, Quaternion.identity, spawnedParent.transform);
                newObject.transform.localScale = originalScale * finalScaleMultiplier;
                newObject.name = element.image_name;

                // 6. Record spawned asset for collision tracking
                RecordSpawnedAsset(element.layer, spawnPosition, assetRadius);

                EnableInteractionScript(newObject, element.interaction);
                Debug.Log($"[SPAWN SUCCESS] {element.image_name} (Layer: {element.layer}, Section: {element.section}, Scale: {finalScaleMultiplier:F1}) @ {spawnPosition}");
            }
            else
            {
                Debug.LogWarning($"[SPAWN FAILED] Could not find prefab named '{targetPrefabName}' in the assigned list.");
            }
        }
        Debug.Log("--- Finished attempting element spawning ---");
    }

    private Vector3 FindZoneBasedPosition(int layer, float finalScale, float assetRadius, float sectionMinY, float sectionMaxY, float zPosition)
    {
        bool isLargeAsset = finalScale > SizeThreshold;
        bool usePrimaryZone = false;

        // Determine zone preference based on asset size
        if (isLargeAsset)
        {
            // Large assets (size > 3): 100% peripheral, never in primary
            usePrimaryZone = false;
        }
        else
        {
            // Small assets (size <= 3): 70% primary, 30% peripheral
            usePrimaryZone = UnityEngine.Random.value < PrimaryZoneChance;
        }

        Vector3 candidatePosition = Vector3.zero;
        bool foundValidPosition = false;

        for (int attempt = 0; attempt < MaxPlacementAttempts; attempt++)
        {
            if (usePrimaryZone)
            {
                candidatePosition = GetRandomPrimaryPosition(sectionMinY, sectionMaxY, zPosition);
            }
            else
            {
                candidatePosition = GetRandomPeripheralPosition(layer, sectionMinY, sectionMaxY, zPosition);
            }

            // Check collision with same-layer assets
            if (!CheckCollisionWithLayer(layer, candidatePosition, assetRadius))
            {
                foundValidPosition = true;
                break;
            }

            // After half attempts, try switching zone type
            if (attempt == MaxPlacementAttempts / 2)
            {
                usePrimaryZone = !usePrimaryZone;
            }
        }

        if (!foundValidPosition)
        {
            Debug.LogWarning($"Could not find non-overlapping position after {MaxPlacementAttempts} attempts. Using last candidate.");
        }

        return candidatePosition;
    }

    private Vector3 GetRandomPrimaryPosition(float sectionMinY, float sectionMaxY, float zPosition)
    {
        // Primary zone: center area, but constrained by section Y bounds
        float effectiveMinY = Mathf.Max(sectionMinY, PrimaryMinY);
        float effectiveMaxY = Mathf.Min(sectionMaxY, PrimaryMaxY);

        // If section doesn't overlap with primary Y range, use section bounds
        if (effectiveMinY >= effectiveMaxY)
        {
            effectiveMinY = sectionMinY;
            effectiveMaxY = sectionMaxY;
        }

        float x = UnityEngine.Random.Range(PrimaryMinX, PrimaryMaxX);
        float y = UnityEngine.Random.Range(effectiveMinY, effectiveMaxY);

        return new Vector3(x, y, zPosition);
    }

    private Vector3 GetRandomPeripheralPosition(int layer, float sectionMinY, float sectionMaxY, float zPosition)
    {
        // Peripheral zones: left edge, right edge, or edges within section
        // Randomly choose left or right peripheral zone
        int zoneChoice = UnityEngine.Random.Range(0, 2);

        // Use layer-specific X boundaries
        float minX = (layer == 1) ? FullMinXLayer1 : FullMinX;
        float maxX = (layer == 1) ? FullMaxXLayer1 : FullMaxX;

        float x, y;

        if (zoneChoice == 0)
        {
            // Left peripheral zone
            x = UnityEngine.Random.Range(minX, PrimaryMinX);
        }
        else
        {
            // Right peripheral zone
            x = UnityEngine.Random.Range(PrimaryMaxX, maxX);
        }

        // Y is constrained by section bounds
        y = UnityEngine.Random.Range(sectionMinY, sectionMaxY);

        return new Vector3(x, y, zPosition);
    }

    private bool CheckCollisionWithLayer(int layer, Vector3 position, float radius)
    {
        if (!spawnedAssetsByLayer.ContainsKey(layer))
        {
            return false; // No assets in this layer yet
        }

        foreach (var asset in spawnedAssetsByLayer[layer])
        {
            float distance = Vector3.Distance(position, asset.position);
            float minDistance = radius + asset.radius + CollisionPadding;

            if (distance < minDistance)
            {
                return true; // Collision detected
            }
        }

        return false; // No collision
    }

    private void RecordSpawnedAsset(int layer, Vector3 position, float radius)
    {
        if (!spawnedAssetsByLayer.ContainsKey(layer))
        {
            spawnedAssetsByLayer[layer] = new List<SpawnedAssetInfo>();
        }

        spawnedAssetsByLayer[layer].Add(new SpawnedAssetInfo
        {
            position = position,
            radius = radius
        });
    }

    private void GetSectionBounds(int section, int layer, out float minY, out float maxY)
    {
        if (section == 0)
        {
            if (layer == 1)
            {
                minY = 0.5f;
                maxY = 0.9f;
            }
            else if (layer == 2)
            {
                minY = 1f;
                maxY = 1.3f;
            }
            else
            {
                minY = 1.2f;
                maxY = 1.6f;
            }
        }
        else if (section == 1)
        {
            if (layer == 1)
            {
                minY = 1.3f;
                maxY = 2.5f;
            }
            else if (layer == 2)
            {
                minY = 1.2f;
                maxY = 2.2f;
            }
            else
            {
                minY = 1.5f;
                maxY = 2f;
            }
        }
        else
        {
            // Default
            minY = 1.5f;
            maxY = 2.7f;
        }
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
            spawnedParent = null;
            Debug.Log("--- All Spawned Elements Destroyed! ---");
        }

        // Clear collision tracking
        if (spawnedAssetsByLayer != null)
        {
            spawnedAssetsByLayer.Clear();
        }
    }

    private void ManageActiveParagraph(double currentTime)
    {
        if (allParagraphs == null) return;

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
            hasLoggedStart = false;
            hasLoggedEnd = false;
        }
    }
}
