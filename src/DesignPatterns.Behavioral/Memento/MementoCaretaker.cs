namespace Snippets.DesignPatterns.Behavioral.Memento;

/// <summary>
/// Generic caretaker for managing mementos with history
/// </summary>
public class MementoCaretaker<T>(int maxHistorySize = 50) : ICaretaker<T>
    where T : IMemento
{
    private readonly List<(T Memento, string Label, DateTime SavedAt)> history = [];

    public void SaveMemento(T memento, string? label = null)
    {
        var saveLabel = label ?? $"Save {history.Count + 1}";
        history.Add((memento, saveLabel, DateTime.UtcNow));

        // Maintain max history size
        while (history.Count > maxHistorySize)
        {
            history.RemoveAt(0);
        }

        Console.WriteLine($"Memento saved: {saveLabel} ({memento.Description})");
    }

    public T? RestoreMemento(int index)
    {
        if (index >= 0 && index < history.Count)
        {
            var (memento, label, savedAt) = history[index];
            Console.WriteLine($"Restoring memento {index}: {label}");
            return memento;
        }

        Console.WriteLine($"Invalid memento index: {index}");
        return default(T);
    }

    public T? RestoreLatest()
    {
        if (history.Count > 0)
        {
            return RestoreMemento(history.Count - 1);
        }

        Console.WriteLine("No mementos available");
        return default(T);
    }

    public IReadOnlyList<T> GetHistory()
    {
        return history.Select(h => h.Memento).ToList().AsReadOnly();
    }

    public void ClearHistory()
    {
        var count = history.Count;
        history.Clear();
        Console.WriteLine($"Cleared {count} mementos from history");
    }

    public void PrintHistory()
    {
        Console.WriteLine($"\nMemento History ({history.Count} items):");
        for (int i = 0; i < history.Count; i++)
        {
            var (memento, label, savedAt) = history[i];
            Console.WriteLine($"{i}: {label} - {memento.Description} (Saved: {savedAt:HH:mm:ss})");
        }
    }

    public void DeleteMemento(int index)
    {
        if (index >= 0 && index < history.Count)
        {
            var (_, label, _) = history[index];
            history.RemoveAt(index);
            Console.WriteLine($"Deleted memento: {label}");
        }
    }

    public List<T> GetMementosByTimeRange(DateTime start, DateTime end)
    {
        return history
            .Where(h => h.SavedAt >= start && h.SavedAt <= end)
            .Select(h => h.Memento)
            .ToList();
    }
}