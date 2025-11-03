# String Truncate

**Description**: Safely truncate a string to a specified maximum length and add ellipsis if truncated.

**Language/Technology**: C# / .NET 8.0

**Code**:

```csharp
public static class StringExtensions
{
    /// <summary>
    /// Truncates a string to the specified maximum length and adds ellipsis if truncated.
    /// </summary>
    /// <param name="value">The string to truncate</param>
    /// <param name="maxLength">Maximum length of the result (including ellipsis)</param>
    /// <returns>Truncated string with ellipsis if needed</returns>
    public static string Truncate(this string? value, int maxLength)
    {
        const string ellipsis = "...";
        
        if (string.IsNullOrEmpty(value))
            return value ?? string.Empty;
            
        if (maxLength <= 0)
            throw new ArgumentException("Max length must be greater than 0", nameof(maxLength));
            
        if (value.Length <= maxLength)
            return value;
            
        // Reserve characters for ellipsis
        var truncateAt = maxLength - ellipsis.Length;
        if (truncateAt <= 0)
            return ellipsis;
            
        return string.Concat(value.AsSpan(0, truncateAt), ellipsis);
    }
}
```

**Usage**:

```csharp
using System;

class Program
{
    static void Main()
    {
        string longText = "This is a very long string that needs to be truncated";
        
        // Truncate to 20 characters
        string result = longText.Truncate(20);
        Console.WriteLine(result);
        // Output: "This is a very lo..."
        
        // Short string - no truncation
        string shortText = "Short";
        Console.WriteLine(shortText.Truncate(20));
        // Output: "Short"
        
        // Null or empty handling
        string? nullText = null;
        Console.WriteLine(nullText.Truncate(20));
        // Output: null
    }
}
```

**Notes**:

- Targets .NET 8.0 SDK with modern C# features
- Uses `AsSpan()` and `string.Concat()` for better performance
- Implemented as an extension method for convenient usage
- Handles null and empty strings safely with nullable annotations
- Ellipsis counts toward the maximum length
- Throws ArgumentException if maxLength is 0 or negative
- Uses `var` only when type is obvious (truncateAt variable)
- Related snippets: [String Helpers](string-helpers.md)
