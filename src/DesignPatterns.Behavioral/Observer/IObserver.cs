namespace Snippets.DesignPatterns.Behavioral.Observer;

public interface IObserver<T>
{
    void Update(T data);
    string Name { get; }
}