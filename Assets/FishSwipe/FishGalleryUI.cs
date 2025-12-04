using UnityEngine;
using UnityEngine.UI;

public class FishGalleryUI : MonoBehaviour
{
    [Header("Resources/Fish folder name")]
    public string resourcesFolder = "Fish";

    [Header("UI Image that shows the fish")]
    public Image targetImage;

    private Sprite[] fishSprites;
    private int currentIndex = 0;

    private void Awake()
    {
        fishSprites = Resources.LoadAll<Sprite>(resourcesFolder);

        if (fishSprites == null || fishSprites.Length == 0)
        {
            Debug.LogError("FishGalleryUI: No sprites found in Resources/" + resourcesFolder);
            return;
        }

        ShowCurrent();
    }

    private void ShowCurrent()
    {
        if (targetImage == null || fishSprites.Length == 0) return;
        targetImage.sprite = fishSprites[currentIndex];
        targetImage.SetNativeSize(); // optional
    }

    public void NextFish()
    {
        if (fishSprites.Length == 0) return;
        currentIndex = (currentIndex + 1) % fishSprites.Length;
        ShowCurrent();
    }

    public void PreviousFish()
    {
        if (fishSprites.Length == 0) return;
        currentIndex = (currentIndex - 1 + fishSprites.Length) % fishSprites.Length;
        ShowCurrent();
    }
}
