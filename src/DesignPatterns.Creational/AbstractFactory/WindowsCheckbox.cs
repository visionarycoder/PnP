namespace Snippets.DesignPatterns.Creational.AbstractFactory;

public class WindowsCheckbox : ICheckbox
{
    public void Render()
    {
        Console.WriteLine("Rendering Windows-style checkbox");
    }

    public void Toggle()
    {
        Console.WriteLine("Windows checkbox toggled");
    }
}