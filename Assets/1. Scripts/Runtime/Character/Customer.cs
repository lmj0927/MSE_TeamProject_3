// Owned by JunYoung Park
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using Fusion;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class Customer : MonoBehaviour, IInteractable
{
    public enum cState { Entering, GoingSeat, Sitting, GoingTrash, Leaving, StageEnd }
    public enum Emotion { Happy, Boring, Angry}

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

    public Sprite portrait { get; private set; }

    private Vector2 sitRange;
    private Vector2 mealRange;
    private Vector2 speedRange;

    private int seatNum = -1;
    private float dragChair = 0.25f;

    private bool isDecided = false;
    public bool isWaiting { get; private set; } = false;
    public float sitTimer { get; private set; } = 60.0f;
    private float maxSit;
    private Emotion emotion = Emotion.Happy;
    private float boring = 30.0f;
    private float angry = 10.0f;

    private bool isEating = false;
    private bool hasEaten = false;
    private float mealTimer = 10.0f;

    public RecipeSO mainOrder { get; private set; }
    public RecipeSO drinkOrder { get; private set; }
    public RecipeSO sideOrder { get; private set; }
    private NetworkObject food;
    [SerializeField] private Image[] menuImages;
    [SerializeField] private Transform holdAnchor;
    [SerializeField] private GameObject trayPrefab;
    private GameObject tray;


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
                OrderManager.Instance.RemoveOrder(this);
                Stand();
                if (food != null) Destroy(food.gameObject);

            } else if (emotion == Emotion.Boring && sitTimer <= angry)
            {
                emotion = Emotion.Angry;
                SetFace(2);
                anim.SetTrigger("angry");
                patienceColor.SetColorState(2, 0f);
            }
            else if (emotion == Emotion.Happy && sitTimer <= boring)
            {
                patienceColor.SetColorState(1, 0f);
                emotion = Emotion.Boring;
                SetFace(1);
                anim.SetTrigger("boring");
                patienceColor.SetColorState(2, boring - angry);
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

        if (portrait != null)
        {
            if (portrait.texture != null) Destroy(portrait.texture);
            Destroy(portrait);
        }
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

        
        if (anim != null) anim.Update(0f);

        if (portrait != null)
        {
            if (portrait.texture != null) Destroy(portrait.texture);
            Destroy(portrait);
        }

        portrait = PreviewGenerator.TakeLiveSnapshot(this.gameObject, 512, 512, false);
        

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
        patienceColor?.SetColorState(0, 0);

        mealTimer = UnityEngine.Random.Range(mealRange.x, mealRange.y);

        // Random Speed
        if (agent != null) agent.speed = UnityEngine.Random.Range(speedRange.x, speedRange.y);
        if (anim != null) anim.speed = UnityEngine.Random.Range(0.9f, 1.15f);

        //set variables
        menuImages[0].sprite = mainOrder.Result.Sprite;

        SetMenuImg(drinkOrder, 1);
        SetMenuImg(sideOrder, 2);

        isDecided = false;
        emotion = Emotion.Happy;
        isWaiting = false;
        isEating = false;
        hasEaten = false;

        StopAllCoroutines();
        if (agent != null) agent.enabled = true;
        food = null;
        tray = null;
        current = cState.Entering;
        arriveHandled = false;
        alreadyDeciding = false;
        alreadyStand = false;
        agent.stoppingDistance = 0.3f;
    }
    
    private void SetMenuImg(RecipeSO r, int idx)
    {
        if (r == null)
        {
            menuImages[idx].gameObject.SetActive(false);
            return;
        }

        menuImages[idx].gameObject.SetActive(true);
        menuImages[idx].sprite = r.Result.Sprite;
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
                if (tray != null) Destroy(tray);
                setPath(cState.Leaving, exit);
                break;
            case cState.Leaving:
                OnSleep?.Invoke(this);
                gameObject.SetActive(false);
                break;
            case cState.StageEnd:
                GameManager.Instance.CompleteTask();
                Destroy(gameObject);
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

        if (current != cState.StageEnd)
        {
            isDecided = true;
            agent.enabled = true;
        }
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

        if (current != cState.StageEnd)
        {
            isWaiting = true;
            if (patienceBar != null) patienceBar.gameObject.SetActive(true);

            patienceColor?.SetColorState(1, maxSit - boring);
        }
    }

    private void GetFood(NetworkObject served)
    {
        if (patienceBar != null) patienceBar.gameObject.SetActive(false);
        isWaiting = false;

        OrderManager.Instance.RemoveOrder(this);
        served.transform.position = destination.GetChild(1).position;

        int points = CheckOrder(served);

        if (points == 0)
        {
            SetFace(2);
            anim.SetTrigger("wrong");
            Stand();
            Destroy(served.gameObject);

            return;
        }
        else GameManager.Instance.AddPoint(points);

            SetFace(0);
        anim.SetTrigger("correct");
        isEating = true;
        agent.stoppingDistance = 1.5f;
        food = served;

        food.transform.SetParent(holdAnchor, true);
        food.transform.localPosition = Vector3.zero;
        food.transform.localRotation = Quaternion.identity;
    }

    private bool CheckOrder(NetworkObject served)
    {
        List<FoodSO> orders = new List<FoodSO>();
        int points = 0;
        
        orders.Add(mainOrder.Result);
        if (drinkOrder != null) orders.Add(drinkOrder.Result);
        if (sideOrder != null) orders.Add(sideOrder.Result);

        List<FoodSO> servedFoods = served.GetComponentsInChildren<Food>().Select(f => f.Data).ToList();

        if (orders.Count != servedFoods.Count) return points;

        foreach (var order in orders)
        {
            if (servedFoods.Contains(order))
            {
                servedFoods.Remove(order);
                points += order.Point;

                continue;
            }
            return 0;
        }

        float weight = 1f;

        switch(emotion)
        {
            case Emotion.Boring: weight = 0.75f; break;
            case Emotion.Angry: weight = 0.5f; break;
        }

        return Mathf.RoundToInt(points * weight);
    }

    private void Stand()
    {
        if (alreadyStand) return;

        if (patienceBar != null) patienceBar.gameObject.SetActive(false);

        alreadyStand = true;

        if (food != null)
        {
            Destroy(food.gameObject);
        }

        StartCoroutine(StandRoutine());
        
    }

    IEnumerator StandRoutine()
    {
        yield return new WaitForSeconds(0.1f);
        if (!anim.GetCurrentAnimatorStateInfo(0).IsName("wrong"))   // Default Stand Scenario
        {
            anim.SetTrigger("stand");
            yield return new WaitForSeconds(0.1f);
        }
        else
        {
            while(true)     // Patience Stand Scenario (Controlled by AnimController)
            {
                if (anim.GetCurrentAnimatorStateInfo(0).IsName("stand")) break;
                yield return null;
            }
        }
        yield return new WaitForSeconds(anim.GetCurrentAnimatorStateInfo(0).length * 0.4f);

        transform.position = new Vector3(transform.position.x, 0, transform.position.z);
        destination.position += (destination.forward * dragChair);
        if (current != cState.StageEnd) current = cState.Leaving;

        agent.enabled = true;
        
        if (hasEaten)
        {
            tray = Instantiate(trayPrefab);

            tray.transform.SetParent(holdAnchor, true);
            
            tray.transform.localPosition = Vector3.zero;
            tray.transform.localRotation = Quaternion.identity;

            var rb = tray.GetComponent<Rigidbody>();
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        if (current != cState.StageEnd) OnMealFinished?.Invoke(seatNum);
        else setPath(current, destination);
    }

    public bool IsReady()
    {
        if (!alreadyDeciding || !isDecided) return false;
        else return true;
    }

    public void SetValues(Vector2 sit, Vector2 meal, Vector2 speed, bool beverage, bool sidemenu)
    {
        sitRange = sit;
        mealRange = meal;
        speedRange = speed;

        mainOrder = RecipeManager.Instance.GiveRandomAssembleRecipe();
        drinkOrder = beverage ? RecipeManager.Instance.GiveRandomBeverageRecipe() : null;
        sideOrder = sidemenu ? RecipeManager.Instance.GiveRandomSideRecipe() : null;

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
            
            anim.SetBool("hasTrash", next != null);
            anim.SetTrigger("walk");
        }
    }
    public float GetPatienceHueShift()
    {
        if (patienceColor != null) return patienceColor.GetCurrentHueShift(); ;
        return 0f;
    }

    public void Interact(PlayerController player)
    {
        if (!isWaiting) return;
        if (!player.HasFood()) return;

        NetworkObject served = player.RemoveFood();
        GetFood(served);
    }

    public void ForceExit(Transform exitPos)
    {
        current = cState.StageEnd;
        destination = exitPos;
        Stand();
        GameManager.Instance.RegisterTask();
    }
}
