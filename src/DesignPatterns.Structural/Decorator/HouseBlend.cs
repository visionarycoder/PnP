namespace Snippets.DesignPatterns.Structural.Decorator;

public class HouseBlend(string size = "Medium") : IBeverage
{
    public string GetDescription() => $"{size} House Blend Coffee";

    public decimal GetCost() => size switch
    {
        "Small" => 1.49m,
        "Medium" => 1.89m,
        "Large" => 2.29m,
        _ => 1.89m
    };

    public int GetCalories() => size switch
    {
        "Small" => 2,
        "Medium" => 3,
        "Large" => 5,
        _ => 3
    };

    public string GetSize() => size;
}