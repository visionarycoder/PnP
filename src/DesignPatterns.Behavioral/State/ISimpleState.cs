namespace Snippets.DesignPatterns.Behavioral.State;

/// <summary>
/// Simple state interface without generic constraints for easier use
/// </summary>
public interface ISimpleState
{
    string StateName { get; }
    void Enter(object context);
    void Exit(object context);
    void Handle(object context);
}