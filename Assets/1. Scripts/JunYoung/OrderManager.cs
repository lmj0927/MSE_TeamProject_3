using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class OrderManager : Singleton<OrderManager>
{
    [SerializeField] private GameObject orderUI;
    [SerializeField] private OrderItem[] uiSlots;

    private List<(Customer c, RecipeSO r)> orders = new List<(Customer c, RecipeSO r)>();

    private void Awake()
    {
        orderUI = transform.GetChild(0).gameObject;
        uiSlots = orderUI.GetComponentsInChildren<OrderItem>(true);
    }

    private void Start()
    {
        CloseUI();
    }
    public void AddOrder(Customer customer, RecipeSO order)
    {
        orders.Add((customer, order));
    }

    public void RemoveOrder(Customer customer)
    {
        orders.RemoveAll(o => o.c == customer);
        Resort();
    }

    public void Resort()
    {
        var waitingOrders = orders
            .OrderBy(o => o.c.SitTimer) 
            .Take(uiSlots.Length) 
            .ToList();

        if (waitingOrders.Count == 0)
        {
            CloseUI();
            return;
        }

        if (!orderUI.activeSelf) ShowUI();

        SoundManager.Instance.Order();
        
        for (int i = 0; i < uiSlots.Length; i++)
        {
            if (i < waitingOrders.Count)
            {
                uiSlots[i].gameObject.SetActive(true);
                uiSlots[i].SetOrder(waitingOrders[i].c.Portrait, waitingOrders[i].r);
            }
            else
            {
                uiSlots[i].gameObject.SetActive(false);
            }
        }
    }

    public void ShowUI() => orderUI.SetActive(true);
    public void CloseUI() => orderUI.SetActive(false);
}