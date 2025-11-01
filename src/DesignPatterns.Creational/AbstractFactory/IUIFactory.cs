namespace Snippets.DesignPatterns.Creational.AbstractFactory;

public interface IUiFactory
{
    IButton CreateButton();
    ICheckbox CreateCheckbox();
    ITextField CreateTextField();
}