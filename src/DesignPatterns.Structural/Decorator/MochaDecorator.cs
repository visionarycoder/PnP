namespace Snippets.DesignPatterns.Structural.Decorator;

public class MochaDecorator(IBeverage beverage) : CondimentDecorator(beverage)
{
    public override string GetDescription()
    {
        return $"{Beverage.GetDescription()}, Mocha";
    }

    public override decimal GetCost()
    {
        return Beverage.GetCost() + 0.80m;
    }

    public override int GetCalories()
    {
        return Beverage.GetCalories() + 30;
    }
}