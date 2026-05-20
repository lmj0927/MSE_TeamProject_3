// Owned by MinJun Lee
using Fusion;
using UnityEngine;

public class BaseCounter : ACounter
{
    [Header("Scatter Settings")]
    [SerializeField] private float scatterRadius = 0.2f;

    public override void Interact(PlayerController player)
    {
        Object.RequestStateAuthority();
        if(!Object.HasStateAuthority)
        {
            Debug.LogError("[BaseCounter Interact] The client has no state authority");
        }

        if (player.HasFood())
        {
            AddFood(player.RemoveFood());
            return;
        }
        else if (CanRemoveFood(player))
        {
            player.AddFood(RemoveFood());
            return;
        }
    }

    protected override void AddFood(NetworkObject food)
    {
        foods.Add(food);

        float randomX = Random.Range(-scatterRadius, scatterRadius);
        float randomZ = Random.Range(-scatterRadius, scatterRadius);
        Vector3 randomOffset = new Vector3(randomX, 0f, randomZ);

        // food.transform.position = foodPoint.position + (Vector3.up * foods.Count * 0.1f) + randomOffset;
        foodPositions.Add(foodPoint.position + (Vector3.up * foods.Count * 0.1f) + randomOffset);
    }
}