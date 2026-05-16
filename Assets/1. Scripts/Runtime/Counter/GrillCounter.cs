// Owned by MinJun Lee
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.Serialization;

public class GrillCounter : AFireCounter
{
    public override void Interact(PlayerController player)
    {
        if (CanAddFood(player))
        {
            AddFood(player.RemoveFood());
            var recipe = RecipeManager.Instance.Cook(GetFoodSOs(), RecipeType.Grill);
            if (recipe != null)
            {
                cookTime = recipe.Value;
                resultFood = recipe.Result;
                SoundManager.Instance.GrillStart();
            }

            SetState(CookState);
        }
        else if (isDone && CanRemoveFood(player))
        {
            SoundManager.Instance.GrillEnd();
            player.AddFood(RemoveFood());
            SetState(NoneState);

            isDone = false;
            resultFood = null;
        }
    }

    
}
