// Owned by MinJun Lee
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Fusion;

public abstract class ACounter : NetworkBehaviour, IInteractable
{
    [SerializeField] protected Transform foodPoint;

    /// <summary>
    /// Food spawner/despawner for networked food object.
    /// </summary>
    [SerializeField] protected FoodSpawner foodSpawner;
    
    /// <summary>
    /// List of the network object ids for each of the food object.
    /// </summary>
    [Networked, Capacity(16)] protected NetworkLinkedList<NetworkId> foods { get; }

    /// <summary>
    /// List of the positions of each foods.
    /// </summary>
    [Networked, Capacity(16)] protected NetworkLinkedList<Vector3> foodPositions { get; }

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
            // Debug.Log("[ACounter Spawned] foodSpawner found.");
        }
    }

    public virtual void Interact(PlayerController player) { }

    protected bool HasFood()
    {
        return foods.Count > 0;
    }

    protected List<FoodSO> GetFoodSOs()
    {
        return foods.Select(f => Runner.FindObject(f).GetComponent<Food>().Data).ToList();
    }

    protected virtual void AddFood(NetworkObject food)
    {
        foods.Add(food);
        // food.transform.position = foodPoint.position;
        foodPositions.Add(foodPoint.position);
    }

    /// <summary>
    /// This function is used for removing food.
    /// No restoring for rigidbody of food.
    /// </summary>
    /// <returns>Food NetworkObject without rigidbody.</returns>
    protected NetworkObject RemoveFood()
    {
        if (foods.Count == 0) return null;

        var temp = foods.Last();
        foods.Remove(temp);
        foodPositions.Remove(foodPositions.Last());
        return Runner.FindObject(temp);
    }

    /// <summary>
    /// This function is used for the player re-picking a food.
    /// Restore the rigidbody of food.
    /// </summary>
    /// <returns>Food NetworkObject with rigidbody.</returns>
    protected NetworkObject RemoveFoodAndRestoreRigidBody()
    {
        NetworkObject foodNO = RemoveFood();
        if(foodNO == null) return null;

        foreach (var rb in foodNO.GetComponentsInChildren<Rigidbody>())
            rb.isKinematic = false;

        foreach (var col in foodNO.GetComponentsInChildren<Collider>())
            col.enabled = true;

        return foodNO;
    }

    /// <summary>
    /// This function removes all of the foods this counter have in the scene.
    /// </summary>
    protected void ClearFood()
    {
        foreach (var food in foods)
        {
            foodSpawner.Despawn(food);
        }
        foods.Clear();
        foodPositions.Clear();
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

    public override void FixedUpdateNetwork()
    {
        for(int i = 0; i < foods.Count; ++i)
        {
            NetworkObject foodNO = Runner.FindObject(foods[i]);
            foreach(Rigidbody rb in foodNO.GetComponentsInChildren<Rigidbody>())
            {
                if(!rb.isKinematic)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
                rb.isKinematic = true;
            }

            foreach (var col in foodNO.GetComponentsInChildren<Collider>())
                col.enabled = false;

            foodNO.transform.position = foodPositions[i];
        }
    }
}
