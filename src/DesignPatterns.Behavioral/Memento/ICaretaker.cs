namespace Snippets.DesignPatterns.Behavioral.Memento;

/// <summary>
/// Caretaker interface for managing mementos
/// </summary>
public interface ICaretaker<T> where T : IMemento
{
    void SaveMemento(T memento, string? label = null);
    T? RestoreMemento(int index);
    T? RestoreLatest();
    IReadOnlyList<T> GetHistory();
    void ClearHistory();
}