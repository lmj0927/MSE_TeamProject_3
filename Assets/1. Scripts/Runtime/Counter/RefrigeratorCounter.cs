// Owned by MinJun Lee
using System;
using UnityEngine;
using DG.Tweening;
using Fusion;

/// <summary>
/// Refrigerator counter with ingredient popup and door animation.
/// </summary>
public class RefrigeratorCounter : ACounter
{
    [SerializeField] private IngredientPopupUI ingredientPopupUI; // ingredient selection UI
    private PlayerController interactPlayer; // player using fridge

    [SerializeField] private GameObject[] hinges; // door hinge objects
    [SerializeField] private float openAngle = 40f; // door open angle
    [SerializeField] private float doorAnimDuration = 0.5f; // door tween duration
    private float interactionCooltime = 0.2f; // reuse cooldown


    [Networked, OnChangedRender(nameof(OnChangedIsOpen))] private bool isOpen { get; set; }

    public override void Spawned()
    {
        base.Spawned();

        ingredientPopupUI.OnIngredientSelected += OnIngredientSelected;
    }

    public void Update()
    {
        if (interactionCooltime > 0) interactionCooltime -= Time.deltaTime;

        // cancel selection with Escape
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
                    // open popup and freeze player for ingredient pick
                    AuthorityHandler.Barrier();
                    interactPlayer = player;
                    interactPlayer.FreezeMovement(true);
                    isOpen = true;
                    ingredientPopupUI.Show();
                }
                else if(interactionCooltime <= 0 && player.HasFood())
                {
                    // store raw ingredient back into fridge
                    var tmp =  player.HeldFood.Data;
                    if ( tmp.FoodName == "Trash" || tmp.Type != FoodSO.FoodType.Raw) return;

                    isOpen = true;

                    player.Discard(player.HeldFoodObject, () =>
                    {
                        // close door shortly after discard animation
                        DOVirtual.DelayedCall(doorAnimDuration * 0.7f, () =>
                        {
                            isOpen = false;
                        });
                    });
                }
            },
            onNotAuthorized: () =>
            {
                Debug.Log($"[Counter/{name}] well.. denied. It might be because of the barrier by someone.");
            }
        );
    }

    // spawn selected ingredient and hand to player
    private void OnIngredientSelected(FoodSO foodSO)
    {
        if (interactPlayer == null)
        {
            UnuseReset();
            return;
        }

        var target = interactPlayer;
        Place(foodSO, Vector3.zero, spawned =>
        {
            if (spawned == null)
            {
                UnuseReset();
                return;
            }
            HandoffTo(target, spawned, Vector3.zero, () => UnuseReset());
        });
    }

    // unbarrier, close UI, unfreeze player, close door
    private void UnuseReset()
    {
        AuthorityHandler.Unbarrier();
        if (interactPlayer != null)
        {
            interactPlayer.FreezeMovement(false);
            interactPlayer = null;
        }
        isOpen = false;
        interactionCooltime = 0.2f;
    }

    // tween door hinges open or closed
    private void SetDoors(bool isOpen)
    {
        hinges[0].transform.DOKill();
        hinges[1].transform.DOKill();

        float targetAngle = isOpen ? openAngle : 0f;

        // left and right hinges rotate opposite directions
        hinges[0].transform.DOLocalRotate(new Vector3(0, targetAngle, 0), doorAnimDuration)
            .SetEase(Ease.OutQuad);

        hinges[1].transform.DOLocalRotate(new Vector3(0, -targetAngle, 0), doorAnimDuration)
            .SetEase(Ease.OutQuad);
    }

    private void OnChangedIsOpen() => SetDoors(isOpen);

    // Unneccesary check in the refrigerator since it is the exceptional case which does not hold a food but is a counter.
    public override bool CanAdd(Food food) => true;
    public override bool CanRemove() => true;

    protected override void OnAdded(NetworkObject food, Vector3 pos) { }
    protected override void OnRemoved(NetworkObject food) { }

    public override void ClearAll(Action onDone = null) { onDone?.Invoke(); }
}
