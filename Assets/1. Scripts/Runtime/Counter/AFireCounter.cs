// Owned by JunYoung Park
using System;
using Fusion;
using UnityEngine;
using UnityEngine.Serialization;

// Abstract base class for fire-based cooking counters
public abstract class AFireCounter : ACounter
{
    [Networked] protected float cookTime { get; set; }
    [SerializeField] protected float burnTime;

    [FormerlySerializedAs("grilProgressBar")]
    [SerializeField] protected RadialProgressBar cookProgressBar;
    [SerializeField] protected RadialProgressBar burnProgressBar;

    [Networked] protected bool isDone { get; set; }
    public Action OnCookFinished;

    // State instances for different cooking phases
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

        if (HasStateAuthority)
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
        if (stateMachine == null) return;
        stateMachine.Update();
    }

    // Initialize state machine, transition to NoneState
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

    protected abstract RecipeType CookRecipeType { get; }

    // Convert raw ingredients into cooked result
    public void ApplyCookResult()
    {
        var recipe = RecipeManager.Instance.Cook(GetFoodSOs(), CookRecipeType);
        if (recipe == null) return;
        Replace(GetLastFood(), recipe.Result, Vector3.zero);
    }

    // Change state
    public void SetState(IState newState)
    {
        if (!HasStateAuthority) return;

        if (newState == NoneState) currentState = FireState.None; 
        else if (newState == CookState) currentState = FireState.Cook; 
        else if (newState == BurnState) currentState = FireState.Burn;
        // stateMachine.ChangeState(newState);
    }

    // Callback invoked on state change to sync the local state machine
    public void OnChangedCurrentState()
    {
        if (stateMachine == null) return;

        if (currentState == FireState.None) stateMachine.ChangeState(NoneState); 
        else if (currentState == FireState.Cook) stateMachine.ChangeState(CookState); 
        else if (currentState == FireState.Burn) stateMachine.ChangeState(BurnState); 
    }

    // Toggle progress bar visibility
    public void OnChangedShowCookProgress()
    {
        if (cookProgressBar == null) return;

        if (showCookProgress == false)
        {
            cookTime = 0f;
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

        cookProgressBar.SetProgress(cookProgress);
    }

    public void SetBurnProgress()
    {
        if (burnProgressBar == null)
        {
            return;
        }

        burnProgressBar.SetProgress(burnProgress);
    }

    public void AddResultFood(FoodSO resultFood)
    {
        if (resultFood == null) return;
        Replace(GetLastFood(), resultFood, Vector3.zero);
    }

    public void SetDone(bool val)
    {
        isDone = val;
    }

}