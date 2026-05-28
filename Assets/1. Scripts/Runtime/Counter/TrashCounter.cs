// Owned by MinJun Lee
using UnityEngine;

public class TrashCounter : ACounter
{
    public override void Interact(PlayerController player)
    {
        if(!player.HasStateAuthority) return;
        if (player.HasFood())
        {
            FoodSpawner.Despawn(Runner, player.RemoveFood());
            SoundManager.Instance.Trash();
        }
    }
}
