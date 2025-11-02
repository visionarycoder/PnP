using CSharp.StringTruncate;

namespace CSharp.StringTruncate;

class Program
{
    static void Main()
    {
        Console.WriteLine("=== String Truncate Demo ===\n");

        string longText = "This is a very long string that needs to be truncated";
        
        // Truncate to 20 characters
        string result = longText.Truncate(20);
        Console.WriteLine($"Original: {longText}");
        Console.WriteLine($"Truncated (20): {result}");
        Console.WriteLine($"Length: {result.Length}\n");
        
        // Short string - no truncation
        string shortText = "Short";
        Console.WriteLine($"Short text: '{shortText}'");
        Console.WriteLine($"Truncated (20): '{shortText.Truncate(20)}'");
        Console.WriteLine($"No change needed\n");
        
        // Edge cases
        Console.WriteLine("=== Edge Cases ===");
        
        // Null handling
        string? nullText = null;
        Console.WriteLine($"Null text truncated: {nullText?.Truncate(20) ?? "null"}");
        
        // Empty string
        string emptyText = "";
        Console.WriteLine($"Empty text truncated: '{emptyText.Truncate(20)}'");
        
        // Very short max length
        Console.WriteLine($"Very short limit (5): '{longText.Truncate(5)}'");
        
        // Max length shorter than ellipsis
        Console.WriteLine($"Shorter than ellipsis (2): '{longText.Truncate(2)}'");
        
        // Test error condition
        try
        {
            longText.Truncate(0);
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Expected error for 0 length: {ex.Message}");
        }
    }
}