// owned by Yongkyu Lee
using Fusion;
using UnityEngine;

public interface IFoodHolder
{
    AuthorityHandler AuthorityHandler { get; }
    bool CanAdd(Food food);
    bool CanRemove();
    void OnAdded(NetworkObject food, Vector3 pos);
    void OnRemoved(NetworkObject food);

    void OnClear();
}