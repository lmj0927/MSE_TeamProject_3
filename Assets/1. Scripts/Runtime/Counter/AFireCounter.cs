using UnityEngine;
using UnityEngine.Serialization;

public abstract class AFireCounter : ACounter
{
    protected float cookTime = 0;
    [SerializeField] protected float burnTime;
    [FormerlySerializedAs("grilProgressBar")]
    [SerializeField] protected RadialProgressBar cookProgressBar;
    [SerializeField] protected RadialProgressBar burnProgressBar;
    protected bool isDone = false;
    public FireCounter_NoneState NoneState { get; private set; }
    public FireCounter_CookState CookState { get; private set; }
    public FireCounter_BurnState BurnState { get; private set; }

    private StateMachine stateMachine;
    private void Awake()
    {
        InitState();
    }

    private void Update()
    {
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
        stateMachine.ChangeState(newState);
    }
    public void ShowCookProgress()
    {
        if (cookProgressBar == null)
        {
            return;
        }

        cookProgressBar.gameObject.SetActive(true);
    }

    public void HideCookProgress()
    {
        cookTime = 0f;
        if (cookProgressBar == null)
        {
            return;
        }

        cookProgressBar.gameObject.SetActive(false);
    }

    public void SetCookProgress(float normalizedValue)
    {
        if (cookProgressBar == null)
        {
            return;
        }

        cookProgressBar.SetProgress(normalizedValue);
    }

    public void SetBurnProgress(float normalizedValue)
    {
        if (burnProgressBar == null)
        {
            return;
        }

        burnProgressBar.SetProgress(normalizedValue);
    }

    public void AddResultFood(FoodSO resultFood)
    {
        if (resultFood == null) return;
        var food = RemoveFood();
        Destroy(food.gameObject);
        AddFood(resultFood.CreateFood());
    }

    public void SetDone(bool val)
    {
        isDone = val;
    }
}
