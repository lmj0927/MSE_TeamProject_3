using System;
using Fusion;
using UnityEngine;

public abstract class FoodHolder : NetworkBehaviour
{
    public AuthorityHandler AuthorityHandler => GetComponent<AuthorityHandler>();

    public abstract bool CanAdd(Food food);
    public abstract bool CanRemove();

    protected abstract void OnAdded(NetworkObject food, Vector3 pos);
    protected abstract void OnRemoved(NetworkObject food);

    public abstract void ClearAll(Action onDone = null);

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
