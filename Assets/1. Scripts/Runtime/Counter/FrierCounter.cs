// Owned by JunYoung Park
using System.Linq;
using Fusion;
using UnityEngine;
using UnityEngine.Serialization;

public class FrierCounter : AFireCounter
{
    [SerializeField] private FrierCounter otherside;
    [Networked,  OnChangedRender(nameof(OnIsBasketDownChanged))] private bool isBasketDown { get; set; }
    [SerializeField] private ParticleSystem boiling;

    private Vector3 offset = new Vector3(0, -0.13f, 0.05f);

    public override void Spawned()
    {
        base.Spawned();

        if(HasStateAuthority)
        {
            isBasketDown = false;
        }
    }

    public override void Interact(PlayerController player)
    {

        if(!player.HasStateAuthority) return;
        AuthorityHandler.RequestStateAuthority(
            onAuthorized: () =>
            {
                otherside.AuthorityHandler.RequestStateAuthority(
                    onAuthorized: () =>
                    {
                        if (!isBasketDown && CanAddFood(player))
                        {
                            // AddFood(player.RemoveFood());
                            FoodTransfer.Transfer(player, this, player.HeldFoodObject, Vector3.zero);
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
                                resultFood = recipe.Result;
                            }
                        } 
                        else if (!isBasketDown && !isDone && HasFood())
                        {
                            isBasketDown = true;
                            otherside.SetBasket(true);

                            // StartFry();
                            // otherside.StartFry();
                            
                        } 
                        else if (isDone && isBasketDown)
                        {
                            isBasketDown = false;
                            otherside.SetBasket(false);

                            // FinishFry();
                            // otherside.FinishFry();
                        } 
                        else if (isDone || CanRemoveFood(player))
                        {
                            isDone = false;

                            // player.AddFood(RemoveFood());
                            FoodTransfer.Transfer(this, player, GetLastFood(), Vector3.zero);
                            resultFood = null;
                        }
                    },
                    onNotAuthorized: () => Debug.LogWarning("[FrierCounter Interact] Otherside Denied")
                );
            },
            onNotAuthorized: () => Debug.LogWarning("[FrierCounter Interact] Denied")
        );
    }

    private void OnIsBasketDownChanged()
    {
        if(isBasketDown) StartFry();
        else             FinishFry();
    }

    public void StartFry()
    {
        transform.position += offset;

        if (HasFood())
        {
            foreach (var f in foods.Select(food => Runner.FindObject(food)))
            {
                f.GetComponent<AuthorityHandler>().RequestStateAuthority(
                    onAuthorized: () => f.transform.position += offset,
                    onNotAuthorized: () => {}
                );
            }
            // boiling.Play();
            if (HasStateAuthority) RPC_PlayEffects();
            SetState(CookState);
        }
    }

    public void FinishFry(bool onlyBoil = false)
    {
        // boiling.Stop();
        if (HasStateAuthority) RPC_StopEffects();

        if (onlyBoil) return;

        transform.position -= offset;

        if (HasFood())
        {
            foreach (var f in foods.Select(food => Runner.FindObject(food)))
            {
                f.GetComponent<AuthorityHandler>().RequestStateAuthority(
                    onAuthorized: () => f.transform.position = foodPoint.position,
                    onNotAuthorized: () => {}
                );
            }
            SetState(NoneState);
        }
    }

    public void SetBasket(bool val)
    {
        isBasketDown = val;
    }



    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayEffects()
    {
        SoundManager.Instance.FryStart(this);
        boiling.Play();
    }


    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_StopEffects()
    {
        boiling.Stop();
        OnCookFinished?.Invoke(); // fire on every client so each local SoundManager stops its fry audio
    }
}
