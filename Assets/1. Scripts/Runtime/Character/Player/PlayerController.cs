// Owned by JunYoung Park
using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class PlayerController : FoodHolder
{
    [Header("Food")]
    [Networked] public NetworkObject HeldFoodObject { get; set; }
    [SerializeField] private Transform holdAnchor;

    public Transform HoldAnchor => holdAnchor;
    public Food HeldFood => HeldFoodObject != null ? HeldFoodObject.GetComponent<Food>() : null;


    public bool HasFood() => HeldFoodObject != null;

    public override void Spawned()
    {
        if (holdAnchor == null)
            holdAnchor = transform;

        if (HasStateAuthority)
        {
            HeldFoodObject = null;
            if(GameManager.Instance == null) GameManager.BindInitializer(GameManagerActionsSetup);
            else GameManagerActionsSetup();
        }

    }
    private void GameManagerActionsSetup()
    {
        FreezeMovement(true);
        GameManager.Instance.OnStageStart += RPC_HandleStageStart;
        GameManager.Instance.OnResult += RPC_HandleStageEnd;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance == null) return;

        GameManager.Instance.OnStageStart -= RPC_HandleStageStart;
        GameManager.Instance.OnResult -= RPC_HandleStageEnd;
    }

    public void FreezeMovement(bool apply)
    {
        GetComponent<PlayerMovement>().SetInteracting(apply);
    }


    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_SetParent(NetworkObject food, Vector3 pos)
    {
        var foodName = food != null ? food.name : "null";
        Debug.Log($"[Player/P{Object.StateAuthority.PlayerId}] RPC_SetParent received on local. food={foodName} pos={pos}");

        Vector3 targetWorldPos = holdAnchor.TransformPoint(pos);
        Quaternion targetWorldRot = holdAnchor.rotation;

        food.transform.SetParent(holdAnchor, true);

        NetworkTransform fnt = food.GetComponent<NetworkTransform>();
        if(fnt == null) return;
        fnt.Teleport(targetWorldPos, targetWorldRot);
    }

    protected override void OnAdded(NetworkObject food, Vector3 pos)
    {
        var foodName = food != null ? food.name : "null";
        Debug.Log($"[Player/P{Object.StateAuthority.PlayerId}] OnAdded called. food={foodName} pos={pos} HasFoodAuth={(food != null && food.HasStateAuthority)} HasPlayerAuth={HasStateAuthority}");

        // Side menu check
        List<FoodSO> holding = new List<FoodSO>();
        holding.Add(food.GetComponent<Food>().Data);
        var recipe = RecipeManager.Instance.Cook(holding, RecipeType.Side);
        if (recipe != null)
        {
            Debug.Log("[PlayerController OnAdded] Side menu detected");
            Runner.Despawn(food);
            food = Runner.Spawn(recipe.Result.Prefab);
        }
        else Debug.Log("[PlayerController OnAdded] not side menu");

        // Hold
        HeldFoodObject = food;
        RPC_SetParent(HeldFoodObject, pos);
        HeldFood.SetHeld();
        var heldName = HeldFoodObject != null ? HeldFoodObject.name : "null";
        Debug.Log($"[Player/P{Object.StateAuthority.PlayerId}] OnAdded done. HeldFoodObject={heldName} food.IsHeld={HeldFood.IsHeld}");
    }

    protected override void OnRemoved(NetworkObject food)
    {
        var foodName = food != null ? food.name : "null";
        Debug.Log($"[Player/P{Object.StateAuthority.PlayerId}] OnRemoved called. food={foodName} HasFoodAuth={(food != null && food.HasStateAuthority)} HasPlayerAuth={HasStateAuthority}");
        if (HeldFood != null) HeldFood.SetDrop();
        HeldFoodObject = null;
        Debug.Log($"[Player/P{Object.StateAuthority.PlayerId}] OnRemoved done. HeldFoodObject=null");
    }

    public override bool CanAdd(Food food)
    {
        var ok = food != null && HeldFoodObject == null;
        var foodName = food != null ? food.name : "null";
        var heldName = HeldFoodObject != null ? HeldFoodObject.name : "null";
        Debug.Log($"[Player/P{Object.StateAuthority.PlayerId}] CanAdd({foodName}) = {ok} (HeldFoodObject={heldName})");
        return ok;
    }

    public override bool CanRemove()
    {
        var ok = HeldFoodObject != null;
        var heldName = HeldFoodObject != null ? HeldFoodObject.name : "null";
        Debug.Log($"[Player/P{Object.StateAuthority.PlayerId}] CanRemove() = {ok} (HeldFoodObject={heldName})");
        return ok;
    }

    public override void ClearAll(Action onDone = null)
    {
        if (HeldFoodObject == null)
        {
            onDone?.Invoke();
            return;
        }
        Discard(HeldFoodObject, onDone);
    }

    public NetworkObject ReleaseFood()
    {
        NetworkObject released = HeldFoodObject;
        HeldFoodObject = null;
        return released;
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_HandleStageStart() {
        if(!HasStateAuthority) return;
        Debug.Log("[PlayerController HandleStageStart] called to unfreeze player.");
        FreezeMovement(false);
    }
    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_HandleStageEnd() {
        if(!HasStateAuthority) return;
        FreezeMovement(true);
    }
}
