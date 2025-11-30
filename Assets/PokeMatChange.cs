using UnityEngine;

public class PokeMatChange : MonoBehaviour
{
    [Header("Assign Materials")]
    public Material originalMat;
    public Material pokeMat;

    [Header("Inspector-driven properties for Poke Material")]
    public Texture pokeBaseMap;
    public Color pokeColor = Color.white;

    private Renderer rend;
    private bool isPoked = false;

    // Shader property names
    private readonly string texProperty = "_Main Tex";
    private readonly string colorProperty = "_Tint Color";

    private Texture originalTex;
    private Color originalTint;

    void Start()
    {
        rend = GetComponent<Renderer>();

        // Save original material properties
        originalTex = originalMat.GetTexture(texProperty);
        originalTint = originalMat.GetColor(colorProperty);

        // Start with original material
        rend.material = originalMat;
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Finger"))
        {
            isPoked = !isPoked;

            if (isPoked)
            {
                rend.material = pokeMat;

                // Apply inspector-assigned properties
                rend.material.SetTexture(texProperty, pokeBaseMap);
                rend.material.SetColor(colorProperty, pokeColor);
            }
            else
            {
                rend.material = originalMat;

                // Restore original material properties
                rend.material.SetTexture(texProperty, originalTex);
                rend.material.SetColor(colorProperty, originalTint);
            }
        }
    }
}


