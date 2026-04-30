using UnityEngine;

public class GrillCounter_GrillState : BaseState<GrillCounter>
{

    private float elapsedTime;
    public GrillCounter_GrillState(GrillCounter controller) : base(controller)
    {

    }

    public override void Enter()
    {
        controller.ShowGrillProgress();
    }

    public override void UpdateState()
    {
        elapsedTime += Time.deltaTime;
        controller.SetGrillProgress(elapsedTime / controller.GrillTime);
        if (elapsedTime >= controller.GrillTime)
        {
            controller.SetState(controller.BurnState);
        }
    }

    public override void Exit()
    {
        elapsedTime = 0f;
        //TODO : Grill Food Spawn
    }

}
