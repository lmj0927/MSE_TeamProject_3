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


    [Networked, OnChangedRender(nameof(OnChangedIsOpen))] private bool isOpen { get; set; }
    
    public override void Spawned()
    {
        base.Spawned();
        
        ingredientPopupUI.OnIngredientSelected += OnIngredientSelected;
    }

    public void Update()
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
        if(!player.HasStateAuthority) return;
        AuthorityHandler.RequestStateAuthority(
            onAuthorized: () =>
            {
                if (!player.HasFood())
                {
                    AuthorityHandler.Barrier();
                    interactPlayer = player;
                    interactPlayer.FreezeMovement(true);
                    // SetDoors(true);
                    isOpen = true;
                    ingredientPopupUI.Show();
                } else if(interactionCooltime <= 0 && player.HasFood())
                {
                    var tmp =  player.HeldFood.Data;
                    if ( tmp.FoodName == "Trash" || tmp.Type != FoodSO.FoodType.Raw) return;

                    // SetDoors(true);
                    isOpen = true;

                    // Destroy(player.RemoveFood().sgameObject);
                    
                    FoodSpawner.Despawn(Runner, player.HeldFoodObject);

                    DOVirtual.DelayedCall(doorAnimDuration * 0.7f, () =>
                    {
                        // SetDoors(false);
                        isOpen = false;
                    });
                }
            },
            onNotAuthorized: () =>
            {
                Debug.Log($"[Counter/{name}] well.. denied. It might be because of the barrier by someone.");
            }
        );
    }

    private void OnIngredientSelected(FoodSO foodSO)
    {
        NetworkObject food = FoodSpawner.SpawnFood(Runner, foodSO);
        if(food != null)
            FoodTransfer.Transfer(this, interactPlayer, food, Vector3.zero);

        UnuseReset();
    }

    private void UnuseReset()
    {
        AuthorityHandler.Unbarrier();
        if (interactPlayer != null)
        {
            interactPlayer.FreezeMovement(false);
            interactPlayer = null;
        }
        // SetDoors(false);
        isOpen = false;
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

    private void OnChangedIsOpen() => SetDoors(isOpen);

    public override bool CanRemove() => true;
    public override void OnRemoved(NetworkObject food) {}
}
