using UnityEngine;

public class GrillCounter_BurnState : BaseState<GrillCounter>
{
    private float elapsedTime;
    public GrillCounter_BurnState(GrillCounter controller) : base(controller) { }

    public override void Enter()
    {
    }

    public override void UpdateState()
    {
        elapsedTime += Time.deltaTime;
        controller.SetBurnProgress(elapsedTime / controller.BurnTime);
        if (elapsedTime >= controller.BurnTime)
        {
            controller.SetState(controller.NoneState);
        }
    }

    public override void Exit()
    {   
        elapsedTime = 0f;
        //TODO : Burn Food Spawn
    }
}
