using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CustomerManager : MonoBehaviour
{
    [SerializeField]
    private GameObject customer;
    [SerializeField]
    private Transform outside;
    [SerializeField]
    private Transform[] waitingPoint;
    [SerializeField]
    private GameObject[] chairs;
    private bool[] useState;    // Each chair's use state
    private Queue<GameObject> pool = new Queue<GameObject>();
    private GameObject[] customers;
    private List<GameObject> waiting = new List<GameObject>();

    private int count = 0;
    private float spawnTimer = 10.0f;
    private float spawnTerm;
    private bool isLatest = true;
    private void Awake()
    {
        useState = new bool[chairs.Length];
        customers = new GameObject[chairs.Length];
        spawnTerm = spawnTimer;
    }
    void Start()
    {
        
    }

    void Update()
    {
        if (waiting.Count < waitingPoint.Length)
        {
            spawnTimer -= Time.deltaTime;

            if (spawnTimer <= 0 && waiting.Count < waitingPoint.Length)
            {
                spawnTimer = spawnTerm;
                GameObject c = GetCustomer();
                waiting.Add(c);

                Enter(waiting.Count - 1);
                isLatest = false;
            }

            if (!isLatest)
            {
                for (int i = 0; i < chairs.Length; i++)
                {
                    if (!useState[i] && waiting.Count > 0)
                    {
                        GameObject c = waiting[0];
                        waiting.RemoveAt(0);

                        Customer customer = c.GetComponent<Customer>();
                        customer.setPath(chairs[i].transform, Customer.cState.GoingSeat);
                        useState[i] = true;
                        customers[i] = c;

                        customer.OnMealFinished += HandleGetout;
                        customer.AssignSeat(i);
                        count++;
                    }
                }

                if (waiting.Count!= 0)
                {
                    for (int n = 0; n < waiting.Count; n++)
                    {
                        Enter(n);
                    }
                }

                isLatest = true;
            }
        }
    }

    private GameObject GetCustomer()
    {

        GameObject c;

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

    private void Enter(int idx)
    {
        GameObject c = waiting[idx];
        c.GetComponent<Customer>().setPath(waitingPoint[idx], Customer.cState.Entering);
    }

    private void HandleGetout(int idx)
    {
        useState[idx] = false;
        GameObject c = customers[idx];
        Customer customer = c.GetComponent<Customer>();
        customer.OnMealFinished -= HandleGetout;
        customer.setPath(outside, Customer.cState.Leaving);

        customers[idx] = null;
        count--;
        isLatest = false;
    }

    private void AddToPool(GameObject c) {
        pool.Enqueue(c);
    }
}
