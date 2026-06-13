// Owned by MinJun Lee
using UnityEngine;

/// <summary>
/// Burn timer state after cooking finishes.
/// </summary>
public class FireCounter_BurnState : BaseState<AFireCounter>
{
    private float elapsedTime; // time since burn started
    private bool finished; // burn complete flag

    public FireCounter_BurnState(AFireCounter controller) : base(controller) { }

    public override void Enter()
    {
        finished = false;
    }

    public override void UpdateState()
    {
        if (finished) return;

        // track burn elapsed time
        elapsedTime += Time.deltaTime;
        // controller.SetBurnProgress(elapsedTime / controller.BurnTime);
        controller.burnProgress = elapsedTime / controller.BurnTime;

        // burn timeout → replace food with trash and return to idle
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
