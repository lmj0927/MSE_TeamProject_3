// Owned by MinJun Lee

public class GrillCounter : AFireCounter
{
    public override void Interact(PlayerController player)
    {
        if (CanAddFood(player))
        {
            AddFood(player.RemoveFood());

            SoundManager.Instance.GrillStart(this);

            var recipe = RecipeManager.Instance.Cook(GetFoodSOs(), RecipeType.Grill);
            if (recipe != null)
            {
                cookTime = recipe.Value;
                resultFood = recipe.Result;
            }

            SetState(CookState);
        }
        else if (isDone && CanRemoveFood(player))
        {
            OnCookFinished?.Invoke();
            player.AddFood(RemoveFood());
            SetState(NoneState);

            isDone = false;
            resultFood = null;
        }
    }

    
}
