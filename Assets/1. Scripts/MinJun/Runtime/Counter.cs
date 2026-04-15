using System.Collections.Generic;
using minjun;
using UnityEngine;

public abstract class Counter : MonoBehaviour, IInteractable
{
    protected List<Food> foods;
    
    public virtual void Interact(Player player) {}

    protected bool HasFood()
    {
        return foods.Count > 0;
    }

    protected void AddFood(Food food)
    {
        foods.Add(food);
        // TODO 배치까지
    }

    protected Food RemoveFood()
    {
        var temp = foods[0];
        foods.RemoveAt(0);
        return temp;
    }
}
