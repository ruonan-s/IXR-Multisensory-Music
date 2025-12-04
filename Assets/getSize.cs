using UnityEngine;
using UnityEngine.Video;

public class getSize : MonoBehaviour
{
    public VideoPlayer videoPlayer;

    private bool sizeRetrieved = false;

    void Start()
    {
        if (videoPlayer == null)
        {
            videoPlayer = GetComponent<VideoPlayer>();
        }

        if (videoPlayer == null)
        {
            Debug.LogError("No VideoPlayer found! Please attach a VideoPlayer component or assign one in the inspector.");
            return;
        }

        videoPlayer.prepareCompleted += OnVideoPrepared;
        videoPlayer.Prepare();
    }

    void OnVideoPrepared(VideoPlayer vp)
    {
        if (!sizeRetrieved)
        {
            GetVideoSize();
            sizeRetrieved = true;
        }
    }

    void GetVideoSize()
    {
        if (videoPlayer != null && videoPlayer.isPrepared)
        {
            long width = (long)videoPlayer.width;
            long height = (long)videoPlayer.height;

            Debug.Log($"Video Size - Width: {width}, Height: {height}");
            Debug.Log($"Video Frame Rate: {videoPlayer.frameRate}");
            Debug.Log($"Video Frame Count: {videoPlayer.frameCount}");
            Debug.Log($"Video Length: {videoPlayer.length} seconds");
        }
    }

    void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= OnVideoPrepared;
        }
    }
}
