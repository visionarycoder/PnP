using System.Reflection;

namespace Patterns;

/// <summary>
/// Utility class for building dictionaries from class constants.
/// </summary>
public static class ConstantsDictionaryBuilder
{
    /// <summary>
    /// Builds a dictionary from all public static readonly fields and const fields in a given type.
    /// The key is the property/field name, and the value is the value of that field.
    /// </summary>
    /// <typeparam name="T">The type containing the constants.</typeparam>
    /// <returns>A dictionary where keys are field names and values are field values.</returns>
    public static Dictionary<string, object?> BuildFromConstants<T>()
    {
        return BuildFromConstants(typeof(T));
    }

    /// <summary>
    /// Builds a dictionary from all public static readonly fields and const fields in a given type.
    /// The key is the property/field name, and the value is the value of that field.
    /// </summary>
    /// <param name="type">The type containing the constants.</param>
    /// <returns>A dictionary where keys are field names and values are field values.</returns>
    public static Dictionary<string, object?> BuildFromConstants(Type type)
    {
        var dictionary = new Dictionary<string, object?>();

        // Get all public static fields (const and readonly)
        var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static);

        foreach (var field in fields)
        {
            // Include const fields and static readonly fields
            if (field.IsLiteral || (field.IsStatic && field.IsInitOnly))
            {
                var value = field.GetValue(null);
                dictionary[field.Name] = value;
            }
        }

        return dictionary;
    }

    /// <summary>
    /// Builds a dictionary from all public static properties in a given type.
    /// The key is the property name, and the value is the value of that property.
    /// </summary>
    /// <typeparam name="T">The type containing the constants.</typeparam>
    /// <returns>A dictionary where keys are property names and values are property values.</returns>
    public static Dictionary<string, object?> BuildFromProperties<T>()
    {
        return BuildFromProperties(typeof(T));
    }

    /// <summary>
    /// Builds a dictionary from all public static properties in a given type.
    /// The key is the property name, and the value is the value of that property.
    /// </summary>
    /// <param name="type">The type containing the constants.</param>
    /// <returns>A dictionary where keys are property names and values are property values.</returns>
    public static Dictionary<string, object?> BuildFromProperties(Type type)
    {
        var dictionary = new Dictionary<string, object?>();

        // Get all public static properties
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Static);

        foreach (var property in properties)
        {
            if (property.CanRead)
            {
                var value = property.GetValue(null);
                dictionary[property.Name] = value;
            }
        }

        return dictionary;
    }

    /// <summary>
    /// Builds a dictionary from all public static fields, const fields, and properties in a given type.
    /// The key is the member name, and the value is the value of that member.
    /// </summary>
    /// <typeparam name="T">The type containing the constants.</typeparam>
    /// <returns>A dictionary where keys are member names and values are member values.</returns>
    public static Dictionary<string, object?> BuildFromAll<T>()
    {
        return BuildFromAll(typeof(T));
    }

    /// <summary>
    /// Builds a dictionary from all public static fields, const fields, and properties in a given type.
    /// The key is the member name, and the value is the value of that member.
    /// </summary>
    /// <param name="type">The type containing the constants.</param>
    /// <returns>A dictionary where keys are member names and values are member values.</returns>
    public static Dictionary<string, object?> BuildFromAll(Type type)
    {
        var dictionary = BuildFromConstants(type);
        var properties = BuildFromProperties(type);

        foreach (var kvp in properties)
        {
            dictionary[kvp.Key] = kvp.Value;
        }

        return dictionary;
    }
}
