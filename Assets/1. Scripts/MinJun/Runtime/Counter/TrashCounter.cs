using UnityEngine;
using minjun;

public class TrashCounter : ACounter
{
    public override void Interact(Player player)
    {
        if (CanAddFood(player))
        {
            Destroy(player.RemoveFood().gameObject);
        }
    }
}
