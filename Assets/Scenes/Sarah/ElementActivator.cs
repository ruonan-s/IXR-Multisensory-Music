using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Video;
using System.Text;
using System;

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
                else if (element.layer == 2) zPosition = 15f;
                else if (element.layer == 3) zPosition = 0f; 
                
                // 2. Calculate initial position and scale
                Vector3 initialPosition = new Vector3(0f, 0f, zPosition);
                float checkRadius = SpiralIncrement * element.scale.scale_multiplier * CheckRadiusMultiplier;

                // 3. Find a safe position (using the simplified method)
                Vector3 finalSpawnPosition = FindSafePosition(initialPosition, checkRadius);
                
                // 4. Instantiate at the safe position
                GameObject newObject = Instantiate(prefab, finalSpawnPosition, Quaternion.identity, spawnedParent.transform);
                
                // Apply scale
                Vector3 scale = Vector3.one * element.scale.scale_multiplier;
                newObject.transform.localScale = scale;

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
        }
        else
        {
            Debug.LogWarning($"   -> Could not find child GameObject '{interactionName}' in '{targetObject.name}'.");
        }
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