// Owned by MinJun Lee
using UnityEngine;

public class FireCounter_BurnState : BaseState<AFireCounter>
{
    private float elapsedTime;
    private bool finished;
    public FireCounter_BurnState(AFireCounter controller) : base(controller) { }

    public override void Enter()
    {
        finished = false;
    }

    public override void UpdateState()
    {
        if(finished) return;
        elapsedTime += Time.deltaTime;
        // controller.SetBurnProgress(elapsedTime / controller.BurnTime);
        controller.burnProgress = elapsedTime / controller.BurnTime;
        if (elapsedTime >= controller.BurnTime)
        {
            finished = true;
            controller.AddResultFood(RecipeManager.Instance.GetTrashFood());
            controller.SetState(controller.NoneState);
        }
    }

    public override void Exit()
    {   
        elapsedTime = 0f;
    }
}
