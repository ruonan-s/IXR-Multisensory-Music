using UnityEngine;

public class PokeMatChange : MonoBehaviour
{
    [Header("Assign both materials manually")]
    public Material originalMat;
    public Material pokeMat;

    private Renderer rend;
    private bool isPoked = false;

    // Shader property names
    private readonly string texProperty = "_MainTex";
    private readonly string colorProperty = "_TintColor";

    private Texture originalTex;
    private Texture pokeTex;

    private Color originalTint;
    private Color pokeTint;

    void Start()
    {
        rend = GetComponent<Renderer>();

        // Save current inspector-set values
        originalTex = originalMat.GetTexture(texProperty);
        pokeTex = pokeMat.GetTexture(texProperty);

        originalTint = originalMat.GetColor(colorProperty);
        pokeTint = pokeMat.GetColor(colorProperty);

        // Start with original
        rend.material = originalMat;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Finger"))
        {
            isPoked = !isPoked;

            if (isPoked)
            {
                rend.material = pokeMat;
                rend.material.SetTexture(texProperty, pokeTex);
                rend.material.SetColor(colorProperty, pokeTint);
            }
            else
            {
                rend.material = originalMat;
                rend.material.SetTexture(texProperty, originalTex);
                rend.material.SetColor(colorProperty, originalTint);
            }
        }
    }
}

