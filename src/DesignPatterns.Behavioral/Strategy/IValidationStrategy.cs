namespace Snippets.DesignPatterns.Behavioral.Strategy;

/// <summary>
/// Data validation strategy interface
/// </summary>
public interface IValidationStrategy<T>
{
    string Name { get; }
    ValidationResult Validate(T data);
}