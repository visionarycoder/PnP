namespace Snippets.DesignPatterns.Creational.AbstractFactory;

public class LinuxTextField : ITextField
{
    public void Render()
    {
        Console.WriteLine("Rendering Linux GTK-style text field");
    }

    public void SetText(string text)
    {
        Console.WriteLine($"Linux text field set to: {text}");
    }
}