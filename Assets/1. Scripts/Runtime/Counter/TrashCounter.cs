// Owned by MinJun Lee
using Fusion;
using UnityEngine;

public class TrashCounter : ACounter
{
    public override void Interact(PlayerController player)
    {
        if(!player.HasStateAuthority) return;
        if (!player.HasFood()) return;

        player.Discard(player.HeldFoodObject, () => RPC_PlayTrash());
    }

    public override bool CanAdd(Food food) => false;
    public override bool CanRemove() => false;

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_PlayTrash()
    {
        SoundManager.Instance.Trash();
    }

    
}
