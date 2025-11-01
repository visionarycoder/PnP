namespace Snippets.DesignPatterns.Behavioral.Observer;

public class WeakObserverCollection<T>
{
    private readonly List<WeakReference<IObserver<T>>> observers = [];

    public void Subscribe(IObserver<T> observer)
    {
        // Remove dead references first
        CleanupDeadReferences();

        // Check if already subscribed
        if (!IsSubscribed(observer))
        {
            observers.Add(new WeakReference<IObserver<T>>(observer));
        }
    }

    public void Unsubscribe(IObserver<T> observer)
    {
        for (int i = observers.Count - 1; i >= 0; i--)
        {
            if (observers[i].TryGetTarget(out var existingObserver) &&
                ReferenceEquals(existingObserver, observer))
            {
                observers.RemoveAt(i);
                break;
            }
        }
    }

    public void NotifyObservers(T data)
    {
        var livingObservers = new List<IObserver<T>>();

        for (int i = observers.Count - 1; i >= 0; i--)
        {
            if (observers[i].TryGetTarget(out var observer))
            {
                livingObservers.Add(observer);
            }
            else
            {
                // Remove dead reference
                observers.RemoveAt(i);
            }
        }

        foreach (var observer in livingObservers)
        {
            try
            {
                observer.Update(data);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error notifying observer {observer.Name}: {ex.Message}");
            }
        }
    }

    private bool IsSubscribed(IObserver<T> observer)
    {
        foreach (var weakRef in observers)
        {
            if (weakRef.TryGetTarget(out var existingObserver) &&
                ReferenceEquals(existingObserver, observer))
            {
                return true;
            }
        }

        return false;
    }

    private void CleanupDeadReferences()
    {
        for (int i = observers.Count - 1; i >= 0; i--)
        {
            if (!observers[i].TryGetTarget(out _))
            {
                observers.RemoveAt(i);
            }
        }
    }

    public int ActiveObserverCount
    {
        get
        {
            CleanupDeadReferences();
            return observers.Count;
        }
    }
}