using UnityEngine;

[CreateAssetMenu(fileName = "InteractionPlacement", menuName = "Interaction/Placement")]
public class InteractionPlacement : ScriptableObject
{
    [Header("Base Positions (center point for each interaction)")]
    public Vector3 pokeCenter;
    public Vector3 grabCenter;
    public Vector3 swipeCenter;
    public Vector3 shakeCenter;
    public Vector3 waveCenter;

    [Header("Random Offset Range")]
    public float randomRadius = 0.5f; // 겹치지 않도록 퍼뜨리는 범위

    public Vector3 GetSpawnPosition(string interaction)
    {
        Vector3 basePos = interaction switch
        {
            "poke" => pokeCenter,
            "grab" => grabCenter,
            "swipe" => swipeCenter,
            "shake" => shakeCenter,
            "wave" => waveCenter,
            _ => Vector3.zero
        };

        // 중심에서 랜덤 offset 주기
        Vector2 randomCircle = Random.insideUnitCircle * randomRadius;

        return basePos + new Vector3(randomCircle.x, 0, randomCircle.y);
    }
}
