using UnityEngine;


public class PlayerTestFoodSetup : MonoBehaviour
{
    [SerializeField] private float searchRadius = 12f;
    [SerializeField] private bool spawnFallbackFoodIfNoneFound = true;
    [SerializeField] private FoodSO fallbackFoodData;

    private void Start()
    {
        // NOTE: Food 테스트 로직은 일단 비활성화(주석 처리)합니다.
        // var player = FindFirstObjectByType<PlayerController>();
        // if (player == null || player.HasFood())
        //     return;
        //
        // var foods = FindObjectsByType<global::Food>(FindObjectsSortMode.None);
        // if (foods == null || foods.Length == 0)
        // {
        //     if (!spawnFallbackFoodIfNoneFound)
        //         return;
        //
        //     var spawned = SpawnFallbackFoodNear(player.transform.position);
        //     if (spawned != null)
        //         player.AddFood(spawned);
        //     return;
        // }
        //
        // float r2 = searchRadius * searchRadius;
        // var origin = player.transform.position;
        // global::Food best = null;
        // float bestSqr = float.MaxValue;
        //
        // foreach (var f in foods)
        // {
        //     if (f == null)
        //         continue;
        //     float sqr = (f.transform.position - origin).sqrMagnitude;
        //     if (sqr > r2 || sqr >= bestSqr)
        //         continue;
        //     bestSqr = sqr;
        //     best = f;
        // }
        //
        // if (best != null)
        //     player.AddFood(best);
    }

    private global::Food SpawnFallbackFoodNear(Vector3 origin)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "FallbackTestFood";
        go.transform.position = origin + Vector3.right * 1.2f + Vector3.up * 0.5f;
        go.transform.localScale = Vector3.one * 0.25f;

        var food = go.AddComponent<global::Food>();
        if (fallbackFoodData != null)
            food.SetData(fallbackFoodData);

        return food;
    }
}
