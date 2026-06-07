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



    [SerializeField] private Transform outlineRoot;

    public Transform OutlineRoot => outlineRoot;

    // public override void Spawned()
    // {
    // }
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

    /// <summary>
    /// Make the `foodPoint` be the parent of the `food` object and set the offset.
    /// It is RPC since it should be executed on every player to.
    /// </summary>
    /// <param name="food"></param>
    /// <param name="offset"></param>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_SetParent(NetworkObject food, Vector3 offset)
    {
        if(food == null) return;

        var foodName = food != null ? food.name : "null";
        Debug.Log($"[Counter/{name}] RPC_SetParent received on local. food={foodName} pos={offset} foodPoint={foodPoint.position}");
        Vector3 targetWorldPos = foodPoint.TransformPoint(offset);
        Quaternion targetWorldRot = foodPoint.rotation;

        food.transform.SetParent(foodPoint, true);

        NetworkTransform fnt = food.GetComponent<NetworkTransform>();
        if(fnt == null) return;
        fnt.Teleport(targetWorldPos, targetWorldRot);
    }

    /// <summary>
    /// Add the `food` into this counter and set position using `offset`.
    /// </summary>
    /// <param name="food">Food to be added.</param>
    /// <param name="offset">Offset from the position of the holder.</param>
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

    /// <summary>
    /// Remove the `food` from this counter.
    /// It suppose that the state authority of the `food` object is already achieved.
    /// </summary>
    /// <param name="food">Food to remove.</param>
    protected override void OnRemoved(NetworkObject food)
    {
        var foodName = food != null ? food.name : "null";
        Debug.Log($"[Counter/{name}] OnRemoved called. food={foodName} HasFoodAuth={(food != null && food.HasStateAuthority)} HasCounterAuth={HasStateAuthority} foodsCount(before)={foods.Count}");

        food.GetComponent<Food>().SetDrop();
        foods.Remove(food);

        Debug.Log($"[Counter/{name}] OnRemoved done. foodsCount(after)={foods.Count}");
    }

    /// <summary>
    /// Check if a food can be held and this counter can hold it.
    /// </summary>
    /// <param name="food">Food which is going to be added.</param>
    /// <returns>True if can be added.</returns>
    public override bool CanAdd(Food food)
    {
        var accept = food != null && AcceptsFood(food.Data);
        var ok = !HasFood() && accept;
        var foodDesc = food != null && food.Data != null ? food.Data.FoodName : "null";
        Debug.Log($"[Counter/{name}] CanAdd({foodDesc}) = {ok} (HasFood={HasFood()} AcceptsFood={accept})");
        return ok;
    }

    /// <summary>
    /// Check if the counter has food.
    /// </summary>
    /// <returns>True if this has foods.</returns>
    public override bool CanRemove()
    {
        var ok = HasFood();
        Debug.Log($"[Counter/{name}] CanRemove() = {ok} (foodsCount={foods.Count})");
        return ok;
    }

    /// <summary>
    /// Despawn all food network objects and clear list.
    /// Call `onDone` after removing all.
    /// </summary>
    /// <param name="onDone">Callback after removing all foods it has.</param>
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
