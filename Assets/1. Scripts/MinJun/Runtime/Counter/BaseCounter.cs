using UnityEngine;

public class BaseCounter : ACounter
{
    public override void Interact(PlayerController player)
    {
        if (CanAddFood(player))
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
}
