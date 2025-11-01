namespace Snippets.DesignPatterns.Behavioral.Memento;

/// <summary>
/// Memento interface for capturing object state
/// </summary>
public interface IMemento
{
    DateTime CreatedAt { get; }
    string Description { get; }
}