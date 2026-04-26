using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CustomerManager : MonoBehaviour
{
    [SerializeField]
    private GameObject customer;
    [SerializeField]
    private Transform outsideParent;

    private Queue<GameObject> pool = new Queue<GameObject>();

    [SerializeField]
    private Transform[] waitingPoint;
    private bool[] kioskState;      // Each kiosk's use state
    private GameObject[] kCustomers;

    [SerializeField]
    private Transform chairParent;
    private Transform[] chairs;
    private bool[] useState;    // Each chair's use state
    private GameObject[] customers;

    [SerializeField]
    private Transform trashBin;

    private float spawnTimer = 0;
    private float spawnTerm = 3.0f;
    private void Awake()
    {
        kioskState = new bool[waitingPoint.Length];
        kCustomers = new GameObject[waitingPoint.Length];

        chairs = new Transform[chairParent.transform.childCount];

        for (int i = 0; i < chairs.Length; i++)
        {
            chairs[i] = chairParent.GetChild(i);
        }
        useState = new bool[chairs.Length];
        customers = new GameObject[chairs.Length];
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
                GameObject c = GetCustomer();

                kioskState[emptyK] = true;
                kCustomers[emptyK] = c;

                c.GetComponent<Customer>().setPath(Customer.cState.Entering, waitingPoint[emptyK]);
            }
        }

        for (int i = 0; i < waitingPoint.Length; i++)
        {
            if (kioskState[i] && kCustomers[i] != null)
            {
                Customer customer = kCustomers[i].GetComponent<Customer>();

                if (customer.IsReady())
                {
                    int emptyC = GetEmptyChair();

                    if (emptyC != -1)
                    {
                        useState[emptyC] = true;
                        customers[emptyC] = kCustomers[i];

                        customer.AssignSeat(emptyC);
                        customer.OnMealFinished += HandleGetout;
                        customer.setPath(Customer.cState.GoingSeat, chairs[emptyC]);

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
    private GameObject GetCustomer()
    {

        GameObject c;

        Transform outside = GetOutside();

        if (pool.Count == 0)
        {
            c = Instantiate(customer, outside.position, outside.rotation);
            c.GetComponent<Customer>().OnSleep += AddToPool;
            return c;
        }

        c = pool.Dequeue();
        c.transform.position = outside.position;
        c.transform.rotation = outside.rotation;

        c.SetActive(true);

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
        GameObject c = customers[idx];
        Customer customer = c.GetComponent<Customer>();
        customer.OnMealFinished -= HandleGetout;

        Transform outside = GetOutside();

        if (customer.HasEaten()) customer.setPath(Customer.cState.GoingTrash, trashBin, outside);
        else customer.setPath(Customer.cState.Leaving, outside);

        customers[idx] = null;
    }

    private void AddToPool(GameObject c) {
        pool.Enqueue(c);
    }
}
