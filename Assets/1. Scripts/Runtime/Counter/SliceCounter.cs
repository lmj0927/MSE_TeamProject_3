// Owned by MinJun Lee
using Fusion;
using UnityEngine;

public class SliceCounter : ACounter
{
    private int sliceCount = 5;
    [SerializeField] private ProgressBar progressBar;

    private bool isSlicing = false;
    private int currentSliceCount = 0;
    private RecipeSO recipe;

    public override void Spawned()
    {
        base.Spawned();

        if (progressBar != null)
        {
            progressBar.gameObject.SetActive(false);
            progressBar.SetProgress(0f);
        }
    }

    public override void Interact(PlayerController player)
    {
        AuthorityHandler.RequestStateAuthority(
            onAuthorized: () =>
            {
                if (CanAddFood(player))
                {
                    // RPC_AddFood(player.RemoveFood(), foodPoint.position);
                    FoodTransfer.Transfer(player, this, player.HeldFoodObject, Vector3.zero);
                    recipe = RecipeManager.Instance.Cook(GetFoodSOs(), RecipeType.Slice);
                    if (recipe == null) return;

                    sliceCount = recipe.Value;

                    isSlicing = true;
                    if (progressBar != null)
                        progressBar.gameObject.SetActive(true);
                    return;
                }
                else if (CanRemoveFood(player) && !isSlicing)
                {
                    // player.AddFood(RemoveFood());
                    FoodTransfer.Transfer(this, player, GetLastFood(), Vector3.zero);
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
                        var food = GetLastFood();
                        OnRemoved(food);
                        FoodSpawner.Despawn(Runner, food);
                        // if (recipe == null) AddFood(FoodSpawner.SpawnFood(Runner, RecipeManager.Instance.GetTrashFood()));
                        // else
                        // {
                        //     AddFood(FoodSpawner.SpawnFood(Runner, recipe.Result));
                        //     recipe = null;
                        // }
                        FoodSO foodSO = (recipe == null) ? RecipeManager.Instance.GetTrashFood() : recipe.Result;
                        NetworkObject foodNO = FoodSpawner.SpawnFood(Runner, foodSO);
                        OnAdded(foodNO, foodPoint.position);
                    }
                }
                
            },
            onNotAuthorized: () =>
            {
                
            }
        );
        // if (CanAddFood(player))
        // {
        //     RPC_AddFood(player.RemoveFood(), foodPoint.position);
        //     recipe = RecipeManager.Instance.Cook(GetFoodSOs(), RecipeType.Slice);
        //     if (recipe == null) return;

        //     sliceCount = recipe.Value;

        //     isSlicing = true;
        //     if (progressBar != null)
        //         progressBar.gameObject.SetActive(true);
        //     return;
        // }
        // else if (CanRemoveFood(player) && !isSlicing)
        // {
        //     player.AddFood(RemoveFood());
        //     return;
        // }
        // else if (isSlicing)
        // {
        //     SoundManager.Instance.Slice();
        //     currentSliceCount++;
        //     if (progressBar != null)
        //         progressBar.SetProgress((float)currentSliceCount / sliceCount);
        //     if (currentSliceCount >= sliceCount)
        //     {
        //         isSlicing = false;
        //         currentSliceCount = 0;
        //         if (progressBar != null)
        //         {
        //             progressBar.gameObject.SetActive(false);
        //             progressBar.SetProgress(0f);
        //         }
        //         var food = RemoveFood();
        //         FoodSpawner.Despawn(Runner, food);
        //         if (recipe == null) AddFood(FoodSpawner.SpawnFood(Runner, RecipeManager.Instance.GetTrashFood()));
        //         else
        //         {
        //             AddFood(FoodSpawner.SpawnFood(Runner, recipe.Result));
        //             recipe = null;
        //         }
        //     }
        // }
    }
}
