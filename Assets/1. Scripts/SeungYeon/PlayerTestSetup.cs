using UnityEngine;

/// <summary>
/// Player test scene: grants the nearest loose <see cref="Food"/> to <see cref="Player"/> on play,
/// so SliceCounter can receive food via E without wiring sy.Food.
/// </summary>
public class PlayerTestSetup : MonoBehaviour
{
    [SerializeField] private float searchRadius = 12f;

    private void Start()
    {
        var player = FindFirstObjectByType<PlayerController>();
        if (player == null || player.HasFood())
            return;

        var foods = FindObjectsByType<Food>(FindObjectsSortMode.None);
        if (foods == null || foods.Length == 0)
            return;

        float r2 = searchRadius * searchRadius;
        var origin = player.transform.position;
        Food best = null;
        float bestSqr = float.MaxValue;

        foreach (var f in foods)
        {
            if (f == null)
                continue;
            float sqr = (f.transform.position - origin).sqrMagnitude;
            if (sqr > r2 || sqr >= bestSqr)
                continue;
            bestSqr = sqr;
            best = f;
        }

        if (best != null)
            player.AddFood(best);
    }
}
