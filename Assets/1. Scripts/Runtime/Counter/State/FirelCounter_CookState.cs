// Owned by MinJun Lee
using UnityEngine;

public class FireCounter_CookState : BaseState<AFireCounter>
{

    private float elapsedTime;
    public FireCounter_CookState(AFireCounter controller) : base(controller)
    {

    }

    public override void Enter()
    {
        controller.ShowCookProgress();

    }

    public override void UpdateState()
    {
        elapsedTime += Time.deltaTime;
        if (controller.CookTime <= 0.001f)
        {
            controller.SetCookProgress(0);
        } else controller.SetCookProgress(elapsedTime / controller.CookTime);

        if (elapsedTime >= controller.CookTime)
        {
            controller.SetDone(true);
            controller.AddResultFood(controller.resultFood);
            controller.SetState(controller.BurnState);
        }
    }

    public override void Exit()
    {
        elapsedTime = 0f;
    }

}
