namespace Snippets.DesignPatterns.Creational.AbstractFactory;

public class MacOsuiFactory : IUiFactory
{
    public IButton CreateButton() => new MacOsButton();
    public ICheckbox CreateCheckbox() => new MacOsCheckbox();
    public ITextField CreateTextField() => new MacOsTextField();
}