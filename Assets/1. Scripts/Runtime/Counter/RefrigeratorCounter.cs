// Owned by MinJun Lee
using System;
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
                    isOpen = true;
                    ingredientPopupUI.Show();
                }
                else if(interactionCooltime <= 0 && player.HasFood())
                {
                    var tmp =  player.HeldFood.Data;
                    if ( tmp.FoodName == "Trash" || tmp.Type != FoodSO.FoodType.Raw) return;

                    isOpen = true;

                    player.Discard(player.HeldFoodObject, () =>
                    {
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

    public override bool CanAdd(Food food) => true;
    public override bool CanRemove() => true;

    protected override void OnAdded(NetworkObject food, Vector3 pos) { }
    protected override void OnRemoved(NetworkObject food) { }

    public override void ClearAll(Action onDone = null) { onDone?.Invoke(); }
}
