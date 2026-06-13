// Owned by MinJun Lee

/// <summary>
/// State enter, update, exit contract.
/// </summary>
public interface IState
{
    public void Enter();
    public void UpdateState();
    public void Exit();
}
