// Owned by JunYoung Park
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class CustomerManager : NetworkBehaviour
{
    private bool isPlaying = false;

    [SerializeField]
    private GameObject customer;

    [Tooltip("patience tiemr range")]
    [SerializeField] private Vector2 sitTimerRange = new Vector2(45f, 75f);
    [Tooltip("meal timer range")]
    [SerializeField] private Vector2 mealTimerRange = new Vector2(8f, 15f);
    [Tooltip("walk speed range")]
    [SerializeField] private Vector2 walkSpeedRange = new Vector2(2.5f, 3.5f);
    [Tooltip("Parent of Spawnpoints")]
    [SerializeField] private Transform outsideParent;

    private Queue<Customer> pool = new Queue<Customer>();

    [SerializeField] private Transform[] waitingPoint;
    private bool[] kioskState;      // Each kiosk's use state
    private Customer[] kCustomers;

    [Tooltip("Parent of all chairs")]
    [SerializeField] private Transform chairParent;
    private Transform[] chairs;
    private bool[] useState;    // Each chair's use state
    private Customer[] customers;

    [Tooltip("Position of Customer's trash bin")]
    [SerializeField] private Transform trashBin;


    private float spawnTimer = 0;

    [Tooltip("term of customer entrance")]
    [SerializeField] private float spawnTerm = 3.0f;

    [Tooltip("Probability of ordering wiht beverage (0.0 ~ 1.0)")]
    [SerializeField] private float beverageRatio = 0.8f;

    [Tooltip("Probability of ordering wiht sidemenu (0.0 ~ 1.0)")]
    [SerializeField] private float sideRatio = 0.5f;
    public override void Spawned()
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

        if(GameManager.Instance == null) GameManager.BindInitializer(GameManagerActionsSetup);
        else GameManagerActionsSetup();
    }

    private void GameManagerActionsSetup()
    {
        GameManager.Instance.OnStageStart += RPC_HandleStageStart;
        GameManager.Instance.OnStageEnd += RPC_HandleStageEnd;
    }
    public override void FixedUpdateNetwork()
    {
        if (!isPlaying) return;

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


                        OrderManager.Instance.AddOrder(c);
                    }

                }
            }
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance == null) return;

        GameManager.Instance.OnStageStart -= RPC_HandleStageStart;
        GameManager.Instance.OnStageEnd -= RPC_HandleStageEnd;
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
        }
        else
        {
            c = pool.Dequeue();
            c.transform.position = outside.position;
            c.transform.rotation = outside.rotation;

            c.gameObject.SetActive(true);
        }
        bool beverage = UnityEngine.Random.value <= beverageRatio;
        bool sidemenu = UnityEngine.Random.value <= sideRatio;
        c.SetValues(sitTimerRange, mealTimerRange, walkSpeedRange, beverage, sidemenu);
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
    private void AddToPool(Customer c)
    {
        pool.Enqueue(c);
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

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_HandleStageStart() {
        if(!HasStateAuthority) return;
        isPlaying = true;
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_HandleStageEnd() {
        if(!HasStateAuthority) return;
        isPlaying = false;

        foreach(var kC in kCustomers) {
            if (kC == null) continue;

            Transform outside = GetOutside();
            kC.ForceExit(outside);
        }

        foreach (var c in customers)
        {
            if (c == null) continue;

            Transform outside = GetOutside();
            c.ForceExit(outside);
        }
    }
}
