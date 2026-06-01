// Owned by MinJun Lee
using UnityEngine;

public class FireCounter_NoneState : BaseState<AFireCounter>
{
    public FireCounter_NoneState(AFireCounter controller) : base(controller)
    {
    }

    public override void Enter()
    {
        // controller.HideCookProgress();
        // controller.SetCookProgress(0f);
        // controller.SetBurnProgress(0f);
        controller.showCookProgress = false; // call callback
    }

    public override void UpdateState()
    {
       
    }

    public override void Exit()
    {
    }
}
