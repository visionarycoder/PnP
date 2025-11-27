namespace Algorithm.DynamicProgramming;

/// <summary>
/// Represents an item that can be placed in a knapsack.
/// </summary>
public class KnapsackItem(int weight, int value)
{
    public int Weight { get; } = weight;
    public int Value { get; } = value;
    
    public override string ToString() => $"Weight: {Weight}, Value: {Value}";
}