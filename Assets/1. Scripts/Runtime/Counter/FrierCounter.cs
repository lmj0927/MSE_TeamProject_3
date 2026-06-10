// Owned by JunYoung Park
using System.Linq;
using Fusion;
using UnityEngine;

public class FrierCounter : AFireCounter
{
    /// <summary>
    /// 2 frier slots are worked by one interaction.
    /// </summary>
    [SerializeField] private FrierCounter otherside;

    /// <summary>
    /// Synced state of downed basket.
    /// </summary>
    [Networked,  OnChangedRender(nameof(OnIsBasketDownChanged))] private bool isBasketDown { get; set; }

    [SerializeField] private ParticleSystem boiling;

    /// <summary>
    /// Offset for putting a food into an oil.
    /// </summary>
    private Vector3 offset = new Vector3(0, -0.13f, 0.05f);

    protected override RecipeType CookRecipeType => RecipeType.Oil;

    public override void Spawned()
    {
        base.Spawned();

        if(HasStateAuthority)
        {
            isBasketDown = false;
        }
    }

    /// <summary>
    /// Handles player interaction: add food, lower/raise basket, or take cooked food.
    /// </summary>
    public override void Interact(PlayerController player)
    {
        if(!player.HasStateAuthority) return;
        AuthorityHandler.RequestStateAuthority(
            onAuthorized: () =>
            {
                otherside.AuthorityHandler.RequestStateAuthority(
                    onAuthorized: () =>
                    {
                        if (!isBasketDown && CanAdd(player.HeldFood))
                        {
                            player.HandoffTo(this, player.HeldFoodObject, Vector3.zero, () =>
                            {
                                var recipe = RecipeManager.Instance.Cook(GetFoodSOs(), RecipeType.Oil);
                                if (recipe != null)
                                {
                                    float maxAllowedTime = 10f - burnTime;

                                    if (recipe.Value > maxAllowedTime)
                                    {
                                        Debug.LogWarning($"[FrierCounter] The frying time ({recipe.Value} sec) for {recipe.Result.FoodName} is too long." +
                                                         $"Due to SFX length limit, it has been auto-modified to {maxAllowedTime}sec. Please check the Recipe SO.");
                                    }

                                    cookTime = recipe.Value;
                                }
                            });
                        }
                        else if (!isBasketDown && !isDone && HasFood())
                        {
                            isBasketDown = true;
                            otherside.SetBasket(true);
                        }
                        else if (isDone && isBasketDown)
                        {
                            isBasketDown = false;
                            otherside.SetBasket(false);
                        }
                        else if (isDone && !player.HasFood() && CanRemove())
                        {
                            isDone = false;
                            HandoffTo(player, GetLastFood(), Vector3.zero);
                        }
                    },
                    onNotAuthorized: () => Debug.LogWarning("[FrierCounter Interact] Otherside Denied")
                );
            },
            onNotAuthorized: () => Debug.LogWarning("[FrierCounter Interact] Denied")
        );
    }

    /// <summary>
    /// Networked callback: starts or finishes frying when basket state changes.
    /// </summary>
    private void OnIsBasketDownChanged()
    {
        if(isBasketDown) StartFry();
        else             FinishFry();
    }

    /// <summary>
    /// Lowers food into oil, plays effects, and begins cooking.
    /// </summary>
    public void StartFry()
    {
        transform.position += offset;

        if (HasFood())
        {
            foreach (var f in foods.Select(food => Runner.FindObject(food)))
            {
                f.GetComponent<AuthorityHandler>().RequestStateAuthority(
                    // onAuthorized: () => f.transform.position += offset,
                    onAuthorized: () => 
                    {
                        NetworkTransform fnt = f.GetComponent<NetworkTransform>();
                        fnt.Teleport(transform.position + offset);
                    },
                    onNotAuthorized: () => {}
                );
            }
            if (HasStateAuthority) RPC_PlayEffects();
            SetState(CookState);
        }
    }

    /// <summary>
    /// Raises food out of oil and stops effects. onlyBoil stops effects without moving food.
    /// </summary>
    public void FinishFry(bool onlyBoil = false)
    {
        if (HasStateAuthority) RPC_StopEffects();

        if (onlyBoil) return;

        transform.position -= offset;

        if (HasFood())
        {
            foreach (var f in foods.Select(food => Runner.FindObject(food)))
            {
                f.GetComponent<AuthorityHandler>().RequestStateAuthority(
                    // onAuthorized: () => f.transform.position = foodPoint.position,
                    onAuthorized: () => 
                    {
                        NetworkTransform fnt = f.GetComponent<NetworkTransform>();
                        fnt.Teleport(foodPoint.position);
                    },
                    onNotAuthorized: () => {}
                );
            }
            SetState(NoneState);
        }
    }

    /// <summary>
    /// Syncs basket state from the other paired frier.
    /// </summary>
    public void SetBasket(bool val)
    {
        isBasketDown = val;
    }

    /// <summary>
    /// Plays fry sound and boiling particle on all clients.
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayEffects()
    {
        SoundManager.Instance.FryStart(this);
        boiling.Play();
    }


    /// <summary>
    /// Stops boiling particle and fry sound on all clients.
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_StopEffects()
    {
        boiling.Stop();
        OnCookFinished?.Invoke(); // fire on every client so each local SoundManager stops its fry audio
    }
}
