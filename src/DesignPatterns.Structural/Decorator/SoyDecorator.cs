namespace Snippets.DesignPatterns.Structural.Decorator;

public class SoyDecorator(IBeverage beverage) : CondimentDecorator(beverage)
{
    public override string GetDescription()
    {
        return $"{Beverage.GetDescription()}, Soy";
    }

    public override decimal GetCost()
    {
        return Beverage.GetCost() + 0.75m;
    }

    public override int GetCalories()
    {
        return Beverage.GetCalories() + 15;
    }
}