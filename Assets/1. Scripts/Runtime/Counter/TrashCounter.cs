// Owned by MinJun Lee
using Fusion;
using UnityEngine;

public class TrashCounter : ACounter
{
    public override void Interact(PlayerController player)
    {
        if(!player.HasStateAuthority) return;
        if (player.HasFood())
        {
            FoodSpawner.Despawn(Runner, player.RemoveFood());
            RPC_PlayTrash();
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_PlayTrash()
    {
        SoundManager.Instance.Trash();
    }

    
}
