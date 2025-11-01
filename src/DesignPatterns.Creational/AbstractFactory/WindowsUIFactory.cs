namespace Snippets.DesignPatterns.Creational.AbstractFactory;

public class WindowsUiFactory : IUiFactory
{
    public IButton CreateButton() => new WindowsButton();
    public ICheckbox CreateCheckbox() => new WindowsCheckbox();
    public ITextField CreateTextField() => new WindowsTextField();
}