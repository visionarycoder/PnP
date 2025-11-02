namespace CSharp.StringTruncate;

/// <summary>
/// String extension methods for truncation and manipulation.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Truncates a string to the specified maximum length and adds ellipsis if truncated.
    /// </summary>
    /// <param name="value">The string to truncate</param>
    /// <param name="maxLength">Maximum length of the result (including ellipsis)</param>
    /// <returns>Truncated string with ellipsis if needed</returns>
    public static string Truncate(this string value, int maxLength)
    {
        const string ellipsis = "...";
        
        if (string.IsNullOrEmpty(value))
            return value;
            
        if (maxLength <= 0)
            throw new ArgumentException("Max length must be greater than 0", nameof(maxLength));
            
        if (value.Length <= maxLength)
            return value;
            
        // Reserve characters for ellipsis
        int truncateAt = maxLength - ellipsis.Length;
        if (truncateAt <= 0)
            return ellipsis;
            
        return value.Substring(0, truncateAt) + ellipsis;
    }
}