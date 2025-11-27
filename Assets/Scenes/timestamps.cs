using TMPro;

using UnityEngine;
using UnityEngine.Video;

public class timestamps : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public TextMeshPro timestampText;
    public GameObject spherePrefab;   // assign a Sphere prefab or a default sphere
    public float triggerTime = 3f;     // seconds

    private bool hasSpawned = false;



    void Update()
    {
        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            double currentTime = videoPlayer.time;
            Debug.Log("Video Time: " + currentTime.ToString("F2") + "s");
        }
        if (videoPlayer.isPlaying && !hasSpawned)
        {
            if (videoPlayer.time >= triggerTime)
            {
                SpawnSphere();
                hasSpawned = true;
            }
        }
        if (videoPlayer.isPlaying)
        {
            timestampText.text = $"{videoPlayer.time:F2} s";
        }
        void SpawnSphere()
        {
            // Spawns at origin — change as needed
            Instantiate(spherePrefab, new Vector3(0, 1, 0), Quaternion.identity);
            Debug.Log("Sphere spawned at video time: " + videoPlayer.time);
        }


    }
}
