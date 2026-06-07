// Owned by JunYoung Park
using System;
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

                    Vector3 offset;
                    if (type == FoodSO.FoodType.Main) offset = Vector3.zero;
                    else if (type == FoodSO.FoodType.Side) offset = subPoints[0].position - foodPoint.position;
                    else if (type == FoodSO.FoodType.Beverage) offset = subPoints[1].position - foodPoint.position;
                    else return;

                    player.HandoffTo(this, player.HeldFoodObject, offset);
                }
                else // player has no food
                {
                    if (mainFood != null)
                    {
                        Transform traytransform = mainFood.transform.Find("Tray_Root");

                        if (traytransform == null)
                        {
                            currentTray = Runner.Spawn(Tray);
                            RPC_SetTrayParent(currentTray, mainFood);
                        }
                        else currentTray = traytransform.GetComponent<NetworkObject>();

                        CombineAllToMain(() =>
                        {
                            HandoffTo(player, mainFood, Vector3.zero, () =>
                            {
                                currentTray = null;
                            });
                        });
                    }
                    else if (HasFood())
                    {
                        HandoffTo(player, GetLastFood(), Vector3.zero);
                    }
                }
            },
            onNotAuthorized: () =>
            {

            }
        );
    }

    protected override void OnAdded(NetworkObject food, Vector3 offset)
    {
        if (food == null) return;
        var data = food.GetComponent<Food>().Data;
        if (data.Type == FoodSO.FoodType.Main) mainFood = food;
        base.OnAdded(food, offset);
    }

    protected override void OnRemoved(NetworkObject food)
    {
        if (food != null && food == mainFood) mainFood = null;
        base.OnRemoved(food);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_SetTrayParent(NetworkObject tray, NetworkObject main)
    {
        Debug.Log($"[Counter/{name}] RPC_SetTrayParent");
        if (tray == null || main == null) return;
        tray.name = "Tray_Root";
        tray.transform.SetParent(main.transform, true);
        NetworkTransform cnt = tray.GetComponent<NetworkTransform>();
        if(cnt == null) return;
        cnt.Teleport(main.transform.position, main.transform.rotation);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_AttachToTray(NetworkObject food, NetworkObject tray)
    {
        if (food == null || tray == null) return;
        food.transform.SetParent(tray.transform, true);
        NetworkTransform fnt = food.GetComponent<NetworkTransform>();
        if(fnt == null) return;
        fnt.Teleport(food.transform.position, food.transform.rotation);
    }

    private void CombineAllToMain(Action onDone)
    {
        if (mainFood == null || currentTray == null)
        {
            onDone?.Invoke();
            return;
        }

        var snapshot = foods
            .Select(fid => Runner.FindObject(fid))
            .Where(f => f != null && f != mainFood)
            .ToList();

        if (snapshot.Count == 0)
        {
            onDone?.Invoke();
            return;
        }

        int remaining = snapshot.Count;
        foreach (var foodNO in snapshot)
        {
            foodNO.GetComponent<AuthorityHandler>().RequestStateAuthority(
                onAuthorized: () =>
                {
                    RPC_AttachToTray(foodNO, currentTray);
                    foods.Remove(foodNO);
                    remaining--;
                    if (remaining == 0) onDone?.Invoke();
                },
                onNotAuthorized: () =>
                {
                    Debug.LogWarning("[TrayCounter CombineAllToMain] denied.");
                    remaining--;
                    if (remaining == 0) onDone?.Invoke();
                }
            );
        }
    }

    public override bool CanAdd(Food food)
    {
        var accept = food != null && AcceptsFood(food.Data);
        var ok = accept;
        var foodDesc = food != null && food.Data != null ? food.Data.FoodName : "null";
        Debug.Log($"[Counter/{name}] CanAdd({foodDesc}) = {ok} (HasFood={HasFood()} AcceptsFood={accept})");
        return ok;
    }
}
