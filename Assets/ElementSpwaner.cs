using UnityEngine;

public class ElementSpawner : MonoBehaviour
{
    public InteractionPlacement placementRules;
    public string interactionType = "poke";
    public GameObject elementPrefab;

    void Start()
    {
        Spawn();
    }

    void Spawn()
    {
        Vector3 pos = placementRules.GetSpawnPosition(interactionType);

        GameObject obj = Instantiate(elementPrefab);
        obj.transform.position = pos;
    }
}
