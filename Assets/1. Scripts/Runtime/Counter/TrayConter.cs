// Owned by JunYoung Park
using System.Linq;
using Fusion;
using UnityEngine;

public class TrayCounter : ACounter
{
    [Networked] private NetworkObject mainFood { get; set; }
    [Networked] private NetworkObject currentTray { get; set; }

    [SerializeField] private GameObject Tray;
    [SerializeField] private Transform[] subPoints;

    protected override bool AcceptsFood(FoodSO foodData)
    {
        return foodData.Type == FoodSO.FoodType.Main ||
               foodData.Type == FoodSO.FoodType.Side ||
               foodData.Type == FoodSO.FoodType.Beverage;
    }

    public override void Interact(PlayerController player)
    {
        if(!player.HasStateAuthority) return;
        AuthorityHandler.RequestStateAuthority(
            onAuthorized: () =>
            {
                if (player.HasFood())
                {
                    var type = player.HeldFood.Data.Type;

                    if (type == FoodSO.FoodType.Main && mainFood != null) return;

                    if (type == FoodSO.FoodType.Main || type == FoodSO.FoodType.Side || type == FoodSO.FoodType.Beverage)
                    {
                        // AddFood(player.HeldFoodObject);
                        // FoodTransfer.Transfer(player, this, player.HeldFoodObject, Vector3.zero);

                        var food = player.HeldFoodObject;

                        var foodDataType = food.GetComponent<Food>().Data.Type;
                        if (foodDataType == FoodSO.FoodType.Main)
                        {
                            mainFood = food;
                            food.transform.position = foodPoint.position;
                            // food.transform.position = foodPoint.position;
                            // foodPositions.Add(foodPoint.position);
                            // RPC_AddFood(food, foodPoint.position);
                            FoodTransfer.Transfer(player, this, player.HeldFoodObject, foodPoint.position);
                        }
                        else if (foodDataType == FoodSO.FoodType.Side)
                        {
                            // food.transform.position = subPoints[0].position;
                            // foodPositions.Add(subPoints[0].position);
                            // RPC_AddFood(food, subPoints[0].position);
                            FoodTransfer.Transfer(player, this, player.HeldFoodObject, subPoints[0].position-foodPoint.position);
                        }
                        else if (foodDataType == FoodSO.FoodType.Beverage)
                        {
                            // food.transform.position = subPoints[1].position;
                            // foodPositions.Add(subPoints[1].position);
                            // RPC_AddFood(food, subPoints[1].position);
                            FoodTransfer.Transfer(player, this, player.HeldFoodObject, subPoints[1].position-foodPoint.position);
                        }
                    }
                }
                else // player has no food
                {
                    if (mainFood != null)
                    {
                        Transform traytransform = mainFood.transform.Find("Tray_Root");

                        if (traytransform == null)
                        {
                            currentTray = Runner.Spawn(Tray);
                            RPC_SetTrayParent();
                        }
                        else currentTray = traytransform.GetComponent<NetworkObject>();

                        CombineAllToMain();

                        // player.AddFood(mainFood);
                        FoodTransfer.Transfer(this, player, mainFood, Vector3.zero);

                        mainFood = null;
                        currentTray = null;
                    }
                    else if (HasFood())
                    {
                        // player.AddFood(RemoveFood());
                        FoodTransfer.Transfer(this, player, GetLastFood(), Vector3.zero);
                    }
                }
                
            },
            onNotAuthorized: () =>
            {
                
            }
        );
    }


    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_SetTrayParent()
    {
        Debug.Log($"[Counter/{name}] RPC_SetTrayParent");
        currentTray.name = "Tray_Root";
        currentTray.transform.SetParent(mainFood.transform, true);
        currentTray.transform.localPosition = Vector3.zero;
    }

    private void CombineAllToMain()
    {
        if (mainFood == null || currentTray == null) return;

        foreach (var foodNO in foods.Select(food => Runner.FindObject(food)))
        {
            foodNO.GetComponent<AuthorityHandler>().RequestStateAuthority(
                onAuthorized: () =>
                {
                    foodNO.transform.SetParent(currentTray.transform, true);
                    foods.Remove(foodNO);
                },
                onNotAuthorized: () =>
                {
                    Debug.LogWarning("[TrayCounter CombineAllToMain] denied.");
                }
            );
        }
        // foods.Clear();
        // RPC_ClearFood();
        // OnClear();
    }



    public override bool CanAdd(Food food)
    {
        var accept = food != null && AcceptsFood(food.Data);
        var ok = accept;
        var foodDesc = food != null && food.Data != null ? food.Data.FoodName : "null";
        Debug.Log($"[Counter/{name}] CanAdd({foodDesc}) = {ok} (HasFood={HasFood()} AcceptsFood={accept})");
        return ok;
    }

    // protected override void AddFood(NetworkObject food)
    // {
    //     // foods.Add(food);

    //     var foodDataType = food.GetComponent<Food>().Data.Type;
    //     if (foodDataType == FoodSO.FoodType.Main)
    //     {
    //         mainFood = food;
    //         food.transform.position = foodPoint.position;
    //         // food.transform.position = foodPoint.position;
    //         // foodPositions.Add(foodPoint.position);
    //         RPC_AddFood(food, foodPoint.position);
    //     }
    //     else if (foodDataType == FoodSO.FoodType.Side)
    //     {
    //         // food.transform.position = subPoints[0].position;
    //         // foodPositions.Add(subPoints[0].position);
    //         RPC_AddFood(food, subPoints[0].position);
    //     }
    //     else if (foodDataType == FoodSO.FoodType.Beverage)
    //     {
    //         // food.transform.position = subPoints[1].position;
    //         // foodPositions.Add(subPoints[1].position);
    //         RPC_AddFood(food, subPoints[1].position);
    //     }

    //     food.transform.rotation = Quaternion.identity;
    // }
}