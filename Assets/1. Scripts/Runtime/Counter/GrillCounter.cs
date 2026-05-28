// Owned by MinJun Lee

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

                    SoundManager.Instance.GrillStart(this);

                    var recipe = RecipeManager.Instance.Cook(GetFoodSOs(), RecipeType.Grill);
                    if (recipe != null)
                    {
                        cookTime = recipe.Value;
                        resultFood = recipe.Result;
                    }

                    SetState(CookState);
                    smoke.Play();
                }
                else if (isDone && CanRemoveFood(player))
                {
                    OnCookFinished?.Invoke();
                    // player.AddFood(RemoveFood());
                    FoodTransfer.Transfer(this, player, GetLastFood(), Vector3.zero);
                    SetState(NoneState);
                    smoke.Stop();

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

    
}
