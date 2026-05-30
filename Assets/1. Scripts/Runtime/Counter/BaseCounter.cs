// Owned by MinJun Lee
using UnityEngine;

public class BaseCounter : ACounter
{
    [Header("Scatter Settings")]
    [SerializeField] private float scatterRadius = 0.2f;

    public override void Interact(PlayerController player)
    {
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

    protected override void AddFood(Food food)
    {
        foods.Add(food);

        float randomX = Random.Range(-scatterRadius, scatterRadius);
        float randomZ = Random.Range(-scatterRadius, scatterRadius);
        Vector3 randomOffset = new Vector3(randomX, 0f, randomZ);

        food.transform.position = foodPoint.position + (Vector3.up * foods.Count * 0.1f) + randomOffset;
    }
}