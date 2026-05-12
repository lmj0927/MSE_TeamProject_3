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

    protected virtual void AddFood(Food food)
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

    // 기본적으로 Side 음식(사이드, 음료)는 놓을 수 없음
    protected virtual bool AcceptsFood(FoodSO foodData)
    {
        if (foodData.Type == FoodSO.FoodType.Side) return false;

        return true;
    }

    // 플레이어가 음식을 들고 있고 카운터에 음식이 없을 때 + 놓을 수 있는 음식일 때
    protected bool CanAddFood(PlayerController player)
    {
        return player.HasFood() && !HasFood() && AcceptsFood(player.HeldFood.data);
    }

    // 플레이어가 음식을 들고 있지 않고 카운터에 음식이 있을 때
    protected bool CanRemoveFood(PlayerController player)
    {
        return !player.HasFood() && HasFood();
    }
}
