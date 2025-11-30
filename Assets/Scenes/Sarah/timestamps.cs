using TMPro;

using UnityEngine;
using UnityEngine.Video;

public class timestamps : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public TextMeshPro timestampText;

    void Update()
    {
        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            double currentTime = videoPlayer.time;
            
            Debug.Log("Video Time: " + currentTime.ToString("F3") + "s");

            if (timestampText != null)
            {
                timestampText.text = $"{currentTime:F3} s";
            }
        }
    }
}
