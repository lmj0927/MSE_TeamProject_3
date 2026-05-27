// owned by Yongkyu Lee
using Fusion;
using UnityEngine;

public static class FoodSpawner
{

    public static NetworkObject SpawnFood(NetworkRunner runner, FoodSO foodSO, PlayerRef? inputAuthority = null)
    {
        return SpawnFood(runner, foodSO, Vector3.zero, Quaternion.identity, inputAuthority);
    }
    public static NetworkObject SpawnFood(NetworkRunner runner, FoodSO foodSO, Vector3 position, Quaternion rotation, PlayerRef? inputAuthority = null)
    {
        if (foodSO == null)
        {
            Debug.LogError("FoodSO is null.");
            return null;
        }

        if (foodSO.Prefab == null)
        {
            Debug.LogError($"{foodSO.FoodName} prefab is null.");
            return null;
        }

        // if (!Runner.IsServer)
        // {
        //     Debug.LogWarning("Only server/host should spawn food in Host mode.");
        //     return null;
        // }

        if(runner == null)
        {
            Debug.Log("[FoodSpawner SpawnFood] Runner is null.");
            return null;
        }
        // Debug.Log(Runner.CanSpawn + " runner can spawn");

        NetworkObject obj = runner.Spawn(
            foodSO.Prefab,
            position,
            rotation,
            inputAuthority
        );

        // Food food = obj.GetComponent<Food>();

        // if (food == null)
        // {
        //     Debug.LogError("[FoodSpawner SpawnFood] The FoodSO has a prefab of NetworkObject without a Food component. Please try again after add it.");
        // }

        return obj;
    }

    public static void Despawn(NetworkRunner runner, NetworkId foodObjectId)
    {
        // if(foodObjectId)
        runner.Despawn(runner.FindObject(foodObjectId));
    }

    // public static void Despawn(NetworkObject foodObject)
    // {
    //     Runner.Despawn(foodObject);
    // }


    public static bool CanSpawn(NetworkRunner runner)
    {
        return runner != null || runner.CanSpawn;
    }
}