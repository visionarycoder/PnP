namespace Snippets.DesignPatterns.Behavioral.Memento;

/// <summary>
/// Auto-save caretaker with configurable intervals
/// </summary>
public class AutoSaveCaretaker<T> : ICaretaker<T>, IDisposable where T : IMemento
{
    private readonly MementoCaretaker<T> caretaker;
    private readonly Timer autoSaveTimer;
    private readonly TimeSpan autoSaveInterval;
    private Func<T>? mementoFactory;

    public AutoSaveCaretaker(TimeSpan autoSaveInterval, int maxHistorySize = 50)
    {
        caretaker = new MementoCaretaker<T>(maxHistorySize);
        this.autoSaveInterval = autoSaveInterval;
        autoSaveTimer = new Timer(AutoSave, null, Timeout.Infinite, Timeout.Infinite);
    }

    public void SetMementoFactory(Func<T> factory)
    {
        mementoFactory = factory;
    }

    public void StartAutoSave()
    {
        autoSaveTimer.Change(autoSaveInterval, autoSaveInterval);
        Console.WriteLine($"Auto-save started with interval: {autoSaveInterval.TotalSeconds}s");
    }

    public void StopAutoSave()
    {
        autoSaveTimer.Change(Timeout.Infinite, Timeout.Infinite);
        Console.WriteLine("Auto-save stopped");
    }

    private void AutoSave(object? state)
    {
        if (mementoFactory != null)
        {
            var memento = mementoFactory();
            SaveMemento(memento, "Auto-save");
        }
    }

    public void SaveMemento(T memento, string? label = null)
    {
        caretaker.SaveMemento(memento, label);
    }

    public T? RestoreMemento(int index) => caretaker.RestoreMemento(index);
    public T? RestoreLatest() => caretaker.RestoreLatest();
    public IReadOnlyList<T> GetHistory() => caretaker.GetHistory();
    public void ClearHistory() => caretaker.ClearHistory();

    public void Dispose()
    {
        autoSaveTimer?.Dispose();
    }
}