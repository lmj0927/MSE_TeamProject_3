// Owned by JunYoung Park
using UnityEngine;

public class TrayCounter : ACounter
{
    private Food mainFood;
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
                mainFood = player.RemoveFood();
                mainFood.transform.position = foodPoint.position;
                mainFood.transform.rotation = Quaternion.identity;

            }
            else if (type == FoodSO.FoodType.Side || type == FoodSO.FoodType.Beverage)
            {
                AddFood(player.RemoveFood());
            }
        }
        else 
        {
            if (mainFood != null)
            {
                currentTray = mainFood.transform.Find("Tray_Root");

                if (currentTray == null)
                {
                    currentTray = Instantiate(Tray).transform;
                    currentTray.name = "Tray_Root";
                    currentTray.SetParent(mainFood.transform, true);
                    currentTray.localPosition = Vector3.zero;
                }

                CombineAllToMain();

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
            food.transform.SetParent(currentTray, true);
        }
        foods.Clear();
    }

    protected override void AddFood(Food food)
    {
        foods.Add(food);

        if (food.Data.Type == FoodSO.FoodType.Main)
        {
            food.transform.position = foodPoint.position;
        }
        else if (food.Data.Type == FoodSO.FoodType.Side)
        {
            food.transform.position = subPoints[0].position;
        }
        else if (food.Data.Type == FoodSO.FoodType.Beverage)
        {
            food.transform.position = subPoints[1].position;
        }
    }
}