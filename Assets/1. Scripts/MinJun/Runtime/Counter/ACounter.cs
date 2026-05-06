using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public abstract class ACounter : MonoBehaviour, IInteractable
{
    [SerializeField] protected Transform foodPoint;
    protected List<Food> foods = new List<Food>();

    public virtual void Interact(PlayerController player) { }

    protected bool HasFood()
    {
        return foods.Count > 0;
    }

    protected List<FoodSO> GetFoodSOs()
    {
        return foods.Select(food => food.Data).ToList();
    }

    protected void AddFood(Food food)
    {
        foods.Add(food);
        food.transform.position = foodPoint.position;
    }

    protected Food RemoveFood()
    {
        if (foods.Count == 0) return null;

        var temp = foods.Last();
        foods.Remove(temp);
        return temp;
    }

    protected void ClearFood()
    {
        foreach (var food in foods)
        {
            Destroy(food.gameObject);
        }
        foods.Clear();
    }

    // 플레이어가 음식을 들고 있고 카운터에 음식이 없을 때
    protected bool CanAddFood(PlayerController player)
    {
        return player.HasFood() && !HasFood();
    }

    // 플레이어가 음식을 들고 있지 않고 카운터에 음식이 있을 때
    protected bool CanRemoveFood(PlayerController player)
    {
        return !player.HasFood() && HasFood();
    }
}
