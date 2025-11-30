using System.Collections;
using UnityEngine;
using UnityEngine.Video;

public class SkyboxFromVideo : MonoBehaviour

{
    public VideoPlayer mainVideo;          // 메시에서 재생되는 원본
    public VideoPlayer colorSamplerVideo;  // RenderTexture 출력용
    public RenderTexture videoRT;          // 색 뽑는 용도 작은 RT

    public float colorLerpSpeed = 2f;      // 색이 부드럽게 변하는 속도

    private Material skyMat;

    void Start()
    {
        // Procedural Skybox 가져오기
        skyMat = RenderSettings.skybox;
    }

    void Update()
    {
        // 두 VideoPlayer 시간 동기화
        if (mainVideo && colorSamplerVideo)
            colorSamplerVideo.time = mainVideo.time;

        // RenderTexture에서 마지막 픽셀의 색 가져오기
        Color c = GetLastPixelColor(videoRT);

        // Sky Tint + Ground Color 업데이트
        skyMat.SetColor("_SkyTint", Color.Lerp(skyMat.GetColor("_SkyTint"), c, Time.deltaTime * colorLerpSpeed));
        skyMat.SetColor("_GroundColor", Color.Lerp(skyMat.GetColor("_GroundColor"), c, Time.deltaTime * colorLerpSpeed));

        // 스카이박스 즉시 업데이트 적용
        DynamicGI.UpdateEnvironment();
    }

    // 작은 텍스처에서 마지막 픽셀 색 읽기
    Color GetLastPixelColor(RenderTexture rt)
    {
        if (rt == null) return Color.black;

        RenderTexture.active = rt;
        Texture2D temp = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);

        temp.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        temp.Apply();

        // 마지막 픽셀 색
        Color lastPixel = temp.GetPixel(rt.width - 1, rt.height - 1);

        RenderTexture.active = null;
        Destroy(temp);

        return lastPixel;
    }
}
