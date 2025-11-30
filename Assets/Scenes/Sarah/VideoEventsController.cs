/*using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.IO;
using UnityEngine;
using UnityEngine.Video;
using TMPro;
using UnityEngine.UI;


public class VideoEventsController : MonoBehaviour
{
    [Header("Video & UI References")]
    public VideoPlayer videoPlayer;
    public TextMeshPro timestampText;

    [Header("Prefabs & Data")]
    public GameObject spherePrefab;
    public TextAsset jsonFile;

    [Header("Image Loading (StreamingAssets)")]
    [Tooltip("Folder under StreamingAssets where event images are stored, e.g. 'EventImages' for Assets/StreamingAssets/EventImages")]
    public string imageStreamingFolder = "EventImages";
    [Header("Image Prefab")]
    [Tooltip("Prefab that will display the loaded PNG. Must have SpriteRenderer or UI.Image on it or its children.")]
    public GameObject imagePrefab;

    // Map: video timestamp -> elements at that time
    private Dictionary<TimeSpan, List<ElementMatch>> timeEventMap =
        new Dictionary<TimeSpan, List<ElementMatch>>();

    // Keep track of which timestamps already fired
    private HashSet<TimeSpan> triggeredTimes = new HashSet<TimeSpan>();

    // ---------------------------------------------------------------------
    // Unity Lifecycle
    // ---------------------------------------------------------------------

    void Awake()
    {
        if (jsonFile == null)
        {
            Debug.LogError("JSON file not assigned in the inspector!");
            return;
        }

        LoadAndMapEvents(jsonFile.text);
    }

    void Update()
    {
        if (videoPlayer == null || !videoPlayer.isPlaying)
            return;

        double currentTimeSec = videoPlayer.time;

        if (timestampText != null)
        {
            timestampText.text = $"{currentTimeSec:F2} s";
        }

        CheckForEventTrigger(currentTimeSec);
    }

    // ---------------------------------------------------------------------
    // JSON Loading & Mapping (JsonUtility)
    // ---------------------------------------------------------------------

    private void LoadAndMapEvents(string jsonString)
    {
        try
        {
            // JSON must be of shape: { "elements": [ ... ] }
            ElementMatchList data = JsonUtility.FromJson<ElementMatchList>(jsonString);

            if (data == null || data.elements == null || data.elements.Count == 0)
            {
                Debug.LogError("Failed to parse JSON data or file is empty.");
                return;
            }

            foreach (var element in data.elements)
            {
                if (element.time_stamps == null)
                    continue;

                foreach (var timestampString in element.time_stamps)
                {
                    TimeSpan eventTime = ParseTimestampToTimeSpan(timestampString);

                    if (!timeEventMap.ContainsKey(eventTime))
                    {
                        timeEventMap[eventTime] = new List<ElementMatch>();
                    }

                    timeEventMap[eventTime].Add(element);
                }
            }

            Debug.Log($"Successfully mapped {timeEventMap.Keys.Count} unique event times from JSON.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error while loading JSON: {ex.Message}");
        }
    }

    // ---------------------------------------------------------------------
    // Video Event Triggering
    // ---------------------------------------------------------------------

    private void CheckForEventTrigger(double currentTimeSec)
    {
        // Round to ms so lookup is clean
        TimeSpan videoTime = TimeSpan.FromSeconds(currentTimeSec).RoundToMillisecond();

        if (timeEventMap.TryGetValue(videoTime, out var matchingElements) &&
            !triggeredTimes.Contains(videoTime))
        {
            triggeredTimes.Add(videoTime);

            Debug.Log($"EVENT TRIGGERED at {videoTime:mm\\:ss\\.fff}: Found {matchingElements.Count} matching element(s).");

            SpawnSphere(videoTime);
            ProcessEventImages(matchingElements);
        }
    }

    private void SpawnSphere(TimeSpan eventTime)
    {
        if (spherePrefab == null)
        {
            Debug.LogWarning("Sphere prefab is not assigned.");
            return;
        }

        Instantiate(spherePrefab, new Vector3(0, 1, 3), Quaternion.identity);
        Debug.Log($"Sphere spawned for event at: {eventTime:mm\\:ss\\.fff}");
    }

    // ---------------------------------------------------------------------
    // Image Processing (StreamingAssets → Texture2D → Sprite)
    // ---------------------------------------------------------------------

   
    private void ProcessEventImages(List<ElementMatch> elements)
    {
        if (elements == null || elements.Count == 0)
            return;

        if (imagePrefab == null)
        {
            Debug.LogWarning("imagePrefab is not assigned on VideoEventsController.");
            return;
        }

        var uniqueImages = elements
            .Where(e => e.images != null)
            .SelectMany(e => e.images)
            .Where(img => !string.IsNullOrEmpty(img.image_name))
            .GroupBy(img => img.image_name)
            .Select(g => g.First())
            .ToList();

        int index = 0;

        foreach (var img in uniqueImages)
        {
            // ✅ load as Texture2D, not Sprite
            Texture2D tex = LoadTextureFromStreamingAssets(img.image_name);

            var ownerElement = elements.FirstOrDefault(e =>
                e.images != null && e.images.Contains(img));
            string label = ownerElement != null ? ownerElement.label : "(unknown)";

            if (tex != null)
            {
                // ✅ pass Texture2D into SpawnImagePrefab
                SpawnImagePrefab(tex, label, index);
                index++;
            }
            else
            {
                Debug.LogWarning($"Failed to load image '{img.image_name}'.");
            }
        }
    }


    private Texture2D LoadTextureFromStreamingAssets(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return null;

        // Build full path: <project>/Assets/StreamingAssets/<folder>/<fileName>
        string folder = imageStreamingFolder ?? string.Empty;

        string fullPath = string.IsNullOrEmpty(folder)
            ? Path.Combine(Application.streamingAssetsPath, fileName)
            : Path.Combine(Application.streamingAssetsPath, folder, fileName);

        if (!File.Exists(fullPath))
        {
            Debug.LogWarning($"Image file not found at path: {fullPath}");
            return null;
        }

        try
        {
            byte[] bytes = File.ReadAllBytes(fullPath);

            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!tex.LoadImage(bytes))
            {
                Debug.LogWarning($"LoadImage failed for file: {fullPath}");
                return null;
            }

            return tex;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error loading image from StreamingAssets: {ex.Message}");
            return null;
        }
    }
    
        private void SpawnImagePrefab(Texture2D texture, string label, int index)
    {
        if (imagePrefab == null || texture == null)
            return;

        // Instantiate your prefab
        GameObject go = Instantiate(imagePrefab);

        Transform cam = Camera.main != null ? Camera.main.transform : null;

        if (cam != null)
        {
            // Base position in front of camera
            Vector3 basePos = cam.position + cam.forward * 2f + cam.up * 0.3f;

            // Random scatter radius + angle
            float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            float radius = UnityEngine.Random.Range(0.5f, 2f);

            Vector3 right = cam.right;
            Vector3 forwardFlat = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;

            Vector3 randomOffset =
                (Mathf.Cos(angle) * right + Mathf.Sin(angle) * forwardFlat) * radius;

            float yOffset = UnityEngine.Random.Range(-0.3f, 0.3f);
            randomOffset.y += yOffset;

            go.transform.position = basePos + randomOffset;

            // Face the camera
            go.transform.rotation = Quaternion.LookRotation(go.transform.position - cam.position);
        }

        go.name = $"ImagePrefab_{label}_{index}";

        // ---- APPLY TEXTURE TO PREFAB MATERIAL ----

        // 1. Try MeshRenderer (planes, quads, 3D objects)
        Renderer rend = go.GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            // Duplicate material instance so each prefab has its own material
            Material mat = new Material(rend.material);
            mat.mainTexture = texture;
            rend.material = mat;

            return;
        }

        // 2. Try RawImage (UI element)
        UnityEngine.UI.RawImage raw = go.GetComponentInChildren<UnityEngine.UI.RawImage>();
        if (raw != null)
        {
            raw.texture = texture;
            return;
        }

        Debug.LogWarning(
            $"Prefab '{imagePrefab.name}' has no Renderer or RawImage. " +
            $"Cannot apply texture."
        );
    }

    // Parses timestamps like "[00:00:03,123]" into TimeSpan
    private static TimeSpan ParseTimestampToTimeSpan(string timestamp)
    {
        var m = Regex.Match(timestamp, @"\[(\d{2}):(\d{2}):(\d{2}),(\d{3})\]");
        if (m.Success)
        {
            int h = int.Parse(m.Groups[1].Value);
            int min = int.Parse(m.Groups[2].Value);
            int s = int.Parse(m.Groups[3].Value);
            int ms = int.Parse(m.Groups[4].Value);

            // TimeSpan(days, hours, minutes, seconds, milliseconds)
            return new TimeSpan(0, h, min, s, ms);
        }

        throw new FormatException($"Invalid timestamp format: {timestamp}");
    }
}

// Extension method for rounding TimeSpan to nearest millisecond
public static class TimeSpanExtensions
{
    public static TimeSpan RoundToMillisecond(this TimeSpan ts)
    {
        long ticks = (long)Math.Round(ts.TotalMilliseconds) * TimeSpan.TicksPerMillisecond;
        return new TimeSpan(ticks);
    }
}
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Video;
using TMPro;

public class VideoEventsController : MonoBehaviour
{
    [Header("Video & UI")]
    public VideoPlayer videoPlayer;
    public TextMeshPro timestampText;   // optional

    [Header("Sphere Prefab")]
    public GameObject spherePrefab;

    [Header("JSON Data")]
    public TextAsset jsonFile;              // assign your elements_matched.json here

    // All timestamps we care about
    private HashSet<TimeSpan> eventTimes = new HashSet<TimeSpan>();

    // Timestamps that have already been triggered
    private HashSet<TimeSpan> triggeredTimes = new HashSet<TimeSpan>();

    // ------------------------------------------------------------------
    // Unity lifecycle
    // ------------------------------------------------------------------

    void Awake()
    {
        if (jsonFile == null)
        {
            Debug.LogError("JSON file is not assigned on VideoEventsController.");
            return;
        }

        LoadEventTimes(jsonFile.text);
    }

    void Update()
    {
        if (videoPlayer == null || !videoPlayer.isPlaying)
            return;

        double currentTimeSec = videoPlayer.time;

        if (timestampText != null)
        {
            timestampText.text = $"{currentTimeSec:F2} s";
        }

        CheckForEventTrigger(currentTimeSec);
    }

    // ------------------------------------------------------------------
    // JSON loading: fill eventTimes from time_stamps in JSON
    // ------------------------------------------------------------------

    private void LoadEventTimes(string jsonString)
    {
        try
        {
            // Your JSON is a TOP-LEVEL ARRAY: [ {...}, {...}, ... ]
            // JsonUtility can't parse arrays at root, so we wrap it:
            string wrappedJson = "{\"elements\":" + jsonString + "}";

            ElementMatchList data = JsonUtility.FromJson<ElementMatchList>(wrappedJson);

            if (data == null || data.elements == null || data.elements.Count == 0)
            {
                Debug.LogError("Failed to parse JSON or elements list is empty.");
                return;
            }

            foreach (var element in data.elements)
            {
                if (element.time_stamps == null)
                    continue;

                foreach (var tsString in element.time_stamps)
                {
                    TimeSpan ts = ParseTimestampToTimeSpan(tsString);
                    eventTimes.Add(ts);
                }
            }

            Debug.Log($"Loaded {eventTimes.Count} unique event times from JSON (timestamps only).");
        }
        catch (Exception ex)
        {
            Debug.LogError("Error parsing JSON: " + ex.Message);
        }
    }

    // ------------------------------------------------------------------
    // Time checking & sphere spawning
    // ------------------------------------------------------------------

    private void CheckForEventTrigger(double currentTimeSec)
    {
        // Convert seconds → TimeSpan and round to millis so we can match dictionary keys
        TimeSpan videoTime = TimeSpan
            .FromSeconds(currentTimeSec)
            .RoundToMillisecond();

        if (eventTimes.Contains(videoTime) && !triggeredTimes.Contains(videoTime))
        {
            triggeredTimes.Add(videoTime);

            Debug.Log($"EVENT at {videoTime:mm\\:ss\\.fff} → spawn sphere");

            SpawnSphere(videoTime);
        }
    }

    private void SpawnSphere(TimeSpan eventTime)
    {
        if (spherePrefab == null)
        {
            Debug.LogWarning("Sphere prefab is not assigned.");
            return;
        }

        // For now: fixed position in front of world origin
        Vector3 spawnPos = new Vector3(0, 1, 3);
        Instantiate(spherePrefab, spawnPos, Quaternion.identity);

        Debug.Log($"Sphere spawned for event at {eventTime:mm\\:ss\\.fff}");
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    // Parse "[HH:MM:SS,mmm]" → TimeSpan
    private static TimeSpan ParseTimestampToTimeSpan(string timestamp)
    {
        // Example: "[00:00:03,000]"
        var m = Regex.Match(timestamp, @"\[(\d{2}):(\d{2}):(\d{2}),(\d{3})\]");
        if (!m.Success)
            throw new FormatException("Invalid timestamp format: " + timestamp);

        int h = int.Parse(m.Groups[1].Value);
        int min = int.Parse(m.Groups[2].Value);
        int s = int.Parse(m.Groups[3].Value);
        int ms = int.Parse(m.Groups[4].Value);

        // TimeSpan(days, hours, minutes, seconds, milliseconds)
        return new TimeSpan(0, h, min, s, ms);
    }
}

// Extension to round TimeSpan to the nearest millisecond
public static class TimeSpanExtensions
{
    public static TimeSpan RoundToMillisecond(this TimeSpan ts)
    {
        long ticks = (long)Math.Round(ts.TotalMilliseconds) * TimeSpan.TicksPerMillisecond;
        return new TimeSpan(ticks);
    }
}
