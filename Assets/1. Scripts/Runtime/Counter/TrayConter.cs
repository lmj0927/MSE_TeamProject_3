// Owned by JunYoung Park
using Fusion;
using UnityEngine;

public class TrayCounter : ACounter
{
    private NetworkObject mainFood;
    [SerializeField]
    private Transform currentTray;

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
        if (player.HasFood())
        {
            var type = player.HeldFood.Data.Type;

            if (type == FoodSO.FoodType.Main && mainFood != null) return;

            if (type == FoodSO.FoodType.Main)
            {
                mainFood = player.RemoveFoodAndRestoreRigidbody();
                mainFood.transform.position = foodPoint.position;

                currentTray = Instantiate(Tray).transform;
                currentTray.SetParent(mainFood.transform, true);
                currentTray.localPosition = Vector3.zero;

                CombineAllToMain();
            }
            else if (type == FoodSO.FoodType.Side || type == FoodSO.FoodType.Beverage)
            {
                if (mainFood != null)
                {
                    var food = player.RemoveFoodAndRestoreRigidbody();
                    
                    food.transform.SetParent(currentTray, true);
                    AddFood(food);
                }
                else
                {
                    AddFood(player.RemoveFood());
                }
            }
        }
        else
        {
            if (mainFood != null)
            {
                player.AddFood(mainFood);

                mainFood = null;
                currentTray = null;
            }
            else if (HasFood())
            {
                player.AddFood(RemoveFood());
            }
        }
    }

    private void CombineAllToMain()
    {
        if (mainFood == null || currentTray == null) return;

        foreach (var food in foods)
        {
            Runner.FindObject(food).transform.SetParent(currentTray, true);
        }
        foods.Clear();
    }

    protected override void AddFood(NetworkObject food)
    {
        foods.Add(food);

        var foodDataType = food.GetComponent<Food>().Data.Type;
        if (foodDataType == FoodSO.FoodType.Main)
        {
            // food.transform.position = foodPoint.position;
            foodPositions.Add(foodPoint.position);
        }
        else if (foodDataType == FoodSO.FoodType.Side)
        {
            // food.transform.position = subPoints[0].position;
            foodPositions.Add(subPoints[0].position);
        }
        else if (foodDataType == FoodSO.FoodType.Beverage)
        {
            // food.transform.position = subPoints[1].position;
            foodPositions.Add(subPoints[1].position);
        }
    }
}