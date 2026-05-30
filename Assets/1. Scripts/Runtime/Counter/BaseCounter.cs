// Owned by MinJun Lee
using System.Linq;
using Fusion;
using UnityEngine;

public class BaseCounter : ACounter
{
    [Header("Scatter Settings")]
    [SerializeField] private float scatterRadius = 0.2f;

    public override void Interact(PlayerController player)
    {
        if(!player.HasStateAuthority) return;
        AuthorityHandler.RequestStateAuthority(
            onAuthorized: () =>
            {
                
                if (player.HasFood())
                {
                    Debug.Log("Player to counter");
                    // AddFood(player.RemoveFood());
                    // return;
                    float randomX = Random.Range(-scatterRadius, scatterRadius);
                    float randomZ = Random.Range(-scatterRadius, scatterRadius);
                    Vector3 randomOffset = new Vector3(randomX, 0f, randomZ);

                    FoodTransfer.Transfer(player, this, player.HeldFoodObject, (Vector3.up * foods.Count * 0.1f) + randomOffset);
                }
                else if (CanRemoveFood(player))
                {
                    // player.AddFood(RemoveFood());
                    // return;
                    FoodTransfer.Transfer(this, player, GetLastFood(), Vector3.zero);
                }
            },
            onNotAuthorized: () =>
            {
                Debug.LogWarning("[BaseCounter Interact] Denied");
            }
        );
    }

    // protected override void AddFood(NetworkObject food)
    // {
    //     // foods.Add(food);

    //     float randomX = Random.Range(-scatterRadius, scatterRadius);
    //     float randomZ = Random.Range(-scatterRadius, scatterRadius);
    //     Vector3 randomOffset = new Vector3(randomX, 0f, randomZ);

    //     // food.transform.position = foodPoint.position + (Vector3.up * foods.Count * 0.1f) + randomOffset;
    //     // foodPositions.Add(foodPoint.position + (Vector3.up * foods.Count * 0.1f) + randomOffset);
    //     // RPC_AddFood(food, foodPoint.position + (Vector3.up * foods.Count * 0.1f) + randomOffset);
    //     base.AddFood(food, foodPoint.position + (Vector3.up * foods.Count * 0.1f) + randomOffset);
    // }
}