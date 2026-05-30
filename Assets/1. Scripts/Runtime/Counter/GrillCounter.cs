// Owned by MinJun Lee

using Fusion;
using UnityEngine;

public class GrillCounter : AFireCounter
{
    [SerializeField] private ParticleSystem smoke;
    public override void Interact(PlayerController player)
    {
        if(!player.HasStateAuthority) return;
        AuthorityHandler.RequestStateAuthority(
            onAuthorized: () =>
            {
                if (CanAddFood(player))
                {
                    // AddFood(player.RemoveFood());
                    FoodTransfer.Transfer(player, this, player.HeldFoodObject, Vector3.zero);

                    // SoundManager.Instance.GrillStart(this);

                    var recipe = RecipeManager.Instance.Cook(GetFoodSOs(), RecipeType.Grill);
                    if (recipe != null)
                    {
                        cookTime = recipe.Value;
                        resultFood = recipe.Result;
                    }

                    SetState(CookState);
                    // smoke.Play();
                    RPC_PlayEffects();
                }
                else if (isDone && CanRemoveFood(player))
                {
                    // player.AddFood(RemoveFood());
                    FoodTransfer.Transfer(this, player, GetLastFood(), Vector3.zero);
                    SetState(NoneState);
                    // smoke.Stop();
                    RPC_StopEffects();

                    isDone = false;
                    resultFood = null;
                }
            },
            onNotAuthorized: () =>
            {
                Debug.LogWarning("[GrillCounter Interact] Denied");
            }
        );
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayEffects()
    {
        smoke.Play();
        SoundManager.Instance.GrillStart(this);
    }


    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_StopEffects()
    {
        smoke.Stop();
        OnCookFinished?.Invoke(); // fire on every client so each local SoundManager stops its grill audio
    }
}
