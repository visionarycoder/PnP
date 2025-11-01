namespace Snippets.DesignPatterns.Creational.AbstractFactory;

public class LinuxCheckbox : ICheckbox
{
    public void Render()
    {
        Console.WriteLine("Rendering Linux GTK-style checkbox");
    }

    public void Toggle()
    {
        Console.WriteLine("Linux checkbox toggled");
    }
}