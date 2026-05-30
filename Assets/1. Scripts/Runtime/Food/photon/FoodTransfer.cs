// owned by Yongkyu Lee
using Fusion;
using UnityEngine;

public static class FoodTransfer
{
    /// <summary>
    /// Suppose that the authority of both `from` and `to` is already received.
    /// Transfer a food from `from` to `to`. 
    /// </summary>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <param name="food"></param>
    /// <param name="pos"></param>
    public static void Transfer(IFoodHolder from, IFoodHolder to, NetworkObject food, Vector3 pos)
    {
        var fromMb = from as MonoBehaviour;
        var toMb = to as MonoBehaviour;
        var fromName = fromMb != null ? fromMb.name : (from != null ? from.GetType().Name : "null");
        var toName = toMb != null ? toMb.name : (to != null ? to.GetType().Name : "null");
        var foodName = food != null ? food.name : "null";
        Debug.Log($"[Transfer] Enter from={fromName} to={toName} food={foodName} pos={pos}");

        if(food == null)
        {
            Debug.LogWarning($"[Transfer] ABORT — food is null.");
            return;
        }
        if(!from.CanRemove())
        {
            Debug.LogWarning($"[Transfer] ABORT — {fromName}.CanRemove()=false.");
            return;
        }
        if(!to.CanAdd(food.GetComponent<Food>()))
        {
            Debug.LogWarning($"[Transfer] ABORT — {toName}.CanAdd(food)=false.");
            return;
        }

        Debug.Log($"[Transfer] Guards passed. Requesting food authority on {foodName}.");
        food.GetComponent<AuthorityHandler>().RequestStateAuthority(
            onAuthorized: () =>
            {
                Debug.Log($"[Transfer] Food authority granted. Calling {fromName}.OnRemoved + {toName}.OnAdded.");
                from.OnRemoved(food);
                to.OnAdded(food, pos);
                Debug.Log($"[Transfer] Complete: {fromName} → {toName} ({foodName}).");
            },
            onNotAuthorized: () =>
            {
                Debug.LogWarning($"[Transfer] DENIED — food authority not granted ({foodName}). Transfer aborted; state may be partially inconsistent.");
            }
        );
    }
}