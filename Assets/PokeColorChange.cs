using UnityEngine;

public class PokeShaderToggle : MonoBehaviour
{
    public Color pokeColor = Color.red; // 포크 색
    private Material matInstance;
    private Color originalColor;
    private bool isPoked = false;

    private readonly string colorProperty = "_Tint_Color"; // Shader Graph에서 만든 Color 프로퍼티 이름

    void Start()
    {
        Renderer renderer = GetComponent<Renderer>();
        matInstance = renderer.material; 
        originalColor = matInstance.GetColor(colorProperty);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Finger"))
        {
            isPoked = !isPoked;
            matInstance.SetColor(colorProperty, isPoked ? pokeColor : originalColor);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Finger"))
        {
            Debug.Log("=== Change back!");
        }
    }
}
