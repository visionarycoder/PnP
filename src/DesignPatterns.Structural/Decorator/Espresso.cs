namespace Snippets.DesignPatterns.Structural.Decorator;

public class Espresso(string size = "Medium") : IBeverage
{
    public string GetDescription() => $"{size} Espresso";

    public decimal GetCost() => size switch
    {
        "Small" => 1.99m,
        "Medium" => 2.49m,
        "Large" => 2.99m,
        _ => 2.49m
    };

    public int GetCalories() => size switch
    {
        "Small" => 5,
        "Medium" => 8,
        "Large" => 12,
        _ => 8
    };

    public string GetSize() => size;
}