// Owned by MinJun Lee
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Fusion;

public abstract class ACounter : NetworkBehaviour, IInteractable
{
    [SerializeField] protected Transform foodPoint;
    [SerializeField] protected FoodSpawner foodSpawner;
    protected List<Food> foods = new List<Food>();

    public override void Spawned()
    {
        
        foodSpawner = NetworkRunner.GetRunnerForGameObject(gameObject).GetComponent<FoodSpawner>();
        if(foodSpawner == null)
        {
            Debug.LogError("[ACounter Spawned] foodSpawner is null.");
        }
        else if(!foodSpawner.CanSpawn())
        {
            Debug.LogError("[ACounter Spawned] foodSpawner cannot spawn since it is null or Runner.CanSpawn is false.");
        }
        else
        {
            Debug.Log("[ACounter Spawned] foodSpawner found.");
        }
    }

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
}
