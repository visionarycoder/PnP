namespace Snippets.DesignPatterns.Behavioral.State;

/// <summary>
/// Simple state machine base class without complex generics
/// </summary>
public abstract class SimpleStateMachine
{
    private readonly List<string> stateHistory = [];

    public ISimpleState? CurrentState { get; private set; }

    public List<string> GetStateHistory() => [..stateHistory];

    public virtual void TransitionTo(ISimpleState newState)
    {
        var previousState = CurrentState?.StateName ?? "None";

        CurrentState?.Exit(this);
        CurrentState = newState;
        CurrentState?.Enter(this);

        stateHistory.Add($"{DateTime.Now:HH:mm:ss} - {previousState} -> {newState.StateName}");
        Console.WriteLine($"State transition: {previousState} -> {newState.StateName}");
    }

    public virtual void Handle()
    {
        CurrentState?.Handle(this);
    }

    protected void SetInitialState(ISimpleState initialState)
    {
        CurrentState = initialState;
        CurrentState?.Enter(this);
        stateHistory.Add($"{DateTime.Now:HH:mm:ss} - Initial state: {initialState.StateName}");
    }
}