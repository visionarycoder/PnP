namespace Snippets.DesignPatterns.Structural.Decorator;

public abstract class CondimentDecorator(IBeverage beverage) : IBeverage
{
    protected IBeverage Beverage = beverage ?? throw new ArgumentNullException(nameof(beverage));

    public abstract string GetDescription();
    public abstract decimal GetCost();
    public abstract int GetCalories();

    public string GetSize() => Beverage.GetSize();
}