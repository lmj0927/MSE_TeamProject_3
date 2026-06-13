// Owned by MinJun Lee
using UnityEngine;

/// <summary>
/// Active cooking timer state.
/// </summary>
public class FireCounter_CookState : BaseState<AFireCounter>
{
    private float elapsedTime; // time since cook started
    private bool finished; // cook complete flag

    public FireCounter_CookState(AFireCounter controller) : base(controller)
    {
    }

    public override void Enter()
    {
        // show cook progress UI on all clients
        // controller.ShowCookProgress();
        controller.showCookProgress = true; // call callback
        finished = false;
    }

    public override void UpdateState()
    {
        if (finished) return;

        elapsedTime += Time.deltaTime;

        // update cook progress bar ratio
        if (controller.CookTime <= 0.001f)
        {
            // controller.SetCookProgress(0);
            controller.cookProgress = 0f;
        // } else controller.SetCookProgress(elapsedTime / controller.CookTime);
        } else controller.cookProgress = elapsedTime / controller.CookTime;

        // cook done → apply recipe result and start burn timer
        if (elapsedTime >= controller.CookTime)
        {
            finished = true;
            if (controller.HasStateAuthority)
            {
                controller.SetDone(true);
                controller.ApplyCookResult();
                controller.SetState(controller.BurnState);
            }
        }
    }

    public override void Exit()
    {
        elapsedTime = 0f;
    }
}
