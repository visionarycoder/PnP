namespace Snippets.DesignPatterns.Creational.AbstractFactory;

public class MacOsTextField : ITextField
{
    public void Render()
    {
        Console.WriteLine("Rendering macOS-style text field");
    }

    public void SetText(string text)
    {
        Console.WriteLine($"macOS text field set to: {text}");
    }
}