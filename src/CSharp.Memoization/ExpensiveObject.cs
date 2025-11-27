namespace CSharp.Memoization;

/// <summary>
/// Example class for demonstrating object creation caching.
/// </summary>
public class ExpensiveObject
{
    public string Name { get; }
    public DateTime CreatedAt { get; }

    public ExpensiveObject(string name)
    {
        Name = name;
        CreatedAt = DateTime.Now;
        // Simulate expensive initialization
        Thread.Sleep(50);
    }

    public override string ToString() => $"{Name} (created at {CreatedAt:HH:mm:ss.fff})";
}