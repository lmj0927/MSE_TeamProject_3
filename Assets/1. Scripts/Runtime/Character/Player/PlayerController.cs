// Owned by SeungYeon Jung
using Fusion;
using UnityEngine;
using System.Collections.Generic;

public class PlayerController : NetworkBehaviour, IFoodHolder
{
    [Header("Food")]
    [Networked] public NetworkObject HeldFoodObject { get; set; }
    [SerializeField] private Transform holdAnchor;

    public Transform HoldAnchor => holdAnchor;
    public Food HeldFood => HeldFoodObject != null ? HeldFoodObject.GetComponent<Food>() : null;


    public bool HasFood() => HeldFoodObject != null;
    
    public AuthorityHandler AuthorityHandler => GetComponent<AuthorityHandler>();

    public override void Spawned()
    {
        if (holdAnchor == null)
            holdAnchor = transform;

        if (HasStateAuthority)
            HeldFoodObject = null;
    }
    private void Start()
    {
        FreezeMovement(true);
        GameManager.Instance.OnStageStart += HandleStageStart;
        GameManager.Instance.OnResult += HandleStageEnd;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance == null) return;

        GameManager.Instance.OnStageStart -= HandleStageStart;
        GameManager.Instance.OnResult -= HandleStageEnd;
    }
    public bool AddFood(NetworkObject food)
    {
        if (foodNO == null || HeldFoodObject != null)
            return false;

        HeldFoodObject = foodNO;
        // foodNO.RequestStateAuthority();

        // var food = foodNO.GetComponent<Food>();
        // if (food != null)
        // {
        //     Debug.Log("[PlayerController AddFood] HeldFoodObject try to call food RPC_SetHolder");
        //     food.RPC_SetHolder(Food.HolderKind.Player, this, Vector3.zero);
        // }

		// TODO-Photon: What it means? 
		//List<FoodSO> holding = new List<FoodSO>();

        //holding.Add(food.data); 
        //var recipe = RecipeManager.Instance.Cook(holding, RecipeType.Side);
        //if (recipe != null)
        //{
        //    Destroy(food.gameObject);
        //    food = recipe.Result.CreateFood();
        //}

        //heldFood = food;
        //AttachHeldFood(food);

        HeldFoodObject.GetComponent<AuthorityHandler>().RequestStateAuthority(
            onAuthorized: () => 
            {
                Debug.Log("[PlayerController AddFood] Authorized.");

                HeldFoodObject.transform.SetParent(holdAnchor, false);
                HeldFoodObject.transform.SetPositionAndRotation(holdAnchor.position, holdAnchor.rotation);

                HeldFood.RPC_SetHeld();
            },
            onNotAuthorized: () =>
            {
                Debug.Log("[PlayerController AddFood] Not Authorized.");
            }
        );
        
        Debug.Log("[PlayerController AddFood] HeldFoodObject is " + HeldFood.Data.FoodName);
        return true;
    }

    /// <summary>
    /// Remove the food the player is holding. The next holder (counter) is responsible
    /// for re-assigning the food's Holder via its own RPC; the food keeps following the
    /// player until that happens, which prevents a one-tick "Holder=None" gap.
    /// </summary>
    public NetworkObject RemoveFood()
    {
        if (HeldFoodObject == null) return null;

        HeldFoodObject.GetComponent<AuthorityHandler>().RequestStateAuthority(
            onAuthorized: () => 
            {
                Debug.Log("[PlayerController RemoveFood] Authorized.");

                // HeldFoodObject.transform.SetParent(holdAnchor);
                // HeldFoodObject.transform.SetPositionAndRotation(holdAnchor.position, holdAnchor.rotation);

                HeldFood.RPC_SetDrop();
            },
            onNotAuthorized: () =>
            {
                Debug.Log("[PlayerController RemoveFood] Not Authorized.");
            }
        );

        var removed = HeldFoodObject;
        HeldFoodObject = null;

        return removed;
    }

    public void FreezeMovement(bool apply)
    {
        GetComponent<PlayerMovement>().SetInteracting(apply);
    }


    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_SetParent(NetworkObject food, Vector3 pos)
    {
        var foodName = food != null ? food.name : "null";
        Debug.Log($"[Player/P{Object.StateAuthority.PlayerId}] RPC_SetParent received on local. food={foodName} pos={pos} holdAnchor={holdAnchor.position}");
        food.transform.SetParent(holdAnchor, false);
        food.transform.SetLocalPositionAndRotation(pos, Quaternion.identity);
    }

    public void OnAdded(NetworkObject food, Vector3 pos)
    {
        var foodName = food != null ? food.name : "null";
        Debug.Log($"[Player/P{Object.StateAuthority.PlayerId}] OnAdded called. food={foodName} pos={pos} HasFoodAuth={(food != null && food.HasStateAuthority)} HasPlayerAuth={HasStateAuthority}");
        HeldFoodObject = food;
        // HeldFoodObject.transform.SetParent(holdAnchor, false);
        // HeldFoodObject.transform.SetPositionAndRotation(holdAnchor.position, holdAnchor.rotation);
        RPC_SetParent(HeldFoodObject, pos);
        HeldFood.SetHeld();
        var heldName = HeldFoodObject != null ? HeldFoodObject.name : "null";
        Debug.Log($"[Player/P{Object.StateAuthority.PlayerId}] OnAdded done. HeldFoodObject={heldName} food.IsHeld={HeldFood.IsHeld}");
    }

    public void OnRemoved(NetworkObject food)
    {
        var foodName = food != null ? food.name : "null";
        Debug.Log($"[Player/P{Object.StateAuthority.PlayerId}] OnRemoved called. food={foodName} HasFoodAuth={(food != null && food.HasStateAuthority)} HasPlayerAuth={HasStateAuthority}");
        HeldFood.SetDrop();
        HeldFoodObject = null;
        Debug.Log($"[Player/P{Object.StateAuthority.PlayerId}] OnRemoved done. HeldFoodObject=null");
    }

    public bool CanAdd(Food food)
    {
        var ok = food != null && HeldFoodObject == null;
        var foodName = food != null ? food.name : "null";
        var heldName = HeldFoodObject != null ? HeldFoodObject.name : "null";
        Debug.Log($"[Player/P{Object.StateAuthority.PlayerId}] CanAdd({foodName}) = {ok} (HeldFoodObject={heldName})");
        return ok;
    }

    public bool CanRemove()
    {
        var ok = HeldFoodObject != null;
        var heldName = HeldFoodObject != null ? HeldFoodObject.name : "null";
        Debug.Log($"[Player/P{Object.StateAuthority.PlayerId}] CanRemove() = {ok} (HeldFoodObject={heldName})");
        return ok;
    }
    private void HandleStageStart() => FreezeMovement(false);
    private void HandleStageEnd() => FreezeMovement(true);
}
