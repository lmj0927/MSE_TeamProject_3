// Owned by MinJun Lee
using Fusion;
using UnityEngine;

public class CookingCounter : ACounter
{
    private RecipeSO recipe;
    public override void Interact(PlayerController player)
    {
        // Cooking Counter는 음식 여러개 추가 가능
        if (player.HasFood() && AcceptsFood(player.HeldFood.Data))
        {
            AddFood(player.RemoveFood());
            recipe = RecipeManager.Instance.Cook(GetFoodSOs(), RecipeType.Assemble);
        }
        else if (!player.HasFood())
        {
            if (recipe != null)
            {
                ClearFood();
                player.AddFood(foodSpawner.SpawnFood(recipe.Result));
                recipe = null;
            }
            else
            {
                var temp = RemoveFood();
                
                if (temp != null) player.AddFood(temp);
            }
        }
    }

    protected override void AddFood(NetworkObject food)
    {
        foods.Add(food);
        // food.transform.position = foodPoint.position + (Vector3.up * foods.Count * 0.1f);
        foodPositions.Add(foodPoint.position + (Vector3.up * foods.Count * 0.1f));
    }
}
