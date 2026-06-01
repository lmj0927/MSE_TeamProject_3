// Owned by MinJun Lee
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Fusion;

public abstract class ACounter : FoodHolder, IInteractable
{
    [SerializeField] protected Transform foodPoint;

    public Transform FoodPoint => foodPoint;


    /// <summary>
    /// List of the network object ids for each of the food object.
    /// </summary>
    [Networked, Capacity(16)] protected NetworkLinkedList<NetworkId> foods { get; }

    /// <summary>
    /// List of the positions of each foods.
    /// </summary>
    // [Networked, Capacity(16)] protected NetworkLinkedList<Vector3> foodPositions { get; }

    // public AuthorityHandler AuthorityHandler => GetComponent<AuthorityHandler>();

    public override void Spawned()
    {
    }

    [SerializeField] private Transform outlineRoot;

    public Transform OutlineRoot => outlineRoot;
    public virtual void Interact(PlayerController player) { }

    protected bool HasFood()
    {
        return foods.Count > 0;
    }

    protected List<FoodSO> GetFoodSOs()
    {
        return foods.Select(f => Runner.FindObject(f).GetComponent<Food>().Data).ToList();
    }

    protected NetworkObject GetLastFood()
    {
        if(foods.Count == 0) return null;
        return Runner.FindObject(foods.Last());
    }

    protected virtual bool AcceptsFood(FoodSO foodData)
    {
        if (foodData.Type == FoodSO.FoodType.Side || foodData.Type == FoodSO.FoodType.Beverage) return false;

        return true;
    }

    // 플레이어가 음식을 들고 있고 카운터에 음식이 없을 때 + 놓을 수 있는 음식일 때
    protected bool CanAddFood(PlayerController player)
    {
        return player.HasFood() && !HasFood() && AcceptsFood(player.HeldFood.Data);
    }

    // 플레이어가 음식을 들고 있지 않고 카운터에 음식이 있을 때
    protected bool CanRemoveFood(PlayerController player)
    {
        return !player.HasFood() && HasFood();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_SetParent(NetworkObject food, Vector3 offset)
    {
        if(food == null) return;

        var foodName = food != null ? food.name : "null";
        Debug.Log($"[Counter/{name}] RPC_SetParent received on local. food={foodName} pos={offset} foodPoint={foodPoint.position}");
        Vector3 targetWorldPos = foodPoint.TransformPoint(offset);
        Quaternion targetWorldRot = foodPoint.rotation;

        food.transform.position = targetWorldPos;
        food.transform.rotation = targetWorldRot;

        food.transform.SetParent(foodPoint, true);
    }

    protected override void OnAdded(NetworkObject food, Vector3 offset)
    {
        if(food == null) return;

        var foodName = food != null ? food.name : "null";
        Debug.Log($"[Counter/{name}] OnAdded called. food={foodName} offset={offset} HasFoodAuth={(food != null && food.HasStateAuthority)} HasCounterAuth={HasStateAuthority} foodsCount(before)={foods.Count}");

        RPC_SetParent(food, offset);

        food.GetComponent<Food>().SetDrop();
        foods.Add(food);

        Debug.Log($"[Counter/{name}] OnAdded done. foodsCount(after)={foods.Count}");
    }

    protected override void OnRemoved(NetworkObject food)
    {
        var foodName = food != null ? food.name : "null";
        Debug.Log($"[Counter/{name}] OnRemoved called. food={foodName} HasFoodAuth={(food != null && food.HasStateAuthority)} HasCounterAuth={HasStateAuthority} foodsCount(before)={foods.Count}");

        food.GetComponent<Food>().SetDrop();
        foods.Remove(food);

        Debug.Log($"[Counter/{name}] OnRemoved done. foodsCount(after)={foods.Count}");
    }

    public override bool CanAdd(Food food)
    {
        var accept = food != null && AcceptsFood(food.Data);
        var ok = !HasFood() && accept;
        var foodDesc = food != null && food.Data != null ? food.Data.FoodName : "null";
        Debug.Log($"[Counter/{name}] CanAdd({foodDesc}) = {ok} (HasFood={HasFood()} AcceptsFood={accept})");
        return ok;
    }

    public override bool CanRemove()
    {
        var ok = HasFood();
        Debug.Log($"[Counter/{name}] CanRemove() = {ok} (foodsCount={foods.Count})");
        return ok;
    }

    public override void ClearAll(Action onDone = null)
    {
        var snapshot = foods.Select(fid => Runner.FindObject(fid)).Where(f => f != null).ToList();
        if (snapshot.Count == 0)
        {
            onDone?.Invoke();
            return;
        }

        int remaining = snapshot.Count;
        foreach (var food in snapshot)
        {
            Discard(food, () =>
            {
                remaining--;
                if (remaining == 0) onDone?.Invoke();
            });
        }
    }
}
