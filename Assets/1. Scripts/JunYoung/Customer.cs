using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class Customer : MonoBehaviour
{
    public enum cState { Entering, GoingSeat, Sitting, Leaving }

    [SerializeField]
    private GameObject hat;
    [SerializeField]
    private GameObject body;
    [SerializeField]
    private Texture2D[] faces;

    [SerializeField]
    private Transform destination;
    private NavMeshAgent agent;
    private Animator anim;

    private int seatNum = -1;

    private bool isWaiting = false;
    private float seatTimer = 60.0f;
    private float boring = 30.0f;
    private bool isBored = false; 
    private float angry = 10.0f;
    private bool isAngry = false;

    private bool isEating = false;
    private float mealTimer = 10.0f;

    // temp variable
    private int food = 0;

    // Event for CustomerManger to check Customer leaving
    public Action<int> OnMealFinished;

    private cState current = cState.Entering;
    private bool arriveHandled = false;
    private bool alreadyStand = false;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
    }
    private void Start()
    {
        RandomColor();
        int ran = UnityEngine.Random.Range(0, 3);
        SetHat(ran);
        SetFace(0);
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
                Stand();
            }
        }
    }

    // Basic Functions:
    private void RandomColor()
    {
        Color randomC = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
        Renderer rd = body.GetComponent<Renderer>();

        rd.materials[0].SetColor("_BaseColor", randomC);
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
    void SetFace(int type)
    {
        type = Mathf.Abs(type);
        int idx = faces.Length > type ? type : 0;
        
        Renderer rd = body.GetComponent<Renderer>();


        rd.materials[1].SetTexture("_BaseMap", faces[idx]);
    }

    void AssignSeat(int idx)
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
                break;
            case cState.GoingSeat:
                Sit();
                break;
            case cState.Leaving:
                Destroy(gameObject);
                break;
        }
    }

    private void Sit()
    {
        agent.enabled = false;

        transform.position = destination.position;
        transform.rotation = Quaternion.LookRotation(-destination.forward);

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

        current = cState.Leaving;

        agent.enabled = true;

        OnMealFinished?.Invoke(seatNum);
    }

    // temp parameter
    void getFood(int served)
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
    }

    void setPath(Transform pos, cState state)
    {
        current = state;
        arriveHandled = false;

        destination = pos;

        if (agent.enabled)
        {
            agent.SetDestination(destination.position);
            anim.SetTrigger("walk");
        }
    }

  

}
