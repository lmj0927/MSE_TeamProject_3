using UnityEngine;
using minjun;

public class SliceCounter : ACounter
{
    [SerializeField] private int sliceCount = 5;
    [SerializeField] private ProgressBar progressBar;

    private bool isSlicing = false;
    private int currentSliceCount = 0;

    private void Awake()
    {
        progressBar.gameObject.SetActive(false);
        progressBar.SetProgress(0f);
    }

    public override void Interact(Player player)
    {
        if (CanAddFood(player))
        {
            AddFood(player.RemoveFood());
            isSlicing = true;
            progressBar.gameObject.SetActive(true);
            return;
        }
        else if (CanRemoveFood(player) && !isSlicing)
        {
            player.AddFood(RemoveFood());
            return;
        }
        else if (isSlicing)
        {
            currentSliceCount++;
            progressBar.SetProgress((float)currentSliceCount / sliceCount);
            if (currentSliceCount >= sliceCount)
            {
                isSlicing = false;
                currentSliceCount = 0;
                progressBar.gameObject.SetActive(false);
                progressBar.SetProgress(0f);
                // TODO : Spawn Sliced Food
            }
        }
    }
}
