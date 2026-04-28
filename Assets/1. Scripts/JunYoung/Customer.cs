using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class Customer : MonoBehaviour       // Food 및 레시피 완성되면 수정 필요함
{
    public enum cState { Entering, GoingSeat, Sitting, GoingTrash, Leaving }

    [SerializeField]
    private GameObject hat;
    [SerializeField]
    private GameObject body;
    [SerializeField]
    private Texture2D[] faces;

    private Transform destination;
    private NavMeshAgent agent;
    private Animator anim;
    private Renderer rd;
    private Material[] mat;

    private Vector2 seatRange;
    private Vector2 mealRange;
    private Vector2 speedRange;

    private int seatNum = -1;
    private float dragChair = 0.25f;

    private bool isDecided = false;
    private bool isWaiting = false;
    private float seatTimer = 60.0f;
    private float boring = 30.0f;
    private bool isBored = false; 
    private float angry = 10.0f;
    private bool isAngry = false;

    private bool isEating = false;
    private bool hasEaten = false;
    private float mealTimer = 10.0f;

    // temp variable
    private int food = 0;

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
            seatTimer -= Time.deltaTime;

            if (seatTimer <= 0 )
            {
                SetFace(2);
                isWaiting = false;
                Stand();

            } else if (!isAngry && seatTimer <= angry)
            {
                isAngry = true;
                SetFace(2);
                anim.SetTrigger("angry");
            }
            else if (!isAngry && !isBored && seatTimer <= boring)
            {
                isBored = true;
                SetFace(1);
                anim.SetTrigger("boring");
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
        SetHat(UnityEngine.Random.Range(0, 3));
        SetFace(0);

        // Random Timer
        seatTimer = UnityEngine.Random.Range(seatRange.x, seatRange.y);
        boring = seatTimer * 0.5f;
        angry = seatTimer * 0.15f;

        mealTimer = UnityEngine.Random.Range(mealRange.x, mealRange.y);

        // Random Speed
        if (agent != null) agent.speed = UnityEngine.Random.Range(speedRange.x, speedRange.y);
        if (anim != null) anim.speed = UnityEngine.Random.Range(0.9f, 1.15f);
    }
    private void RandomColor()
    {
        Color randomC = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);

        mat[0].SetColor("_BaseColor", randomC);
    }

    // 0 is nothing
    // 1 is fedora
    // 2 is party hat
    private void SetHat(int type)
    {
        int max = hat.transform.childCount;

        for (int i = 0; i < max; i++)
        {
            hat.transform.GetChild(i).gameObject.SetActive(i == (type-1));

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
                Reset();
                gameObject.SetActive(false);
                break;
        }
    }

    private void Order()
    {
        if (alreadyDeciding) return;

        alreadyDeciding = true;
        StartCoroutine("DecideRoutine");
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
        isWaiting = true;
    }

    private void Stand()
    {
        if (alreadyStand) return;

        alreadyStand = true;
        StartCoroutine("StandRoutine");
        
    }

    IEnumerator StandRoutine()
    {
        yield return new WaitForSeconds(0.1f);
        if (!anim.GetCurrentAnimatorStateInfo(0).IsName("wrong"))   // Default Stand Scenario
        {
            anim.SetTrigger("stand");

            while (!anim.GetCurrentAnimatorStateInfo(0).IsName("stand")) yield return null;

            yield return new WaitForSeconds(anim.GetCurrentAnimatorStateInfo(0).length);
        } else yield return new WaitForSeconds(1.0f);   // Patience Stand Scenario (Controlled by AnimController)

        transform.position = new Vector3(transform.position.x, 0, transform.position.z);
        destination.position += (destination.forward * dragChair);
        current = cState.Leaving;

        agent.enabled = true;

        OnMealFinished?.Invoke(seatNum);
    }

    public void SetRanges(Vector2 seat, Vector2 meal, Vector2 speed)
    {
        seatRange = seat;
        mealRange = meal;
        speedRange = speed;
        InitializeStats();
    }
    // temp parameter
    public void getFood(int served)
    {
        isWaiting = false;

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

    private void Reset()
    {
        InitializeStats();

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

        OnSleep?.Invoke(this);
    }

}
