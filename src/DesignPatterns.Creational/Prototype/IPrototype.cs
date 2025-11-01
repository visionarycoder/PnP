namespace Snippets.DesignPatterns.Creational.Prototype;

/// <summary>
/// Abstract prototype interface
/// </summary>
public interface IPrototype<T>
{
    T Clone();
}