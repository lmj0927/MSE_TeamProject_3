using minjun;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class Customer : MonoBehaviour, IInteractable
{
    public enum cState { Entering, GoingSeat, Sitting, GoingTrash, Leaving }

    [SerializeField]
    private GameObject hat;
    [SerializeField]
    private GameObject glasses;
    [SerializeField]
    private GameObject mouth;
    [SerializeField]
    private GameObject body;
    [SerializeField]
    private Texture2D[] faces;
    [SerializeField]
    private ProgressBar patienceBar;
    private StateChanger patienceColor;

    private Transform destination;
    private NavMeshAgent agent;
    private Animator anim;
    private Renderer rd;
    private Material[] mat;

    private Vector2 sitRange;
    private Vector2 mealRange;
    private Vector2 speedRange;

    private int seatNum = -1;
    private float dragChair = 0.25f;

    private bool isDecided = false;
    private bool isWaiting = false;
    private float sitTimer = 60.0f;
    private float maxSit;
    private float boring = 30.0f;
    private bool isBored = false; 
    private float angry = 10.0f;
    private bool isAngry = false;

    private bool isEating = false;
    private bool hasEaten = false;
    private float mealTimer = 10.0f;

    private Food food;

    // Event for CustomerManger to check Customer leaving
    public Action<int> OnMealFinished;
    public Action<Customer> OnSleep;

    private cState current = cState.Entering;
    private bool arriveHandled = false;
    private bool alreadyDeciding = false;
    private bool alreadyStand = false;

    private Transform exit;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        rd = body.GetComponent<Renderer>();
        mat = rd.materials;

        if (patienceBar != null)
        {
            patienceColor = patienceBar.GetComponent<StateChanger>();
        }
    }

    private void Update()
    {
        // Arrival Triggers
        if (agent.enabled && !arriveHandled && IsArrived())
        {
            arriveHandled = true;
            HandleArrival();
        }

        // Waiting Food
        if (isWaiting)
        {
            sitTimer -= Time.deltaTime;

            if (patienceBar != null)
            {
                patienceBar.SetProgress(sitTimer / maxSit);
            }

            if (sitTimer <= 0 )
            {
                SetFace(2);
                isWaiting = false;
                Stand();

            } else if (!isAngry && sitTimer <= angry)
            {
                isAngry = true;
                SetFace(2);
                anim.SetTrigger("angry");
                patienceColor.SetColorState(2);
            }
            else if (!isAngry && !isBored && sitTimer <= boring)
            {
                isBored = true;
                SetFace(1);
                anim.SetTrigger("boring");
                patienceColor.SetColorState(1);
            }

        }

        // Eating food
        if (isEating)
        {
            mealTimer -= Time.deltaTime;

            if (mealTimer <= 0)
            {
                isEating = false;
                hasEaten = true;
                Stand();
            }
        }
    }

    private void OnDestroy()
    {
        OnMealFinished = null;
        OnSleep = null;
    }

    // Basic Functions:

    private void InitializeStats()
    {
        // Random Appearance
        RandomColor();
        int hatType = UnityEngine.Random.Range(0, hat.transform.childCount + 1);
        int glassesType = UnityEngine.Random.Range(0, glasses.transform.childCount + 1);
        int mouthType = UnityEngine.Random.Range(0, mouth.transform.childCount + 1);
        SetCostume(hatType, glassesType, mouthType);
        SetFace(0);

        // Random Timer
        sitTimer = UnityEngine.Random.Range(sitRange.x, sitRange.y);
        maxSit = sitTimer;
        boring = sitTimer * 0.6f;
        angry = sitTimer * 0.25f;

        if (patienceBar != null)
        {
            patienceBar.SetProgress(1f);
            patienceBar.gameObject.SetActive(false);
        }
        patienceColor?.SetColorState(0);

        mealTimer = UnityEngine.Random.Range(mealRange.x, mealRange.y);

        // Random Speed
        if (agent != null) agent.speed = UnityEngine.Random.Range(speedRange.x, speedRange.y);
        if (anim != null) anim.speed = UnityEngine.Random.Range(0.9f, 1.15f);

        food = null;
        isDecided = false;
        isBored = false;
        isAngry = false;
        isWaiting = false;
        isEating = false;
        hasEaten = false;

        current = cState.Entering;
        arriveHandled = false;
        alreadyDeciding = false;
        alreadyStand = false;
        agent.stoppingDistance = 0.3f;
    }

    private void RandomColor()
    {
        Color randomC = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);

        mat[0].SetColor("_BaseColor", randomC);
    }

    // h = Hat type
    // 0 is nothing
    // 1 is fedora
    // 2 is party hat
    // 3 is clown hat
    // 4 is advebture hat
    // 5 is bunny hat

    // g = Glasses type
    // 0 is nothing
    // 1 is fedora
    // 2 is party hat
    // 3 is clown nose

    // m = Mouth type
    // 0 is nothing
    // 1 is mustache_1
    // 2 is mustache_2
    // 3 is pacifier
    private void SetCostume(int h, int g, int m)
    {
        int hMax = hat.transform.childCount;

        for (int i = 0; i < hMax; i++)
        {
            hat.transform.GetChild(i).gameObject.SetActive(i == (h-1));

        }

        int gMax = glasses.transform.childCount;

        for (int i = 0; i < gMax; i++)
        {
            glasses.transform.GetChild(i).gameObject.SetActive(i == (g - 1));

        }

        int mMax = mouth.transform.childCount;

        for (int i = 0; i < mMax; i++)
        {
            mouth.transform.GetChild(i).gameObject.SetActive(i == (m - 1));

        }
    }

    // 0 is happy (default)
    // 1 is uncomfortable
    // 2 is angry
    private void SetFace(int type)
    {
        type = Mathf.Abs(type);
        int idx = faces.Length > type ? type : 0;
        
        mat[1].SetTexture("_BaseMap", faces[idx]);
    }

    public void AssignSeat(int idx)
    {
        seatNum = idx;
    }

    bool IsArrived()
    {
        if (!agent.enabled || agent.pathPending) return false;

        if (agent.remainingDistance <= agent.stoppingDistance)
        {
            if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
            {
                return true;
            }
        }

        return false;
    }

    // State Change Functions:
    private void HandleArrival()
    {
        switch (current)
        {
            case cState.Entering:
                anim.SetTrigger("idle");
                Order();
                break;
            case cState.GoingSeat:
                Sit();
                break;
            case cState.GoingTrash:
                setPath(cState.Leaving, exit);
                break;
            case cState.Leaving:
                OnSleep?.Invoke(this);
                gameObject.SetActive(false);
                break;
        }
    }

    private void Order()
    {
        if (alreadyDeciding) return;

        alreadyDeciding = true;
        StartCoroutine(DecideRoutine());
    }

    IEnumerator DecideRoutine()
    {
        agent.enabled = false;
        transform.rotation = Quaternion.LookRotation(destination.forward);

        yield return new WaitForSeconds(UnityEngine.Random.Range(1, 6));
        // 주문 결정 기능 구현 필요

        isDecided = true;
        agent.enabled = true;
    }

    public bool IsReady()
    {
        if (!alreadyDeciding || !isDecided) return false;
        else return true;
    }

    private void Sit()
    {
        agent.enabled = false;
        
        transform.rotation = Quaternion.LookRotation(destination.forward);

        transform.position = destination.GetChild(0).position + (-transform.forward * 0.1f);
        destination.position += (-destination.forward * dragChair);

        anim.SetTrigger("sit");
        current = cState.Sitting;

        StartCoroutine(SitRoutine());
    }

    IEnumerator SitRoutine()
    {
        yield return new WaitForSeconds(0.1f);
        yield return new WaitForSeconds(anim.GetCurrentAnimatorStateInfo(0).length * 0.9f);

        isWaiting = true;
        if (patienceBar != null) patienceBar.gameObject.SetActive(true);
    }

    private void getFood(Food served)
    {
        if (patienceBar != null) patienceBar.gameObject.SetActive(false);
        isWaiting = false;

        // Food 스크립트 확정 후, 수정 필요
        if (food != served)
        {
            SetFace(2);
            anim.SetTrigger("wrong");
            Stand();

            return;
        }

        SetFace(0);
        anim.SetTrigger("correct");
        isEating = true;
        agent.stoppingDistance = 1.5f;
    }

    private void Stand()
    {
        if (alreadyStand) return;

        if (patienceBar != null) patienceBar.gameObject.SetActive(false);

        alreadyStand = true;
        StartCoroutine(StandRoutine());
        
    }

    IEnumerator StandRoutine()
    {
        yield return new WaitForSeconds(0.1f);
        if (!anim.GetCurrentAnimatorStateInfo(0).IsName("wrong"))   // Default Stand Scenario
        {
            anim.SetTrigger("stand");
            yield return new WaitForSeconds(0.1f);

            yield return new WaitForSeconds(anim.GetCurrentAnimatorStateInfo(0).length *0.4f);
        } else yield return new WaitForSeconds(1.0f);   // Patience Stand Scenario (Controlled by AnimController)

        transform.position = new Vector3(transform.position.x, 0, transform.position.z);
        destination.position += (destination.forward * dragChair);
        current = cState.Leaving;

        agent.enabled = true;

        OnMealFinished?.Invoke(seatNum);
    }

    public bool SetOrder(Food order)
    {
        // Trash food 조건 추가 필요
        if (order == null) return false;

        food = order;
        return true;
    }
    public void SetRanges(Vector2 sit, Vector2 meal, Vector2 speed)
    {
        sitRange = sit;
        mealRange = meal;
        speedRange = speed;

        InitializeStats();
    }

    public bool HasEaten() => hasEaten;

    public void setPath(cState state, Transform pos, Transform next = null)     // next is for special case(successful meal)
    {
        current = state;
        arriveHandled = false;

        if (next == null) destination = pos;
        else
        {
            destination = pos;
            exit = next;
        }

        if (agent.enabled)
        {
            agent.SetDestination(destination.position);
            anim.SetTrigger("walk");
        }
    }

    public void Interact(Player player)
    {
        if (!isWaiting) return;
        if (!player.HasFood()) return;

        Food served = player.RemoveFood();
        getFood(served);

    }
}
