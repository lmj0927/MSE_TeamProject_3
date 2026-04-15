using UnityEngine;
using minjun;

public class GrillCounter : Counter
{
    public override void Interact(Player player)
    {
        if (player.HasFood() && !HasFood())
        {
            AddFood(player.RemoveFood());
        }
    }
}
