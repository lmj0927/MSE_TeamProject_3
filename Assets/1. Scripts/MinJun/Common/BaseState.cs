using UnityEngine;

public abstract class BaseState<T> : IState
{
    protected T controller;

    public BaseState(T controller)
    {
        this.controller = controller;
    }

    public abstract void Enter();
    public abstract void UpdateState();
    public abstract void Exit();
}