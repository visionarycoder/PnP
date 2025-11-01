namespace Snippets.DesignPatterns.Behavioral.Observer;

public interface ISubject<T>
{
    void Subscribe(IObserver<T> observer);
    void Unsubscribe(IObserver<T> observer);
    void NotifyObservers();
}