// Owned by MinJun Lee

using Fusion;
using UnityEngine;

public class GrillCounter : AFireCounter
{
    [SerializeField] private ParticleSystem smoke;

    protected override RecipeType CookRecipeType => RecipeType.Grill;

    public override void Interact(PlayerController player)
    {
        if(!player.HasStateAuthority) return;
        AuthorityHandler.RequestStateAuthority(
            onAuthorized: () =>
            {
                if (CanAdd(player.HeldFood))
                {
                    player.HandoffTo(this, player.HeldFoodObject, Vector3.zero, () =>
                    {
                        var recipe = RecipeManager.Instance.Cook(GetFoodSOs(), RecipeType.Grill);
                        if (recipe != null)
                        {
                            cookTime = recipe.Value;
                        }

                        SetState(CookState);
                        RPC_PlayEffects();
                    });
                }
                else if (isDone && !player.HasFood() && CanRemove())
                {
                    HandoffTo(player, GetLastFood(), Vector3.zero, () =>
                    {
                        SetState(NoneState);
                        RPC_StopEffects();

                        isDone = false;
                    });
                }
            },
            onNotAuthorized: () =>
            {
                Debug.LogWarning("[GrillCounter Interact] Denied");
            }
        );
    }

    /// <summary>
    /// Play effects to all players.
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayEffects()
    {
        smoke.Play();
        SoundManager.Instance.GrillStart(this);
    }


    /// <summary>
    /// Stop playing effects to all players.
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_StopEffects()
    {
        smoke.Stop();
        OnCookFinished?.Invoke(); // fire on every client so each local SoundManager stops its grill audio
    }
}
