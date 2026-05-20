// Owned by MinJun Lee
using UnityEngine;

public class TrashCounter : ACounter
{
    public override void Interact(PlayerController player)
    {
        if (player.HasFood())
        {
            // Destroy(player.RemoveFood().gameObject);
            foodSpawner.Despawn(player.RemoveFood());
        }
    }
}
