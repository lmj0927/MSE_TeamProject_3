// Owned by MinJun Lee
using Fusion;
using UnityEngine;

public class CookingCounter : ACounter
{
    private RecipeSO recipe;
    public override void Interact(PlayerController player)
    {
        // Cooking Counter는 음식 여러개 추가 가능
        // if (player.HasFood() && AcceptsFood(player.HeldFood.Data))
        // {
        //     AddFood(player.RemoveFood());
        //     recipe = RecipeManager.Instance.Cook(GetFoodSOs(), RecipeType.Assemble);
        // }
        // else if (!player.HasFood())
        // {
        //     if (recipe != null)
        //     {
        //         RPC_ClearFood();
        //         player.AddFood(FoodSpawner.SpawnFood(Runner, recipe.Result));
        //         recipe = null;
        //     }
        //     else
        //     {
        //         var temp = RemoveFood();
                
        //         if (temp != null) player.AddFood(temp);
        //     }
        // }
        AuthorityHandler.RequestStateAuthority(
            onAuthorized: () =>
            {
                if(player.HasFood() && AcceptsFood(player.HeldFood.Data))
                {
                    FoodTransfer.Transfer(player, this, player.HeldFoodObject, Vector3.up * foods.Count * 0.1f);
                }
                else if(CanRemoveFood(player))
                {
                    recipe = RecipeManager.Instance.Cook(GetFoodSOs(), RecipeType.Assemble);

                    if (recipe != null)
                    {
                        // RPC_ClearFood();
                        OnClear();
                        NetworkObject foodNO = FoodSpawner.SpawnFood(Runner, recipe.Result);
                        OnAdded(foodNO, foodPoint.position); // temporarily add
                        FoodTransfer.Transfer(this, player, foodNO, Vector3.zero);
                        recipe = null;
                        
                    }
                    else
                    {
                        FoodTransfer.Transfer(this, player, GetLastFood(), Vector3.zero);
                    }
                }
            },
            onNotAuthorized: () =>
            {
                Debug.LogWarning("[CookingCounter Interact] Denied");
            }
        );
    }

    // protected override void AddFood(NetworkObject food)
    // {
    //     // foods.Add(food);
    //     // food.transform.position = foodPoint.position + (Vector3.up * foods.Count * 0.1f);
    //     // foodPositions.Add(foodPoint.position + (Vector3.up * foods.Count * 0.1f));
    //     RPC_AddFood(food, foodPoint.position + (Vector3.up * foods.Count * 0.1f));
    // }


    public override bool CanAdd(Food food)
    {
        var accept = food != null && AcceptsFood(food.Data);
        var ok = accept;
        var foodDesc = food != null && food.Data != null ? food.Data.FoodName : "null";
        Debug.Log($"[Counter/{name}] CanAdd({foodDesc}) = {ok} (HasFood={HasFood()} AcceptsFood={accept})");
        return ok;
    }
}
