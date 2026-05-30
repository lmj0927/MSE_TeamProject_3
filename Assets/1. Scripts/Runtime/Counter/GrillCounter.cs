// Owned by MinJun Lee

using UnityEngine;

public class GrillCounter : AFireCounter
{
    [SerializeField] private ParticleSystem smoke;
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
            smoke.Play();
        }
        else if (isDone && CanRemoveFood(player))
        {
            OnCookFinished?.Invoke();
            player.AddFood(RemoveFood());
            SetState(NoneState);
            smoke.Stop();

            isDone = false;
            resultFood = null;
        }
    }

    
}
