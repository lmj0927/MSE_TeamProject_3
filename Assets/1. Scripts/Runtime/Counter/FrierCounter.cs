// Owned by JunYoung Park
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

public class FrierCounter : AFireCounter
{
    [SerializeField] private FrierCounter otherside;
    private bool isBasketDown = false;
    [SerializeField] private ParticleSystem boiling;

    private Vector3 offset = new Vector3(0, -0.13f, 0.05f);

    public override void Interact(PlayerController player)
    {
        AuthorityHandler.RequestStateAuthority(
            onAuthorized: () =>
            {
                if (!isBasketDown && CanAddFood(player))
                {
                    // AddFood(player.RemoveFood());
                    FoodTransfer.Transfer(player, this, player.HeldFoodObject, Vector3.zero);
                    var recipe = RecipeManager.Instance.Cook(GetFoodSOs(), RecipeType.Oil);
                    if (recipe != null)
                    {
                        cookTime = recipe.Value;
                        resultFood = recipe.Result;
                    }
                } 
                else if (!isBasketDown && !isDone && HasFood())
                {
                    isBasketDown = true;
                    otherside.SetBasket(true);

                    StartFry();
                    otherside.StartFry();
                    
                } 
                else if (isDone && isBasketDown)
                {
                    isBasketDown = false;
                    otherside.SetBasket(false);

                    FinishFry();
                    otherside.FinishFry();
                } 
                else if (isDone && CanRemoveFood(player))
                {
                    isDone = false;

                    // player.AddFood(RemoveFood());
                    FoodTransfer.Transfer(this, player, GetLastFood(), Vector3.zero);
                    resultFood = null;
                }
            },
            onNotAuthorized: () =>
            {
                Debug.LogWarning("[FrierCounter Interact] Denied");
            }
        );
    }

    public void StartFry()
    {
        transform.position += offset;

        if (HasFood())
        {
            SoundManager.Instance.FryStart(this);
            foreach (Food f in foods.Select(food => Runner.FindObject(food).GetComponent<Food>()))
            {
                f.transform.position += offset;
            }
            boiling.Play();
            SetState(CookState);
        }
    }

    public void FinishFry(bool onlyBoil = false)
    {
        boiling.Stop();

        if (onlyBoil) return; 

        transform.position -= offset;

        if (HasFood())
        {
            OnCookFinished?.Invoke();
            foreach (Food f in foods.Select(food => Runner.FindObject(food).GetComponent<Food>()))
            {
                f.transform.position = foodPoint.position;
            }
            SetState(NoneState);
        }
    }

    public void SetBasket(bool val)
    {
        isBasketDown = val;
    }
}
