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

public class Customer : FoodHolder, IInteractable
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

    [Networked] private bool isDecided { get; set; }= false;
    [Networked] public bool isWaiting { get; private set; } = false;
    [Networked] public float sitTimer { get; private set; } = 60.0f;
    [Networked] private float maxSit { get; set; }
    [Networked] private Emotion emotion { get; set; } = Emotion.Happy;
    private float boring = 30.0f;
    private float angry = 10.0f;

    public Action OnEmotionChange;
    private Vector2 pitchRange = new Vector2(0.8f, 2f);
    private float pitch;

    [Networked] private bool isEating { get; set; }= false;
    [Networked] private bool hasEaten { get; set; } = false;

    private float mealTimer = 10.0f;

    // Recipe selection synced by idx; SO looked up locally via RecipeManager
    [Networked] private int mainOrderIdx { get; set; } = -1;
    [Networked] private int drinkOrderIdx { get; set; } = -1;
    [Networked] private int sideOrderIdx { get; set; } = -1;
    public RecipeSO mainOrder => mainOrderIdx >= 0 ? RecipeManager.Instance?.GetAssemble(mainOrderIdx) : null;
    public RecipeSO drinkOrder => drinkOrderIdx >= 0 ? RecipeManager.Instance?.GetBeverage(drinkOrderIdx) : null;
    public RecipeSO sideOrder => sideOrderIdx >= 0 ? RecipeManager.Instance?.GetSide(sideOrderIdx) : null;
    private NetworkObject food;
    [SerializeField] private Image[] menuImages;
    [SerializeField] private Transform holdAnchor;
    [SerializeField] private GameObject trayPrefab;
    private NetworkObject tray;

    public Action<int> OnMealFinished;
    public Action<Customer> OnSleep;

    [Networked] private cState current { get; set; } = cState.Entering;
    [Networked] private bool arriveHandled { get; set; } = false;
    [Networked] private bool alreadyDeciding { get; set; } = false;
    [Networked] private bool alreadyStand { get; set; } = false;

    // Networked costume (replicates appearance to all clients)
    [Networked] private Color bodyColor { get; set; }
    [Networked] private int hatIdx { get; set; }
    [Networked] private int glassesIdx { get; set; }
    [Networked] private int mouthIdx { get; set; }
    [Networked, OnChangedRender(nameof(OnFaceChanged))] private int faceIdx { get; set; }
    [Networked, OnChangedRender(nameof(OnCostumeChanged))] private int costumeVersion { get; set; }

    private enum AnimTrigger { Idle, Angry, Boring, Sit, Wrong, Correct, Stand }

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

    // Proxy: initial state already synced -> apply now. Master: skip (values still default, InitializeStats will apply).
    public override void Spawned()
    {
        base.Spawned();
        if (costumeVersion > 0) OnCostumeChanged();
    }

    public override void FixedUpdateNetwork()
    {
        if(!HasStateAuthority) return;
        // Arrival Triggers
        if (agent.enabled && !arriveHandled && IsArrived())
        {
            arriveHandled = true;
            HandleArrival();
        }

        // Waiting Food
        if (isWaiting)
        {
            sitTimer -= Runner.DeltaTime;

            if (patienceBar != null)
            {
                patienceBar.SetProgress(sitTimer / maxSit);
            }

            if (sitTimer <= 0 )
            {
                faceIdx = 2;
                isWaiting = false;
                OrderManager.Instance.RemoveOrder(this);
                Stand();
                Discard(food);

            } else if (emotion == Emotion.Boring && sitTimer <= angry)
            {
                emotion = Emotion.Angry;

                OnEmotionChange?.Invoke();
                SoundManager.Instance.Angry(this, pitch);
                faceIdx = 2;
                RPC_PlayAnim(AnimTrigger.Angry);
                RPC_SetPatience(2, 0f);
            }
            else if (emotion == Emotion.Happy && sitTimer <= boring)
            {
                RPC_SetPatience(1, 0f);
                emotion = Emotion.Boring;

                OnEmotionChange?.Invoke();
                SoundManager.Instance.Boring(this, pitch);
                faceIdx = 1;
                RPC_PlayAnim(AnimTrigger.Boring);
                RPC_SetPatience(2, boring - angry);
            }

        }

        // Eating food
        if (isEating)
        {
            mealTimer -= Runner.DeltaTime;

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
        OnEmotionChange = null;
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
        // Random Appearance (sync via Networked + costumeVersion bump triggers ApplyCostume on all)
        bodyColor = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
        hatIdx = UnityEngine.Random.Range(0, hat.transform.childCount + 1);
        glassesIdx = UnityEngine.Random.Range(0, glasses.transform.childCount + 1);
        mouthIdx = UnityEngine.Random.Range(0, mouth.transform.childCount + 1);
        faceIdx = 0;
        costumeVersion++;

        // Random Timer
        sitTimer = UnityEngine.Random.Range(sitRange.x, sitRange.y);
        maxSit = sitTimer;
        boring = sitTimer * 0.6f;
        angry = sitTimer * 0.25f;

        pitch = UnityEngine.Random.Range(pitchRange.x, pitchRange.y);

        if (patienceBar != null)
        {
            patienceBar.SetProgress(1f);
            patienceBar.gameObject.SetActive(false);
        }
        RPC_SetPatience(0, 0f);

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

        // Apply locally on master (OnChangedRender on authority isn't guaranteed same-tick)
        OnCostumeChanged();
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
    // OnChangedRender callbacks: replicate appearance to all clients
    private void OnFaceChanged() => SetFace(faceIdx);

    private void OnCostumeChanged()
    {
        if (mat != null && mat.Length > 0) mat[0].SetColor("_BaseColor", bodyColor);
        SetCostume(hatIdx, glassesIdx, mouthIdx);
        SetFace(faceIdx);

        if (anim != null) anim.Update(0f);
        TakeSnapshot();
    }

    private int lastSnapshotVersion = -1;

    // Defer to end-of-frame: camera.Render() during Fusion tick breaks URP render pass state
    private void TakeSnapshot()
    {
        StartCoroutine(TakeSnapshotRoutine());
    }

    private IEnumerator TakeSnapshotRoutine()
    {
        yield return new WaitForEndOfFrame();
        if (this == null || gameObject == null) yield break;
        if (lastSnapshotVersion == costumeVersion) yield break; // dedupe per costume version
        lastSnapshotVersion = costumeVersion;

        if (portrait != null)
        {
            if (portrait.texture != null) Destroy(portrait.texture);
            Destroy(portrait);
        }
        portrait = PreviewGenerator.TakeLiveSnapshot(this.gameObject, 512, 512, false);
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
        if (hat == null || glasses == null || mouth == null) return;
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
        if (mat == null || mat.Length < 2 || faces == null || faces.Length == 0) return;
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
                RPC_PlayAnim(AnimTrigger.Idle);
                Order();
                break;
            case cState.GoingSeat:
                Sit();
                break;
            case cState.GoingTrash:
                if (tray != null) Runner.Despawn(tray);
                setPath(cState.Leaving, exit);
                break;
            case cState.Leaving:
                OnSleep?.Invoke(this);
                gameObject.SetActive(false);
                break;
            case cState.StageEnd:
                GameManager.Instance.CompleteTask();
                Runner.Despawn(Object);
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

        RPC_PlayAnim(AnimTrigger.Sit);
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

            RPC_SetPatience(1, maxSit - boring);
        }
    }

    protected override void OnAdded(NetworkObject served, Vector3 _)
    {
        if (patienceBar != null) patienceBar.gameObject.SetActive(false);
        isWaiting = false;
        OrderManager.Instance.RemoveOrder(this);

        OnEmotionChange?.Invoke();
        SoundManager.Instance.Happy(this, pitch);
        faceIdx = 0;
        RPC_PlayAnim(AnimTrigger.Correct);
        isEating = true;
        agent.stoppingDistance = 1.5f;
        food = served;

        served.GetComponent<Food>().SetHeld();
        served.transform.SetParent(holdAnchor, true);
        served.transform.localPosition = Vector3.zero;
        served.transform.localRotation = Quaternion.identity;
    }

    protected override void OnRemoved(NetworkObject served)
    {
        if (served != null && served == food) food = null;
    }

    public override bool CanAdd(Food f) => isWaiting && food == null;
    public override bool CanRemove() => food != null;

    public override void ClearAll(Action onDone = null)
    {
        if (food == null)
        {
            onDone?.Invoke();
            return;
        }
        Discard(food, onDone);
    }

    private int CheckOrder(NetworkObject served)
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

        Discard(food);

        StartCoroutine(StandRoutine());
        
    }

    IEnumerator StandRoutine()
    {
        yield return new WaitForSeconds(0.1f);
        if (!anim.GetCurrentAnimatorStateInfo(0).IsName("wrong"))   // Default Stand Scenario
        {
            RPC_PlayAnim(AnimTrigger.Stand);
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
            tray = Runner.Spawn(trayPrefab);

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

        mainOrderIdx = RecipeManager.Instance.RandomAssembleIdx();
        drinkOrderIdx = beverage ? RecipeManager.Instance.RandomBeverageIdx() : -1;
        sideOrderIdx = sidemenu ? RecipeManager.Instance.RandomSideIdx() : -1;

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
            RPC_PlayWalk(next != null);
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

        NetworkObject served = player.HeldFoodObject;
        int points = CheckOrder(served);

        if (points == 0)
        {
            if (patienceBar != null) patienceBar.gameObject.SetActive(false);
            isWaiting = false;
            OrderManager.Instance.RemoveOrder(this);

            OnEmotionChange?.Invoke();
            SoundManager.Instance.Angry(this, pitch);
            faceIdx = 2;
            RPC_PlayAnim(AnimTrigger.Wrong);
            Stand();

            player.Discard(served);
            return;
        }

        GameManager.Instance.AddPoint(points);
        player.HandoffTo(this, served, Vector3.zero);
    }

    public void ForceExit(Transform exitPos)
    {
        current = cState.StageEnd;
        destination = exitPos;
        Stand();
        GameManager.Instance.RegisterTask();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayAnim(AnimTrigger trigger)
    {
        if (anim == null) return;
        switch (trigger)
        {
            case AnimTrigger.Idle: anim.SetTrigger("idle"); break;
            case AnimTrigger.Angry: anim.SetTrigger("angry"); break;
            case AnimTrigger.Boring: anim.SetTrigger("boring"); break;
            case AnimTrigger.Sit: anim.SetTrigger("sit"); break;
            case AnimTrigger.Wrong: anim.SetTrigger("wrong"); break;
            case AnimTrigger.Correct: anim.SetTrigger("correct"); break;
            case AnimTrigger.Stand: anim.SetTrigger("stand"); break;
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayWalk(bool trash)
    {
        if (anim == null) return;
        anim.SetBool("hasTrash", trash);
        anim.SetTrigger("walk");
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_SetPatience(int state, float duration)
    {
        patienceColor?.SetColorState(state, duration);
    }
}
