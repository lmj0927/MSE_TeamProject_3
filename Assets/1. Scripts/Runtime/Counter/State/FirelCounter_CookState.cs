// Owned by MinJun Lee
using UnityEngine;

public class FireCounter_CookState : BaseState<AFireCounter>
{

    private float elapsedTime;
    private bool finished;
    public FireCounter_CookState(AFireCounter controller) : base(controller)
    {

    }

    public override void Enter()
    {
        // controller.ShowCookProgress();
        controller.showCookProgress = true; // call callback
        finished = false;
    }

    public override void UpdateState()
    {
        if(finished) return;

        elapsedTime += Time.deltaTime;
        if (controller.CookTime <= 0.001f)
        {
            // controller.SetCookProgress(0);
            controller.cookProgress = 0f;
        // } else controller.SetCookProgress(elapsedTime / controller.CookTime);
        } else controller.cookProgress = elapsedTime / controller.CookTime;

        if (elapsedTime >= controller.CookTime)
        {
            finished = true;
            if(controller.HasStateAuthority)
            {
                controller.SetDone(true);
                controller.AddResultFood(controller.resultFood);
                controller.SetState(controller.BurnState);
            }
        }
    }

    public override void Exit()
    {
        elapsedTime = 0f;
    }

}
