// Owned by MinJun Lee
using UnityEngine;

public class CookingCounter : ACounter
{
    public override void Interact(PlayerController player)
    {
        if(!player.HasStateAuthority) return;

        AuthorityHandler.RequestStateAuthority(
            onAuthorized: () =>
            {
                if(CanAdd(player.HeldFood))
                {
                    player.HandoffTo(this, player.HeldFoodObject, Vector3.up * foods.Count * 0.1f);
                }
                else if(!player.HasFood() && CanRemove())
                {
                    RecipeSO recipe = RecipeManager.Instance.Cook(GetFoodSOs(), RecipeType.Assemble);

                    if (recipe != null)
                    {
                        var assembled = recipe.Result;
                        recipe = null;
                        ClearAll(() =>
                        {
                            Place(assembled, foodPoint.position, spawned =>
                            {
                                if (spawned == null) return;
                                HandoffTo(player, spawned, Vector3.zero);
                            });
                        });
                    }
                    else
                    {
                        HandoffTo(player, GetLastFood(), Vector3.zero);
                    }
                }
            },
            onNotAuthorized: () =>
            {
                Debug.LogWarning("[CookingCounter Interact] Denied");
            }
        );
    }

    public override bool CanAdd(Food food)
    {
        var accept = food != null && AcceptsFood(food.Data);
        var ok = accept;
        var foodDesc = food != null && food.Data != null ? food.Data.FoodName : "null";
        Debug.Log($"[Counter/{name}] CanAdd({foodDesc}) = {ok} (HasFood={HasFood()} AcceptsFood={accept})");
        return ok;
    }
}
