using UnityEngine;
using minjun;
using UnityEngine.Serialization;

public class GrillCounter : ACounter
{
    [SerializeField] private float grillTime;
    [SerializeField] private float burnTime;
    [FormerlySerializedAs("grilProgressBar")]
    [SerializeField] private RadialProgressBar grillProgressBar;
    [SerializeField] private RadialProgressBar burnProgressBar;

    public GrillCounter_NoneState NoneState { get; private set; }
    public GrillCounter_GrillState GrillState { get; private set; }
    public GrillCounter_BurnState BurnState { get; private set; }

    public float GrillTime => grillTime;
    public float BurnTime => burnTime;

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

        NoneState = new GrillCounter_NoneState(this);
        GrillState = new GrillCounter_GrillState(this);
        BurnState = new GrillCounter_BurnState(this);

        stateMachine.ChangeState(NoneState);
    }

    public override void Interact(Player player)
    {
        if (CanAddFood(player))
        {
            AddFood(player.RemoveFood());
            SetState(GrillState);
        }
        else if (CanRemoveFood(player))
        {
            player.AddFood(RemoveFood());
            SetState(NoneState);
        }
    }

    public void SetState(IState newState)
    {
        stateMachine.ChangeState(newState);
    }

    public void ShowGrillProgress()
    {
        if (grillProgressBar == null)
        {
            return;
        }

        grillProgressBar.gameObject.SetActive(true);
    }

    public void HideGrillProgress()
    {
        if (grillProgressBar == null)
        {
            return;
        }

        grillProgressBar.gameObject.SetActive(false);
    }

    public void SetGrillProgress(float normalizedValue)
    {
        if (grillProgressBar == null)
        {
            return;
        }

        grillProgressBar.SetProgress(normalizedValue);
    }

    public void SetBurnProgress(float normalizedValue)
    {
        if (burnProgressBar == null)
        {
            return;
        }

        burnProgressBar.SetProgress(normalizedValue);
    }
}
