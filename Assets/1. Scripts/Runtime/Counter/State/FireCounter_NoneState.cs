// Owned by MinJun Lee
using UnityEngine;

/// <summary>
/// Idle state when counter is not cooking.
/// </summary>
public class FireCounter_NoneState : BaseState<AFireCounter>
{
    public FireCounter_NoneState(AFireCounter controller) : base(controller)
    {
    }

    public override void Enter()
    {
        // hide cook progress UI on all clients
        // controller.HideCookProgress();
        // controller.SetCookProgress(0f);
        // controller.SetBurnProgress(0f);
        controller.showCookProgress = false; // call callback
    }

    public override void UpdateState()
    {
        // no update while idle
    }

    public override void Exit()
    {
    }
}
