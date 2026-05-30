// Owned by JunYoung Park
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using DG.Tweening;
using TMPro;
using UnityEngine.UIElements.Experimental;
using Fusion;

public class OrderManager : NetworkSingleton<OrderManager>
{
    [SerializeField] private GameObject orderUI;
    private OrderItem[] uiSlots;
    [SerializeField] private RadialProgressBar timerUI;
    [SerializeField] private TextMeshProUGUI point;
    [SerializeField] private GameManager starParent;

    private bool isPlaying = false;
    private List<Customer> orders = new List<Customer>();
    private HashSet<Customer> animatedCustomers = new HashSet<Customer>();

    public override void Spawned()
    {
        base.Spawned();
        uiSlots = orderUI.GetComponentsInChildren<OrderItem>(true);
        CloseOrder();
        timerUI.gameObject.SetActive(false);

        if(GameManager.Instance == null) GameManager.BindInitializer(GameManagerActionsSetup);
        else GameManagerActionsSetup();
    }

    private void GameManagerActionsSetup()
    {
        GameManager.Instance.OnStageStart += RPC_HandleStageStart;
        GameManager.Instance.OnStageEnd += RPC_HandleStageEnd;
        GameManager.Instance.OnPointUpdated += RPC_UpdatePoint;
    }

    public override void FixedUpdateNetwork()
    {
        if (!isPlaying) return;

        timerUI.SetProgress(GameManager.Instance.stageTimer / GameManager.Instance.StageT);
    }
    private void OnDestroy()
    {
        if (GameManager.Instance == null) return;

        GameManager.Instance.OnStageStart -= RPC_HandleStageStart;
        GameManager.Instance.OnStageEnd -= RPC_HandleStageEnd;
        GameManager.Instance.OnPointUpdated -= RPC_UpdatePoint;
    }

    public void AddOrder(Customer customer)
    {
        if (!HasStateAuthority) return;
        RPC_AddOrder(customer);
    }

    public void RemoveOrder(Customer customer)
    {
        if (!HasStateAuthority) return;
        RPC_RemoveOrder(customer);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_AddOrder(Customer customer)
    {
        if (!isPlaying || customer == null || orders.Contains(customer)) return;

        orders.Add(customer);
        SoundManager.Instance.Order();
        Resort();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_RemoveOrder(Customer customer)
    {
        if (customer == null) return;
        orders.Remove(customer);
        animatedCustomers.Remove(customer);

        if (isPlaying) Resort();
    }

    public void Resort()
    {
        var waitingOrders = orders
            .OrderBy(c => c.sitTimer)
            .Take(uiSlots.Length)
            .ToList();

        if (waitingOrders.Count == 0)
        {
            CloseOrder();
            return;
        }

        if (!orderUI.activeSelf) ShowOrder();

        for (int i = 0; i < uiSlots.Length; i++)
        {
            if (i < waitingOrders.Count)
            {
                Customer currentCustomer = waitingOrders[i];
                uiSlots[i].gameObject.SetActive(true);

                uiSlots[i].SetOrder(currentCustomer);

                if (!animatedCustomers.Contains(currentCustomer))
                {
                    animatedCustomers.Add(currentCustomer);

                    uiSlots[i].transform.DOKill(true);
                    uiSlots[i].transform.localScale = Vector3.one;
                    uiSlots[i].transform.DOPunchScale(new Vector3(0.1f, 0.1f, 0f), 0.3f, 5, 1);
                }
            }
            else
            {
                uiSlots[i].gameObject.SetActive(false);
            }
        }
    }
    public void ShowOrder() => orderUI.SetActive(true);
    public void CloseOrder() => orderUI.SetActive(false);

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_HandleStageStart()
    {
        CloseOrder();
        timerUI.gameObject.SetActive(true);

        timerUI.SetProgress(0);
        isPlaying = true;
    }
    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_HandleStageEnd()
    {
        isPlaying = false;
        CloseOrder();
        timerUI.gameObject.SetActive(false);
        orders.Clear();
        animatedCustomers.Clear();

        foreach (var slot in uiSlots)
        {
            slot.transform.DOKill(true);
            slot.transform.localScale = Vector3.one;
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_UpdatePoint(int p, int grade)
    {
        point.text = "Point: " + p;

       foreach (GameObject star in starParent.transform)
        {
            if (grade > 0)
            {
                star.SetActive(true);
                grade--;
            } else star.SetActive(false);
        }
    }
}