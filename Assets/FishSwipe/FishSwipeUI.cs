using UnityEngine;
using UnityEngine.EventSystems;

public class FishSwipeUI : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Reference to the gallery script")]
    public FishGalleryUI fishGallery;

    [Header("Swipe settings (in screen pixels)")]
    public float minSwipeDistancePixels = 80f;

    [Header("Debug")]
    public bool debugLogs = true;

    private Vector2 _startPos;

    public void OnPointerDown(PointerEventData eventData)
    {
        _startPos = eventData.position;

        if (debugLogs)
            Debug.Log("FishSwipeUI: Swipe start at " + _startPos);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Vector2 endPos = eventData.position;
        Vector2 delta = endPos - _startPos;

        if (debugLogs)
            Debug.Log($"FishSwipeUI: Swipe end at {endPos}, delta {delta}");

        // Only care about horizontal movement
        if (Mathf.Abs(delta.x) < minSwipeDistancePixels ||
            Mathf.Abs(delta.x) < Mathf.Abs(delta.y))
        {
            if (debugLogs)
                Debug.Log("FishSwipeUI: Not a valid horizontal swipe");
            return;
        }

        if (fishGallery == null)
        {
            Debug.LogWarning("FishSwipeUI: No FishGalleryUI assigned.");
            return;
        }

        if (delta.x > 0)
        {
            fishGallery.PreviousFish();
            if (debugLogs) Debug.Log("FishSwipeUI: PreviousFish()");
        }
        else
        {
            fishGallery.NextFish();
            if (debugLogs) Debug.Log("FishSwipeUI: NextFish()");
        }
    }
}
