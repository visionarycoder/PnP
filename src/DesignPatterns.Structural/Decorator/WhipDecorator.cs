namespace Snippets.DesignPatterns.Structural.Decorator;

public class WhipDecorator(IBeverage beverage) : CondimentDecorator(beverage)
{
    public override string GetDescription()
    {
        return $"{Beverage.GetDescription()}, Whip";
    }

    public override decimal GetCost()
    {
        return Beverage.GetCost() + 0.70m;
    }

    public override int GetCalories()
    {
        return Beverage.GetCalories() + 50;
    }
}