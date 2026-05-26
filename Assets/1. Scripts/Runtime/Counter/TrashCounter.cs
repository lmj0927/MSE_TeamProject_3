// Owned by MinJun Lee
using UnityEngine;

public class TrashCounter : ACounter
{
    public override void Interact(PlayerController player)
    {
        if (player.HasFood())
        {
            foodSpawner.Despawn(player.RemoveFood());
            SoundManager.Instance.Trash();
        }
    }
}
