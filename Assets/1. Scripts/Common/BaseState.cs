// Owned by MinJun Lee
using UnityEngine;

/// <summary>
/// State pattern base class.
/// </summary>
public abstract class BaseState<T> : IState
{
    protected T controller; // state owner

    public BaseState(T controller)
    {
        this.controller = controller;
    }

    public abstract void Enter();
    public abstract void UpdateState();
    public abstract void Exit();
}
