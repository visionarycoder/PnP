namespace Snippets.DesignPatterns.Structural.Decorator;

// Component interface - defines the interface for objects that can have responsibilities added dynamically

// Concrete Component - defines an object to which additional responsibilities can be attached
public class BasicTextProcessor : ITextProcessor
{
    public string Process(string text)
    {
        return text ?? string.Empty;
    }

    public string GetDescription()
    {
        return "Basic Text";
    }

    public int GetProcessingCost()
    {
        return 1;
    }
}

// Base Decorator - maintains a reference to a Component object and defines interface that conforms to Component

// Concrete Decorators - add responsibilities to the component

// Advanced Decorator Example: Coffee Shop

// Beverage decorators