namespace Snippets.DesignPatterns.Behavioral.State;

/// <summary>
/// Base state interface for state machines
/// </summary>
public interface IState<T>
{
    string StateName { get; }
    void Enter(T context);
    void Exit(T context);
    void Handle(T context);
}