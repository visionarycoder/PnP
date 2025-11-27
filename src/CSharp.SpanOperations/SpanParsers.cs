using System.Globalization;

namespace CSharp.SpanOperations;

/// <summary>
/// High-performance parsers using spans
/// </summary>
public static class SpanParsers
{
    /// <summary>
    /// Parse CSV line without allocations using a callback for each field
    /// </summary>
    public static void ParseCsvLine(ReadOnlySpan<char> line, Action<ReadOnlySpan<char>, int> fieldProcessor)
    {
        int fieldIndex = 0;
        var remaining = line;
        
        while (!remaining.IsEmpty)
        {
            int commaIndex = remaining.IndexOf(',');
            
            if (commaIndex == -1)
            {
                // Last field
                fieldProcessor(remaining.TrimFast(), fieldIndex);
                break;
            }
            
            fieldProcessor(remaining.Slice(0, commaIndex).TrimFast(), fieldIndex);
            remaining = remaining.Slice(commaIndex + 1);
            fieldIndex++;
        }
    }

    /// <summary>
    /// Parse CSV line and return field count
    /// </summary>
    public static int GetCsvFieldCount(ReadOnlySpan<char> line)
    {
        int count = 0;
        ParseCsvLine(line, (field, index) => count = index + 1);
        return count;
    }

    /// <summary>
    /// Parse key-value pairs (key=value format)
    /// </summary>
    public static bool TryParseKeyValue(ReadOnlySpan<char> line, out ReadOnlySpan<char> key, out ReadOnlySpan<char> value)
    {
        int equalIndex = line.IndexOf('=');
        
        if (equalIndex == -1 || equalIndex == 0 || equalIndex == line.Length - 1)
        {
            key = default;
            value = default;
            return false;
        }
        
        key = line.Slice(0, equalIndex).TrimFast();
        value = line.Slice(equalIndex + 1).TrimFast();
        return true;
    }

    /// <summary>
    /// Parse integers from delimited string
    /// </summary>
    public static void ParseIntegers(ReadOnlySpan<char> text, char delimiter, Span<int> results, out int count)
    {
        count = 0;
        
        foreach (var part in text.Split(delimiter))
        {
            if (count >= results.Length)
                break;
                
            if (int.TryParse(part, out int value))
            {
                results[count++] = value;
            }
        }
    }

    /// <summary>
    /// Parse floating-point numbers
    /// </summary>
    public static void ParseDoubles(ReadOnlySpan<char> text, char delimiter, Span<double> results, out int count)
    {
        count = 0;
        
        foreach (var part in text.Split(delimiter))
        {
            if (count >= results.Length)
                break;
                
            if (double.TryParse(part, out double value))
            {
                results[count++] = value;
            }
        }
    }

    /// <summary>
    /// Parse hex string to bytes
    /// </summary>
    public static bool TryParseHex(ReadOnlySpan<char> hexString, Span<byte> bytes, out int bytesWritten)
    {
        bytesWritten = 0;
        
        if (hexString.Length % 2 != 0)
            return false;
            
        for (int i = 0; i < hexString.Length; i += 2)
        {
            if (bytesWritten >= bytes.Length)
                return false;
                
            var hexByte = hexString.Slice(i, 2);
            if (!byte.TryParse(hexByte, NumberStyles.HexNumber, null, out byte value))
                return false;
                
            bytes[bytesWritten++] = value;
        }
        
        return true;
    }
}