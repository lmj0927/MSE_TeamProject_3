// Owned by MinJun Lee
using UnityEngine;

/// <summary>
/// Manages IState enter, update, exit.
/// </summary>
public class StateMachine
{
    IState currentState; // active state

    public void ChangeState(IState newState)
    {
        // exit previous state before entering new one
        if (currentState != null)
        {
            currentState.Exit();
        }

        currentState = newState;
        currentState.Enter();
    }

    public void Update()
    {
        if (currentState != null)
        {
            currentState.UpdateState();
        }
    }
}
