using System.Collections.Generic;
using minjun;
using UnityEngine;

public abstract class ACounter : MonoBehaviour, IInteractable
{
    [SerializeField] protected Transform foodPoint;
    protected List<Food> foods = new List<Food>();

    public virtual void Interact(Player player) { }

    protected bool HasFood()
    {
        return foods.Count > 0;
    }

    protected void AddFood(Food food)
    {
        foods.Add(food);
        food.transform.position = foodPoint.position;
    }

    protected Food RemoveFood()
    {
        var temp = foods[0];
        foods.RemoveAt(0);
        return temp;
    }

    // 플레이어가 음식을 들고 있고 카운터에 음식이 없을 때
    protected bool CanAddFood(Player player)
    {
        return player.HasFood() && !HasFood();
    }

    // 플레이어가 음식을 들고 있지 않고 카운터에 음식이 있을 때
    protected bool CanRemoveFood(Player player)
    {
        return !player.HasFood() && HasFood();
    }
}
