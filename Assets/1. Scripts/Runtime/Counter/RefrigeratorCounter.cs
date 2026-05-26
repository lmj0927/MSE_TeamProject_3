// Owned by MinJun Lee
using UnityEngine;
using DG.Tweening;
using Fusion;
public class RefrigeratorCounter : ACounter
{
    [SerializeField] private IngredientPopupUI ingredientPopupUI;
    private PlayerController interactPlayer;

    [SerializeField] private GameObject[] hinges;
    [SerializeField] private float openAngle = 40f;
    [SerializeField] private float doorAnimDuration = 0.5f;
    private float interactionCooltime = 0.2f;
    public override void Spawned()
    {
        base.Spawned();
        
        ingredientPopupUI.OnIngredientSelected += OnIngredientSelected;
    }

    public override void FixedUpdateNetwork()
    {
        if (interactionCooltime > 0) interactionCooltime -= Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            UnuseReset();
            ingredientPopupUI.Hide();
        }
    }

    public override void Interact(PlayerController player)
    {
        if (!player.HasFood())
        {
            interactPlayer = player;
            interactPlayer.FreezeMovement(true);
            SetDoors(true);
            ingredientPopupUI.Show();
        } else if(interactionCooltime <= 0 && player.HasFood())
        {
            var tmp =  player.HeldFood.Data;
            if ( tmp.FoodName == "Trash" || tmp.Type != FoodSO.FoodType.Raw) return;

            SetDoors(true);

            Destroy(player.RemoveFood().gameObject);

            DOVirtual.DelayedCall(doorAnimDuration * 0.7f, () =>
            {
                SetDoors(false);
            });
        }
    }

    private void OnIngredientSelected(FoodSO foodSO)
    {
        NetworkObject food = foodSpawner.SpawnFood(foodSO);
        if(food != null)
            FoodTransfer.Transfer(this, interactPlayer, food, Vector3.zero);

        UnuseReset();
    }

    private void UnuseReset()
    {
        if (interactPlayer != null)
        {
            interactPlayer.FreezeMovement(false);
            interactPlayer = null;
        }
        SetDoors(false);
        interactionCooltime = 0.2f;
    }
    private void SetDoors(bool isOpen)
    {
        hinges[0].transform.DOKill();
        hinges[1].transform.DOKill();

        float targetAngle = isOpen ? openAngle : 0f;

        hinges[0].transform.DOLocalRotate(new Vector3(0, targetAngle, 0), doorAnimDuration)
            .SetEase(Ease.OutQuad);

        hinges[1].transform.DOLocalRotate(new Vector3(0, -targetAngle, 0), doorAnimDuration)
            .SetEase(Ease.OutQuad);
    }

    public override bool CanRemove() => true;
    public override void OnRemoved(NetworkObject food) {}
}
