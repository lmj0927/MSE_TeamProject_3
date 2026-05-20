// Owned by YongKyu Lee
using Fusion;
using UnityEngine;
using UnityEngine.AdaptivePerformance;

public class Food : NetworkBehaviour
{
    [SerializeField] private FoodSO data;

    public FoodSO Data => data;
    public void SetData(FoodSO data)
    {
        this.data = data;
    }
}
