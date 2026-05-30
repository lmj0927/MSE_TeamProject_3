// Owned by MinJun Lee
using Fusion;
using UnityEngine;

public class SliceCounter : ACounter
{
    private int sliceCount = 5;
    [SerializeField] private ProgressBar progressBar;

    [Networked, OnChangedRender(nameof(OnIsSlicingChanged))] private bool isSlicing { get; set; }
    [Networked, OnChangedRender(nameof(OnSlicingCountChanged))] private int currentSliceCount { get; set; }
    private RecipeSO recipe;

    public override void Spawned()
    {
        base.Spawned();

        if (progressBar != null)
        {
            progressBar.gameObject.SetActive(false);
            progressBar.SetProgress(0f);
        }

        if(HasStateAuthority)
        {
            isSlicing = false;
            currentSliceCount = 0;
        }
    }

    public override void Interact(PlayerController player)
    {
        if(!player.HasStateAuthority) return;
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

                    isSlicing = true; // call callback

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
                    currentSliceCount++; // call callback
                    
                    if (currentSliceCount >= sliceCount)
                    {
                        isSlicing = false; // call callback
                        currentSliceCount = 0; // call callback

                        var food = GetLastFood();
                        OnRemoved(food);
                        food.GetComponent<AuthorityHandler>().RequestStateAuthority(
                            onAuthorized: () => FoodSpawner.Despawn(Runner, food),
                            onNotAuthorized: () => {}
                        );
                        FoodSO foodSO = (recipe == null) ? RecipeManager.Instance.GetTrashFood() : recipe.Result;
                        NetworkObject foodNO = FoodSpawner.SpawnFood(Runner, foodSO);
                        OnAdded(foodNO, Vector3.zero);
                    }
                }
                
            },
            onNotAuthorized: () =>
            {
                
            }
        );
    }

    private void OnIsSlicingChanged()
    {
        if(isSlicing)
        {
            if (progressBar != null)
                progressBar.gameObject.SetActive(true);    
        }
    }

    private void OnSlicingCountChanged()
    {
        if (progressBar != null)
            progressBar.SetProgress((float)currentSliceCount / sliceCount);
        
        SoundManager.Instance.Slice();
        
        if(currentSliceCount >= sliceCount)
        {
            progressBar.gameObject.SetActive(false);
            progressBar.SetProgress(0f);
        }
    }
}
