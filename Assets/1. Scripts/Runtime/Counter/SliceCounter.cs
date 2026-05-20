// Owned by MinJun Lee
using UnityEngine;

public class SliceCounter : ACounter
{
    [SerializeField] private int sliceCount = 5;
    [SerializeField] private ProgressBar progressBar;

    private bool isSlicing = false;
    private int currentSliceCount = 0;
    private RecipeSO recipe;

    private void Awake()
    {
        if (progressBar != null)
        {
            progressBar.gameObject.SetActive(false);
            progressBar.SetProgress(0f);
        }
    }

    public override void Interact(PlayerController player)
    {
        if (CanAddFood(player))
        {
            AddFood(player.RemoveFood());
            recipe = RecipeManager.Instance.Cook(GetFoodSOs(), RecipeType.Slice);
            if (recipe != null)
            {
                sliceCount = recipe.Value;
                isSlicing = true;
                if (progressBar != null)
                    progressBar.gameObject.SetActive(true);
            }
            return;
        }
        else if (CanRemoveFood(player) && !isSlicing)
        {
            player.AddFood(RemoveFood());
            return;
        }
        else if (isSlicing)
        {
            SoundManager.Instance.Slice();
            currentSliceCount++;
            if (progressBar != null)
                progressBar.SetProgress((float)currentSliceCount / sliceCount);
            if (currentSliceCount >= sliceCount)
            {
                isSlicing = false;
                currentSliceCount = 0;
                if (progressBar != null)
                {
                    progressBar.gameObject.SetActive(false);
                    progressBar.SetProgress(0f);
                }
                var food = RemoveFood();
                Destroy(food.gameObject);
                AddFood(foodSpawner.SpawnFood(recipe.Result));
                recipe = null;
            }
        }
    }
}
