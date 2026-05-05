using UnityEngine;

public class TrashCounter : ACounter
{
    public override void Interact(PlayerController player)
    {
        if (CanAddFood(player))
        {
            Destroy(player.RemoveFood().gameObject);
        }
    }
}
