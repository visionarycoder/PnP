namespace Snippets.DesignPatterns.Creational.AbstractFactory;

public class MacOsButton : IButton
{
    public void Render()
    {
        Console.WriteLine("Rendering macOS-style button");
    }

    public void Click()
    {
        Console.WriteLine("macOS button clicked - smooth animation");
    }
}