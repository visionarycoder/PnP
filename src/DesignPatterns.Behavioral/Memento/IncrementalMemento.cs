namespace Snippets.DesignPatterns.Behavioral.Memento;

/// <summary>
/// Incremental memento that stores only changes from previous state
/// </summary>
public class IncrementalMemento(object? baseState, Dictionary<string, object?> changes, string description)
    : IMemento
{
    public object? BaseState { get; } = baseState;
    public Dictionary<string, object?> Changes { get; } = changes;
    public DateTime CreatedAt { get; } = DateTime.UtcNow;
    public string Description { get; } = description;

    public static IncrementalMemento CreateFromChanges<T>(T currentState, T previousState, string description)
    {
        var changes = new Dictionary<string, object?>();

        var properties = typeof(T).GetProperties();
        foreach (var prop in properties)
        {
            var currentValue = prop.GetValue(currentState);
            var previousValue = prop.GetValue(previousState);

            if (!Equals(currentValue, previousValue))
            {
                changes[prop.Name] = currentValue;
            }
        }

        return new IncrementalMemento(previousState, changes, description);
    }
}