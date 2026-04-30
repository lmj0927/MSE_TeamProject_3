using UnityEngine;

public class GrillCounter_NoneState : BaseState<GrillCounter>
{
    public GrillCounter_NoneState(GrillCounter controller) : base(controller)
    {
    }

    public override void Enter()
    {
        controller.HideGrillProgress();
        controller.SetGrillProgress(0f);
        controller.SetBurnProgress(0f);
    }

    public override void UpdateState()
    {
       
    }

    public override void Exit()
    {
    }
}
