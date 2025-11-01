namespace Snippets.DesignPatterns.Creational.AbstractFactory;

public class MacOsCheckbox : ICheckbox
{
    public void Render()
    {
        Console.WriteLine("Rendering macOS-style checkbox");
    }

    public void Toggle()
    {
        Console.WriteLine("macOS checkbox toggled with animation");
    }
}