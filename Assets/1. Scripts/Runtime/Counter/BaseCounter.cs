// Owned by MinJun Lee
using UnityEngine;

public class BaseCounter : ACounter
{
    /// <summary>
    /// Position offset radius when dropping food down on this.
    /// </summary>
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
                    float randomX = Random.Range(-scatterRadius, scatterRadius);
                    float randomZ = Random.Range(-scatterRadius, scatterRadius);
                    Vector3 randomOffset = new Vector3(randomX, 0f, randomZ);

                    player.HandoffTo(this, player.HeldFoodObject, (Vector3.up * foods.Count * 0.1f) + randomOffset);
                }
                else if (CanRemove()) // outer `else` already guarantees !player.HasFood()
                {
                    HandoffTo(player, GetLastFood(), Vector3.zero);
                }
            },
            onNotAuthorized: () =>
            {
                Debug.LogWarning("[BaseCounter Interact] Denied");
            }
        );
    }
}
