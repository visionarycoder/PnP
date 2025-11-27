namespace CSharp.Memoization;

/// <summary>
/// Example class for demonstrating method memoization.
/// </summary>
public class Calculator
{
    /// <summary>
    /// Simulates a complex calculation that would benefit from memoization.
    /// </summary>
    public double ComplexCalculation(int a, int b)
    {
        Console.WriteLine($"  Performing complex calculation: {a}, {b}");
        Thread.Sleep(100); // Simulate expensive work
        
        double result = 0;
        for (int i = 0; i < 1000; i++)
        {
            result += Math.Pow(a, 2) * Math.Sin(b + i) + Math.Sqrt(a * b + i);
        }
        
        return Math.Round(result, 4);
    }
}