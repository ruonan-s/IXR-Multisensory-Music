using UnityEngine;

public class AutoSkyColor : MonoBehaviour
{
    public float speed = 0.1f; // 색 변화 속도

    private Material sky;

    void Start()
    {
        sky = RenderSettings.skybox;
    }

    void Update()
    {
        // Hue 값(0~1)을 시간에 따라 회전
        float h = Mathf.Repeat(Time.time * speed, 1f);
        Color c = Color.HSVToRGB(h, 0.6f, 1f);

        // 스카이박스 색 적용
        sky.SetColor("_SkyTint", c);
        sky.SetColor("_GroundColor", c);

        DynamicGI.UpdateEnvironment();
    }
}
