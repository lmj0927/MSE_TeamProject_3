using UnityEngine;

public class CookingCounter : ACounter
{
    private RecipeSO recipe;
    public override void Interact(PlayerController player)
    {
        // Cooking Counter는 음식 여러개 추가 가능
        if (player.HasFood())
        {
            AddFood(player.RemoveFood());
            recipe = RecipeManager.Instance.Cook(GetFoodSOs(), RecipeType.Assemble);
        }
        else if (!player.HasFood())
        {
            if (recipe != null)
            {
                ClearFood();
                player.AddFood(recipe.Result.CreateFood());
                recipe = null;
            }
            else
            {
                var temp = RemoveFood();
                
                if (temp != null) player.AddFood(temp);
            }
        }
    }

    protected void AddFood(Food food)
    {
        foods.Add(food);
        food.transform.position = foodPoint.position + (Vector3.up * foods.Count * 0.1f);
    }
}
