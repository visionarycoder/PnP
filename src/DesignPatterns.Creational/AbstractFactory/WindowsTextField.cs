namespace Snippets.DesignPatterns.Creational.AbstractFactory;

public class WindowsTextField : ITextField
{
    public void Render()
    {
        Console.WriteLine("Rendering Windows-style text field");
    }

    public void SetText(string text)
    {
        Console.WriteLine($"Windows text field set to: {text}");
    }
}