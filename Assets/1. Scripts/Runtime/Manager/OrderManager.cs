// Owned by JunYoung Park
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using DG.Tweening;

public class OrderManager : Singleton<OrderManager>
{
    [SerializeField] private GameObject orderUI;
    [SerializeField] private OrderItem[] uiSlots;

    private List<Customer> orders = new List<Customer>();
    private HashSet<Customer> animatedCustomers = new HashSet<Customer>();

    private void Awake()
    {
        orderUI = transform.GetChild(0).gameObject;
        uiSlots = orderUI.GetComponentsInChildren<OrderItem>(true);
    }

    private void Start()
    {
        CloseUI();
    }

    public void AddOrder(Customer customer)
    {
        if (orders.Contains(customer)) return;

        orders.Add(customer);
        SoundManager.Instance.Order();
        Resort();
    }

    public void RemoveOrder(Customer customer)
    {
        orders.Remove(customer);

        if (animatedCustomers.Contains(customer))
        {
            animatedCustomers.Remove(customer);
        }

        Resort();
    }

    public void Resort()
    {
        var waitingOrders = orders
            .OrderBy(c => c.sitTimer)
            .Take(uiSlots.Length)
            .ToList();

        if (waitingOrders.Count == 0)
        {
            CloseUI();
            return;
        }

        if (!orderUI.activeSelf) ShowUI();

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
    public void ShowUI() => orderUI.SetActive(true);
    public void CloseUI() => orderUI.SetActive(false);
}