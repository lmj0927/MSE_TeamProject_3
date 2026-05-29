using System;
using Fusion;
using UnityEngine;
using UnityEngine.Serialization;

public abstract class AFireCounter : ACounter
{
    [Networked] protected float cookTime { get; set; }
    // [Networked] public float elapsedTime { get; set; }
    [SerializeField] protected float burnTime;
    [FormerlySerializedAs("grilProgressBar")]
    [SerializeField] protected RadialProgressBar cookProgressBar;
    [SerializeField] protected RadialProgressBar burnProgressBar;
    [Networked] protected bool isDone { get; set; }
    public Action OnCookFinished;
    public FireCounter_NoneState NoneState { get; private set; }
    public FireCounter_CookState CookState { get; private set; }
    public FireCounter_BurnState BurnState { get; private set; }
    public enum FireState : byte { None, Cook, Burn }
    [Networked, OnChangedRender(nameof(OnChangedCurrentState))] public FireState currentState { get; set; }

    private StateMachine stateMachine;

    [Networked, OnChangedRender(nameof(OnChangedShowCookProgress))] public bool showCookProgress { get; set; }
    [Networked, OnChangedRender(nameof(SetCookProgress))] public float cookProgress { get; set; }
    [Networked, OnChangedRender(nameof(SetBurnProgress))] public float burnProgress { get; set; }




    public override void Spawned()
    {
        base.Spawned();

        InitState();

        if(HasStateAuthority)
        {
            cookTime = 0f;
            isDone = false;
            showCookProgress = false;
        }

        if (cookProgressBar != null)
        {
            cookProgressBar.gameObject.SetActive(false);
            cookProgressBar.SetProgress(0f);
        }
        if (burnProgressBar != null)
        {
            burnProgressBar.gameObject.SetActive(false);
            burnProgressBar.SetProgress(0f);
        }
    }

    private void Update()
    {
        if(stateMachine == null) return;
        stateMachine.Update();
    }

    private void InitState()
    {
        stateMachine = new StateMachine();

        NoneState = new FireCounter_NoneState(this);
        CookState = new FireCounter_CookState(this);
        BurnState = new FireCounter_BurnState(this);

        stateMachine.ChangeState(NoneState);
    }

    public float CookTime => cookTime;
    public float BurnTime => burnTime;

    public FoodSO resultFood;

    public void SetState(IState newState)
    {
        if(!HasStateAuthority) return;

        if     (newState == NoneState) currentState = FireState.None; // call callback
        else if(newState == CookState) currentState = FireState.Cook; // call callback
        else if(newState == BurnState) currentState = FireState.Burn; // call callback
        // stateMachine.ChangeState(newState);
    }

    public void OnChangedCurrentState()
    {
        if(stateMachine == null) return;

        if     (currentState == FireState.None) stateMachine.ChangeState(NoneState); // local timer
        else if(currentState == FireState.Cook) stateMachine.ChangeState(CookState); // local timer
        else if(currentState == FireState.Burn) stateMachine.ChangeState(BurnState); // local timer
    }
    
    public void OnChangedShowCookProgress()
    {
        if(cookProgressBar == null) return;

        if(showCookProgress == false) {
            cookTime = 0f;
            // SetCookProgress(0f);
            // SetBurnProgress(0f);
            cookProgress = 0f;
            burnProgress = 0f;
        }
        cookProgressBar.gameObject.SetActive(showCookProgress);
    }

    public void SetCookProgress()
    {
        if (cookProgressBar == null)
        {
            return;
        }

        // cookProgressBar.SetProgress(normalizedValue);
        cookProgressBar.SetProgress(cookProgress);
    }

    public void SetBurnProgress()
    {
        if (burnProgressBar == null)
        {
            return;
        }

        // burnProgressBar.SetProgress(normalizedValue);
        burnProgressBar.SetProgress(burnProgress);
    }

    public void AddResultFood(FoodSO resultFood)
    {
        if (resultFood == null) return;
        // var food = RemoveFood();
        // Destroy(food.gameObject);
        // AddFood(FoodSpawner.SpawnFood(Runner, resultFood));
        var food = GetLastFood();
        OnRemoved(food);
        food.GetComponent<AuthorityHandler>().RequestStateAuthority(
            onAuthorized: () =>
            {
                Debug.Log($"[AFireCounter AddResultFood] {food.Name} will be despawned. {resultFood.FoodName} will be spawned.");
                FoodSpawner.Despawn(Runner, food);
                NetworkObject resultNO = FoodSpawner.SpawnFood(Runner, resultFood);
                OnAdded(resultNO, Vector3.zero);        
            },
            onNotAuthorized: () =>
            {
                Debug.Log("[AFireCounter AddResultFood] denied.");
            }
        );
    }

    public void SetDone(bool val)
    {
        isDone = val;
    }

    public void OnChangedElapsedTime()
    {

    }
}
