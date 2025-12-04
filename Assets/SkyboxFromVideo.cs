using UnityEngine;
using UnityEngine.Video;

public class SkyboxFromVideo : MonoBehaviour
{
    public VideoPlayer mainVideo;          // 화면에 재생되는 원본 비디오
    public VideoPlayer colorSamplerVideo;  // 색 추출용 비디오
    public RenderTexture videoRT;          // 색 샘플링용 RenderTexture

    public float colorLerpSpeed = 2f;

    private Material skyMat;

    void Start()
    {
        // Skybox 머티리얼 가져오기
        skyMat = RenderSettings.skybox;

        // 두 비디오 시작 위치를 0으로 맞추고 동시에 재생
        mainVideo.time = 0;
        colorSamplerVideo.time = 0;

        mainVideo.Play();
        colorSamplerVideo.Play();
    }

    void Update()
    {
        // RenderTexture에서 픽셀 색 읽기
        Color c = GetLastPixelColor(videoRT);

        // Skybox 색 업데이트 (부드럽게)
        skyMat.SetColor("_SkyTint",
            Color.Lerp(skyMat.GetColor("_SkyTint"), c, Time.deltaTime * colorLerpSpeed));

        skyMat.SetColor("_GroundColor",
            Color.Lerp(skyMat.GetColor("_GroundColor"), c, Time.deltaTime * colorLerpSpeed));

        // GI 업데이트
        DynamicGI.UpdateEnvironment();
    }

    // RenderTexture의 마지막 픽셀 색 추출
    Color GetLastPixelColor(RenderTexture rt)
    {
        if (rt == null) return Color.yellow;

        RenderTexture.active = rt;
        Texture2D temp = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);

        temp.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        temp.Apply();

        Color lastPixel = temp.GetPixel(rt.width - 1, rt.height - 1);

        RenderTexture.active = null;
        Destroy(temp);

        return lastPixel;
    }
}