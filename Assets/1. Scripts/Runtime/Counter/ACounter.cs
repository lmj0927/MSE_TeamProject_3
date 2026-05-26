// Owned by MinJun Lee
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Fusion;

public abstract class ACounter : NetworkBehaviour, IInteractable, IFoodHolder
{
    [SerializeField] protected Transform foodPoint;

    public Transform FoodPoint => foodPoint;

    /// <summary>
    /// Food spawner/despawner for networked food object.
    /// </summary>
    [SerializeField] protected FoodSpawner foodSpawner;

    /// <summary>
    /// List of the network object ids for each of the food object.
    /// </summary>
    [Networked, Capacity(16)] protected NetworkLinkedList<NetworkId> foods { get; }

    /// <summary>
    /// List of the positions of each foods.
    /// </summary>
    [Networked, Capacity(16)] protected NetworkLinkedList<Vector3> foodPositions { get; }

    public AuthorityHandler AuthorityHandler => GetComponent<AuthorityHandler>();

    public override void Spawned()
    {
        foodSpawner = NetworkRunner.GetRunnerForGameObject(gameObject).GetComponent<FoodSpawner>();
        if (foodSpawner == null)
        {
            Debug.LogError("[ACounter Spawned] foodSpawner is null.");
        }
        else if (!foodSpawner.CanSpawn())
        {
            Debug.LogError("[ACounter Spawned] foodSpawner cannot spawn since it is null or Runner.CanSpawn is false.");
        }
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

    protected virtual void AddFood(NetworkObject food)
    {
        Debug.LogError("[ACounter virtual AddFood] Call RPC_AddFood instead.");
        // RPC_AddFood(food, foodPoint.position);

    }

    protected virtual void AddFood(NetworkObject food, Vector3 position)
    {
        GetComponent<AuthorityHandler>().RequestStateAuthority(
            onAuthorized: () =>
            {
                Debug.Log("[ACounter AddFood] Authorized");

                food.transform.SetParent(foodPoint, false);
                food.transform.SetPositionAndRotation(position, Quaternion.identity);

                food.GetComponent<Food>().RPC_SetDrop();
                foods.Add(food);
            },
            onNotAuthorized: () =>
            {
                Debug.Log("[ACounter AddFood] Not Authorized");
            }
        );
    }

    /// <summary>
    /// This function is used for removing food.
    /// The caller (typically Player.AddFood) is responsible for re-assigning the food's Holder via Food.RPC_SetHolder
    /// </summary>
    /// <returns>Food NetworkObject.</returns>
    protected NetworkObject RemoveFood()
    {
        if (foods.Count == 0) return null;

        // var temp = GetLastFood();
        // RPC_RemoveLastFood();
        // return temp;

        var fid = foods.Last();
        var food = Runner.FindObject(fid);
        GetComponent<AuthorityHandler>().RequestStateAuthority(
            onAuthorized: () =>
            {
                Debug.Log("[ACounter AddFood] Authorized");

                // food.transform.SetParent(foodPoint, false);
                // food.transform.SetPositionAndRotation(position, Quaternion.identity);

                food.GetComponent<Food>().RPC_SetDrop();
                foods.Remove(fid);
            },
            onNotAuthorized: () =>
            {
                Debug.Log("[ACounter AddFood] Not Authorized");
            }
        );
        return food;
    }

    protected NetworkObject GetLastFood()
    {
        return Runner.FindObject(foods.Last());
    }

    /// <summary>
    /// Append a food entry (id + world position) to the counter's networked lists,
    /// and assign this counter as the food's holder so Food.FixedUpdateNetwork
    /// positions it at foodPoint + LocalOffset every tick.
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_AddFood(NetworkId foodId, Vector3 position)
    {
        Debug.Log("[ACounter RPC_AddFood] Called.");
        foods.Add(foodId);
        foodPositions.Add(position);

        var foodNO = Runner.FindObject(foodId);
        Debug.Log("[ACounter RPC_AddFood] foodNO == null is " + (foodNO == null).ToString());
        if (foodNO == null) return;

        var food = foodNO.GetComponent<Food>();
        Debug.Log("[ACounter RPC_AddFood] food == null is " + (food == null).ToString());
        if (food == null) return;

        Debug.Log("[ACounter RPC_AddFood] Call Food RPC_SetHolder.");
        // food.RPC_SetHolder(Food.HolderKind.Counter, this, position - foodPoint.position);


    }

    /// <summary>
    /// Remove the last food entry (id + position) from the counter's networked lists.
    /// Holder is intentionally NOT cleared here; the caller (Player.AddFood) re-assigns it.
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RemoveLastFood()
    {
        if (foods.Count == 0) return;
        foods.Remove(foods.Last());
        foodPositions.Remove(foodPositions.Last());
        Assert.Check(foods.Count == foodPositions.Count);
        Debug.Log("[ACounter RPC_RemoveLastFood] Food count is now " + foods.Count);
    }

    /// <summary>
    /// Remove the all networked list (id + position) of the counter.
    /// Executed on the counter's State Authority; safe to call from any peer.
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_ClearFood()
    {
        if (foods.Count == 0) return;

        foreach (var food in foods)
        {
            foodSpawner.Despawn(food);
        }
        foods.Clear();
        foodPositions.Clear();
        Debug.Log("[ACounter RPC_ClearFood] Foods cleared");
    }

    /// <summary>
    /// This function removes all of the foods this counter have in the scene.
    /// </summary>
    protected void ClearFood()
    {
        foreach (var food in foods)
        {
            foodSpawner.Despawn(food);
        }
        foods.Clear();
        foodPositions.Clear();
    }

    // 기본적으로 Side 음식(사이드, 음료)는 놓을 수 없음
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
    public void RPC_SetParent(NetworkObject food, Vector3 pos)
    {
        var foodName = food != null ? food.name : "null";
        Debug.Log($"[Counter/{name}] RPC_SetParent received on local. food={foodName} pos={pos} foodPoint={foodPoint.position}");
        food.transform.SetParent(foodPoint, false);
        food.transform.SetLocalPositionAndRotation(pos, Quaternion.identity);
    }

    public virtual void OnAdded(NetworkObject food, Vector3 pos)
    {
        var foodName = food != null ? food.name : "null";
        Debug.Log($"[Counter/{name}] OnAdded called. food={foodName} pos={pos} HasFoodAuth={(food != null && food.HasStateAuthority)} HasCounterAuth={HasStateAuthority} foodsCount(before)={foods.Count}");

        // food.transform.SetParent(foodPoint, false);
        // food.transform.SetPositionAndRotation(pos, Quaternion.identity);
        RPC_SetParent(food, pos);

        food.GetComponent<Food>().SetDrop();
        foods.Add(food);

        Debug.Log($"[Counter/{name}] OnAdded done. foodsCount(after)={foods.Count}");
    }

    public virtual void OnRemoved(NetworkObject food)
    {
        var foodName = food != null ? food.name : "null";
        Debug.Log($"[Counter/{name}] OnRemoved called. food={foodName} HasFoodAuth={(food != null && food.HasStateAuthority)} HasCounterAuth={HasStateAuthority} foodsCount(before)={foods.Count}");

        food.GetComponent<Food>().SetDrop();
        foods.Remove(food);

        Debug.Log($"[Counter/{name}] OnRemoved done. foodsCount(after)={foods.Count}");
    }

    public virtual bool CanAdd(Food food)
    {
        var accept = food != null && AcceptsFood(food.Data);
        var ok = !HasFood() && accept;
        var foodDesc = food != null && food.Data != null ? food.Data.FoodName : "null";
        Debug.Log($"[Counter/{name}] CanAdd({foodDesc}) = {ok} (HasFood={HasFood()} AcceptsFood={accept})");
        return ok;
    }

    public virtual bool CanRemove()
    {
        var ok = HasFood();
        Debug.Log($"[Counter/{name}] CanRemove() = {ok} (foodsCount={foods.Count})");
        return ok;
    }
}
