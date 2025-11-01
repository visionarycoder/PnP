namespace Snippets.DesignPatterns.Creational.AbstractFactory;

public class LinuxUiFactory : IUiFactory
{
    public IButton CreateButton() => new LinuxButton();
    public ICheckbox CreateCheckbox() => new LinuxCheckbox();
    public ITextField CreateTextField() => new LinuxTextField();
}