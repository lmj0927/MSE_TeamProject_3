// Owned by JunYoung Park
using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

// Controls player's food holding mechanics and stage-based movement freezing
public class PlayerController : FoodHolder
{
    /// <summary>
    /// Data for the food object which player is holding.
    /// </summary>
    [Header("Food")]
    [Networked] public NetworkObject HeldFoodObject { get; set; }
    /// <summary>
    /// Transform to which the food is stick.
    /// </summary>
    [SerializeField] private Transform holdAnchor;

    public Transform HoldAnchor => holdAnchor;
    
    /// <summary>
    /// It just gives a `Food` data of `HeldFoodObject`.
    /// </summary>
    public Food HeldFood => HeldFoodObject != null ? HeldFoodObject.GetComponent<Food>() : null;


    public bool HasFood() => HeldFoodObject != null;

    /// <summary>
    /// Called on spawned.
    /// Player, at first, is freeze and the `GameManager` manages the unfreezing 
    /// according to the game status.
    /// Thus the binding is needed.
    /// </summary>
    public override void Spawned()
    {
        if (holdAnchor == null)
            holdAnchor = transform;

        if (HasStateAuthority)
        {
            HeldFoodObject = null;
            if (GameManager.Instance == null) GameManager.BindInitializer(GameManagerActionsSetup);
            else GameManagerActionsSetup();
        }
    }
    /// <summary>
    /// Register an player freezing functions according to the status of the game.
    /// </summary>
    private void GameManagerActionsSetup()
    {
        FreezeMovement(true);
        GameManager.Instance.OnStageStart += RPC_HandleStageStart;
        GameManager.Instance.OnResult += RPC_HandleStageEnd;
    }

    /// <summary>
    /// Unregister the freezing functions.
    /// </summary>
    private void OnDestroy()
    {
        if (GameManager.Instance == null) return;

        GameManager.Instance.OnStageStart -= RPC_HandleStageStart;
        GameManager.Instance.OnResult -= RPC_HandleStageEnd;
    }

    /// <summary>
    /// Lock or unlock player movement input
    /// </summary>
    /// <param name="apply">freeze or not</param>
    public void FreezeMovement(bool apply)
    {
        GetComponent<PlayerMovement>().SetInteracting(apply);
    }


    /// <summary>
    /// Sync held food's parent and position across all clients using RPC
    /// </summary>
    /// <param name="food">Set this `food` as held food visually.</param>
    /// <param name="pos">Offset from the `holdAnchor`</param>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_SetParent(NetworkObject food, Vector3 pos)
    {
        var foodName = food != null ? food.name : "null";
        Debug.Log($"[Player/P{Object.StateAuthority.PlayerId}] RPC_SetParent received on local. food={foodName} pos={pos}");

        Vector3 targetWorldPos = holdAnchor.TransformPoint(pos);
        Quaternion targetWorldRot = holdAnchor.rotation;

        food.transform.SetParent(holdAnchor, true);

        NetworkTransform fnt = food.GetComponent<NetworkTransform>();
        if (fnt == null) return;
        fnt.Teleport(targetWorldPos, targetWorldRot);
    }

    /// <summary>
    /// Triggered when the player successfully picks up food
    /// </summary>
    /// <param name="food">What `food` the player is going to hold.</param>
    /// <param name="pos">Offset from the `holdAnchor`</param>
    protected override void OnAdded(NetworkObject food, Vector3 pos)
    {
        var foodName = food != null ? food.name : "null";
        Debug.Log($"[Player/P{Object.StateAuthority.PlayerId}] OnAdded called. food={foodName} pos={pos} HasFoodAuth={(food != null && food.HasStateAuthority)} HasPlayerAuth={HasStateAuthority}");

        // Automatically transform raw ingredients into side menus upon pickup if recipes match
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

        // Assign food to anchor and update states
        HeldFoodObject = food;
        RPC_SetParent(HeldFoodObject, pos);
        HeldFood.SetHeld();
        var heldName = HeldFoodObject != null ? HeldFoodObject.name : "null";
        Debug.Log($"[Player/P{Object.StateAuthority.PlayerId}] OnAdded done. HeldFoodObject={heldName} food.IsHeld={HeldFood.IsHeld}");
    }

    /// <summary>
    /// Triggered when the player drops or serves the food
    /// </summary>
    /// <param name="food">Remove this `food`</param>
    protected override void OnRemoved(NetworkObject food)
    {
        var foodName = food != null ? food.name : "null";
        Debug.Log($"[Player/P{Object.StateAuthority.PlayerId}] OnRemoved called. food={foodName} HasFoodAuth={(food != null && food.HasStateAuthority)} HasPlayerAuth={HasStateAuthority}");
        if (HeldFood != null) HeldFood.SetDrop();
        HeldFoodObject = null;
        Debug.Log($"[Player/P{Object.StateAuthority.PlayerId}] OnRemoved done. HeldFoodObject=null");
    }

    /// <summary>
    /// Check whether the `food` can be held.
    /// </summary>
    /// <param name="food">Check this can be held.</param>
    /// <returns>can add or not</returns>
    public override bool CanAdd(Food food)
    {
        var ok = food != null && HeldFoodObject == null;
        var foodName = food != null ? food.name : "null";
        var heldName = HeldFoodObject != null ? HeldFoodObject.name : "null";
        Debug.Log($"[Player/P{Object.StateAuthority.PlayerId}] CanAdd({foodName}) = {ok} (HeldFoodObject={heldName})");
        return ok;
    }

    /// <summary>
    /// Check whether the food can be removed from here.
    /// </summary>
    /// <returns>can remove or not</returns>
    public override bool CanRemove()
    {
        var ok = HeldFoodObject != null;
        var heldName = HeldFoodObject != null ? HeldFoodObject.name : "null";
        Debug.Log($"[Player/P{Object.StateAuthority.PlayerId}] CanRemove() = {ok} (HeldFoodObject={heldName})");
        return ok;
    }

    /// <summary>
    /// Clear the foods.
    /// </summary>
    /// <param name="onDone">Callback after the food remove is done</param>
    public override void ClearAll(Action onDone = null)
    {
        if (HeldFoodObject == null)
        {
            onDone?.Invoke();
            return;
        }
        Discard(HeldFoodObject, onDone);
    }

    /// <summary>
    /// Detach and return the held food object
    /// </summary>
    /// <returns>HeldFoodObject</returns>
    public NetworkObject ReleaseFood()
    {
        NetworkObject released = HeldFoodObject;
        HeldFoodObject = null;
        return released;
    }

    /// <summary>
    /// Unfreeze player movement when the stage begins
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_HandleStageStart()
    {
        if (!HasStateAuthority) return;
        Debug.Log("[PlayerController HandleStageStart] called to unfreeze player.");
        FreezeMovement(false);
    }

    /// <summary>
    /// Freeze player movement when the stage ends (Result screen)
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_HandleStageEnd()
    {
        if (!HasStateAuthority) return;
        FreezeMovement(true);
    }
}