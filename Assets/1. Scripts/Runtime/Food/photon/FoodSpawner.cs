using Fusion;
using UnityEngine;

public class FoodSpawner : SimulationBehaviour
{

    public NetworkObject SpawnFood(FoodSO foodSO, PlayerRef? inputAuthority = null)
    {
        return SpawnFood(foodSO, Vector3.zero, Quaternion.identity, inputAuthority);
    }
    public NetworkObject SpawnFood(FoodSO foodSO, Vector3 position, Quaternion rotation, PlayerRef? inputAuthority = null)
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

        if(Runner == null)
        {
            Debug.Log("[FoodSpawner SpawnFood] Runner is null.");
            return null;
        }
        // Debug.Log(Runner.CanSpawn + " runner can spawn");

        NetworkObject obj = Runner.Spawn(
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

    public void Despawn(NetworkId foodObjectId)
    {
        Runner.Despawn(Runner.FindObject(foodObjectId));
    }

    public void Despawn(NetworkObject foodObject)
    {
        Runner.Despawn(foodObject);
    }


    public bool CanSpawn()
    {
        return Runner != null || Runner.CanSpawn;
    }
}