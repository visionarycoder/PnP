namespace Snippets.DesignPatterns.Creational.AbstractFactory;

public class LinuxButton : IButton
{
    public void Render()
    {
        Console.WriteLine("Rendering Linux GTK-style button");
    }

    public void Click()
    {
        Console.WriteLine("Linux button clicked");
    }
}