// Owned by YongKyu Lee
using System;
using Fusion;
using UnityEngine;

public abstract class FoodHolder : NetworkBehaviour
{
    /// <summary>
    /// It assume that the object to which this is attached is also have `AuthorityHandler` component.
    /// </summary>
    public AuthorityHandler AuthorityHandler => GetComponent<AuthorityHandler>();

    // some abstract methods below.
    public abstract bool CanAdd(Food food);
    public abstract bool CanRemove();

    protected abstract void OnAdded(NetworkObject food, Vector3 pos);
    protected abstract void OnRemoved(NetworkObject food);

    public abstract void ClearAll(Action onDone = null);

    /// <summary>
    /// Place the object from `FoodSO`
    /// </summary>
    /// <param name="foodSO">what food you want to spawn?</param>
    /// <param name="pos">spawn position</param>
    /// <param name="onDone">callback after the creation</param>
    public void Place(FoodSO foodSO, Vector3 pos, Action<NetworkObject> onDone = null)
    {
        if (foodSO == null)
        {
            Debug.LogWarning($"[FoodHolder/{name}] Place aborted - foodSO is null.");
            onDone?.Invoke(null);
            return;
        }
        if (foodSO.Prefab == null)
        {
            Debug.LogError($"[FoodHolder/{name}] Place aborted - {foodSO.FoodName} prefab is null.");
            onDone?.Invoke(null);
            return;
        }
        if (Runner == null)
        {
            Debug.LogError($"[FoodHolder/{name}] Place aborted - Runner is null.");
            onDone?.Invoke(null);
            return;
        }

        NetworkObject food = Runner.Spawn(foodSO.Prefab);
        if (food == null)
        {
            Debug.LogError($"[FoodHolder/{name}] Place aborted - spawn returned null.");
            onDone?.Invoke(null);
            return;
        }

        OnAdded(food, pos);
        onDone?.Invoke(food);
    }

    /// <summary>
    /// Discard the `food`
    /// </summary>
    /// <param name="food">food you want to despawn</param>
    /// <param name="onDone">Callback after despawning</param>
    public void Discard(NetworkObject food, Action onDone = null)
    {
        if (food == null)
        {
            onDone?.Invoke();
            return;
        }

        food.GetComponent<AuthorityHandler>().RequestStateAuthority(
            onAuthorized: () =>
            {
                OnRemoved(food);
                Runner.Despawn(food);
                onDone?.Invoke();
            },
            onNotAuthorized: () =>
            {
                Debug.LogWarning($"[FoodHolder/{name}] Discard denied for {food.name}.");
                onDone?.Invoke();
            }
        );
    }

    /// <summary>
    /// Handoff one food from me to `to` holder.
    /// </summary>
    /// <param name="to">Other foodholder</param>
    /// <param name="food">food network object I have</param>
    /// <param name="pos">new position</param>
    /// <param name="onDone">Callback after hand-off</param>
    public void HandoffTo(FoodHolder to, NetworkObject food, Vector3 pos, Action onDone = null)
    {
        if (food == null)
        {
            Debug.LogWarning($"[FoodHolder/{name}] HandoffTo aborted - food is null.");
            onDone?.Invoke();
            return;
        }
        if (to == null)
        {
            Debug.LogWarning($"[FoodHolder/{name}] HandoffTo aborted - target is null.");
            onDone?.Invoke();
            return;
        }
        if (!CanRemove())
        {
            Debug.LogWarning($"[FoodHolder/{name}] HandoffTo aborted - CanRemove false.");
            onDone?.Invoke();
            return;
        }
        if (!to.CanAdd(food.GetComponent<Food>()))
        {
            Debug.LogWarning($"[FoodHolder/{name}] HandoffTo aborted - target CanAdd false.");
            onDone?.Invoke();
            return;
        }

        food.GetComponent<AuthorityHandler>().RequestStateAuthority(
            onAuthorized: () =>
            {
                OnRemoved(food);
                to.OnAdded(food, pos);
                onDone?.Invoke();
            },
            onNotAuthorized: () =>
            {
                Debug.LogWarning($"[FoodHolder/{name}] HandoffTo denied for {food.name}.");
                onDone?.Invoke();
            }
        );
    }

    /// <summary>
    /// Replace `old` food I have to new food from `newSO`
    /// </summary>
    /// <param name="old">despawn this</param>
    /// <param name="newSO">spawn this</param>
    /// <param name="pos">position</param>
    /// <param name="onDone">Callback after replacement</param>
    public void Replace(NetworkObject old, FoodSO newSO, Vector3 pos, Action<NetworkObject> onDone = null)
    {
        if (old == null)
        {
            Debug.LogWarning($"[FoodHolder/{name}] Replace aborted - old is null.");
            onDone?.Invoke(null);
            return;
        }
        if (newSO == null)
        {
            Debug.LogWarning($"[FoodHolder/{name}] Replace aborted - newSO is null.");
            onDone?.Invoke(null);
            return;
        }
        if (newSO.Prefab == null)
        {
            Debug.LogError($"[FoodHolder/{name}] Replace aborted - {newSO.FoodName} prefab is null.");
            onDone?.Invoke(null);
            return;
        }

        old.GetComponent<AuthorityHandler>().RequestStateAuthority(
            onAuthorized: () =>
            {
                OnRemoved(old);
                Runner.Despawn(old);
                NetworkObject newFood = Runner.Spawn(newSO.Prefab);
                if (newFood == null)
                {
                    Debug.LogError($"[FoodHolder/{name}] Replace failed - spawn returned null.");
                    onDone?.Invoke(null);
                    return;
                }
                OnAdded(newFood, pos);
                onDone?.Invoke(newFood);
            },
            onNotAuthorized: () =>
            {
                Debug.LogWarning($"[FoodHolder/{name}] Replace denied for {old.name}.");
                onDone?.Invoke(null);
            }
        );
    }
}
