namespace Snippets.DesignPatterns.Structural.Decorator;

public class DarkRoast(string size = "Medium") : IBeverage
{
    public string GetDescription() => $"{size} Dark Roast Coffee";

    public decimal GetCost() => size switch
    {
        "Small" => 1.69m,
        "Medium" => 2.09m,
        "Large" => 2.49m,
        _ => 2.09m
    };

    public int GetCalories() => size switch
    {
        "Small" => 3,
        "Medium" => 5,
        "Large" => 7,
        _ => 5
    };

    public string GetSize() => size;
}