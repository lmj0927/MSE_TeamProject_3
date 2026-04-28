using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CustomerManager : MonoBehaviour
{
    [SerializeField]
    private GameObject customer;

    [Tooltip("patience tiemr range")]
    [SerializeField] private Vector2 seatTimerRange = new Vector2(45f, 75f);

    [Tooltip("meal timer range")]
    [SerializeField] private Vector2 mealTimerRange = new Vector2(8f, 15f);

    [Tooltip("walk speed range")]
    [SerializeField] private Vector2 walkSpeedRange = new Vector2(2.5f, 3.5f);

    [SerializeField]
    private Transform outsideParent;

    private Queue<Customer> pool = new Queue<Customer>();

    [SerializeField]
    private Transform[] waitingPoint;
    private bool[] kioskState;      // Each kiosk's use state
    private Customer[] kCustomers;

    [SerializeField]
    private Transform chairParent;
    private Transform[] chairs;
    private bool[] useState;    // Each chair's use state
    private Customer[] customers;

    [SerializeField]
    private Transform trashBin;


    private float spawnTimer = 0;
    private float spawnTerm = 3.0f;
    private void Awake()
    {
        kioskState = new bool[waitingPoint.Length];
        kCustomers = new Customer[waitingPoint.Length];

        chairs = new Transform[chairParent.transform.childCount];

        for (int i = 0; i < chairs.Length; i++)
        {
            chairs[i] = chairParent.GetChild(i);
        }
        useState = new bool[chairs.Length];
        customers = new Customer[chairs.Length];
    }

    void Update()
    {
        int emptyK = GetEmptyKiosk();

        if (emptyK != -1)
        {
            spawnTimer -= Time.deltaTime;

            if (spawnTimer <= 0)
            {
                spawnTimer = spawnTerm;
                Customer c = GetCustomer();

                kioskState[emptyK] = true;
                kCustomers[emptyK] = c;

                c.setPath(Customer.cState.Entering, waitingPoint[emptyK]);
            }
        }

        for (int i = 0; i < waitingPoint.Length; i++)
        {
            if (kioskState[i] && kCustomers[i] != null)
            {
                Customer c = kCustomers[i];

                if (c.IsReady())
                {
                    int emptyC = GetEmptyChair();

                    if (emptyC != -1)
                    {
                        useState[emptyC] = true;
                        customers[emptyC] = kCustomers[i];

                        c.AssignSeat(emptyC);
                        c.OnMealFinished += HandleGetout;
                        c.setPath(Customer.cState.GoingSeat, chairs[emptyC]);

                        kioskState[i] = false;
                        kCustomers[i] = null;
                    }
                    
                }
            }
        }
    }

    private Transform GetOutside()
    {
        int ran = UnityEngine.Random.Range(0, outsideParent.childCount);

        return outsideParent.GetChild(ran);
        
    }
    private Customer GetCustomer()
    {

        Customer c;

        Transform outside = GetOutside();

        if (pool.Count == 0)
        {
            c = Instantiate(customer, outside.position, outside.rotation).GetComponent<Customer>();
            c.OnSleep += AddToPool;
            c.SetRanges(seatTimerRange, mealTimerRange, walkSpeedRange);
            return c;
        }

        c = pool.Dequeue();
        c.transform.position = outside.position;
        c.transform.rotation = outside.rotation;

        c.gameObject.SetActive(true);

        return c;
    }

    private int GetEmptyKiosk()
    {
        for (int i = 0; i < waitingPoint.Length; i++)
        {
            if (!kioskState[i]) return i;
        }
        return -1;
    }


    private int GetEmptyChair()
    {
        for (int i = 0; i < chairs.Length; i++)
        {
            if (!useState[i]) return i;
        }
        return -1;
    }
    private void HandleGetout(int idx)
    {
        if (customers[idx] == null) return;

        useState[idx] = false;
        Customer c = customers[idx];
        c.OnMealFinished -= HandleGetout;

        Transform outside = GetOutside();

        if (c.HasEaten()) c.setPath(Customer.cState.GoingTrash, trashBin, outside);
        else c.setPath(Customer.cState.Leaving, outside);

        customers[idx] = null;
    }

    private void AddToPool(Customer c) {
        pool.Enqueue(c);
    }
}
