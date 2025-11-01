namespace Snippets.DesignPatterns.Creational.AbstractFactory;

// Abstract Products

// Windows UI Products
public class WindowsButton : IButton
{
    public void Render()
    {
        Console.WriteLine("Rendering Windows-style button");
    }

    public void Click()
    {
        Console.WriteLine("Windows button clicked - showing dialog");
    }
}

// MacOS UI Products

// Linux UI Products

// Abstract Factory Interface

// Concrete Factories

// Client code that uses the factory

// Factory Provider for runtime factory selection