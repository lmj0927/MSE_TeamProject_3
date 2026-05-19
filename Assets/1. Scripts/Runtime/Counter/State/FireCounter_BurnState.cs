// Owned by MinJun Lee
using UnityEngine;

public class FireCounter_BurnState : BaseState<AFireCounter>
{
    private float elapsedTime;
    public FireCounter_BurnState(AFireCounter controller) : base(controller) { }

    public override void Enter()
    {
    }

    public override void UpdateState()
    {
        elapsedTime += Time.deltaTime;
        controller.SetBurnProgress(elapsedTime / controller.BurnTime);
        if (elapsedTime >= controller.BurnTime)
        {
            controller.AddResultFood(RecipeManager.Instance.GetTrashFood());
            controller.SetState(controller.NoneState);
        }
    }

    public override void Exit()
    {   
        elapsedTime = 0f;
    }
}
