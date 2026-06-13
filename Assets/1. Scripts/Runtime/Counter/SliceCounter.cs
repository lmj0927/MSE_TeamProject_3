// Owned by MinJun Lee
using Fusion;
using UnityEngine;

/// <summary>
/// Counter for slicing food with progress bar.
/// </summary>
public class SliceCounter : ACounter
{
    [Networked] private int sliceCount { get; set; } = 5;
    [SerializeField] private ProgressBar progressBar; // slice progress UI

    // Slicing status should be sync
    [Networked, OnChangedRender(nameof(OnIsSlicingChanged))] private bool isSlicing { get; set; }
    [Networked, OnChangedRender(nameof(OnSlicingCountChanged))] private int currentSliceCount { get; set; }

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
                if (CanAdd(player.HeldFood))
                {
                    // place food and start slicing minigame
                    player.HandoffTo(this, player.HeldFoodObject, Vector3.zero, () =>
                    {
                        RecipeSO recipe = RecipeManager.Instance.Cook(GetFoodSOs(), RecipeType.Slice);
                        if (recipe == null) return;

                        sliceCount = recipe.Value;
                        isSlicing = true;
                    });
                }
                else if (!player.HasFood() && CanRemove() && !isSlicing)
                {
                    // pick up sliced food when idle
                    HandoffTo(player, GetLastFood(), Vector3.zero);
                }
                else if (isSlicing)
                {
                    // each interact counts as one slice action
                    currentSliceCount++;

                    if (currentSliceCount >= sliceCount)
                    {
                        isSlicing = false;
                        currentSliceCount = 0;

                        // replace with slice result or trash on failure
                        RecipeSO recipe = RecipeManager.Instance.Cook(GetFoodSOs(), RecipeType.Slice);
                        FoodSO resultSO = (recipe == null) ? RecipeManager.Instance.GetTrashFood() : recipe.Result;
                        Replace(GetLastFood(), resultSO, Vector3.zero);
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
        if(progressBar == null) return;
        progressBar.gameObject.SetActive(isSlicing);
    }

    private void OnSlicingCountChanged()
    {
        if (progressBar != null)
            progressBar.SetProgress((float)currentSliceCount / sliceCount);

        SoundManager.Instance.Slice();

        // if(currentSliceCount >= sliceCount)
        // {
        //     progressBar.gameObject.SetActive(false);
        //     progressBar.SetProgress(0f);
        // }
    }
}
