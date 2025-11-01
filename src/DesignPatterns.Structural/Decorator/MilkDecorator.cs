namespace Snippets.DesignPatterns.Structural.Decorator;

public class MilkDecorator(IBeverage beverage) : CondimentDecorator(beverage)
{
    public override string GetDescription()
    {
        return $"{Beverage.GetDescription()}, Milk";
    }

    public override decimal GetCost()
    {
        return Beverage.GetCost() + 0.60m;
    }

    public override int GetCalories()
    {
        return Beverage.GetCalories() + 20;
    }
}