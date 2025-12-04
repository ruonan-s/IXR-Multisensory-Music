using UnityEngine;

public class floatMotion : MonoBehaviour
{
    [Tooltip("amp")]
    public float amplitude = 0.08f;
    
    [Tooltip("speed")]
    public float frequency = 1f;
    
    [Tooltip("smooth)")]
    public float smoothness = 5f;
    
    private Vector3 startPosition;
    private float currentY;
    void Start()
    {
        startPosition = transform.localPosition;
        currentY = 0f;
    }
    void Update()
    {
        float targetY = Mathf.Sin(Time.time * frequency) * amplitude;
        
        currentY = Mathf.Lerp(currentY, targetY, Time.deltaTime * smoothness);
        
        transform.localPosition = startPosition + new Vector3(0f, currentY, 0f);
    }
}