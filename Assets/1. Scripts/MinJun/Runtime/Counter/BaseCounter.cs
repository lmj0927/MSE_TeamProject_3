using minjun;
using UnityEngine;

public class BaseCounter : ACounter
{
    public override void Interact(Player player)
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
