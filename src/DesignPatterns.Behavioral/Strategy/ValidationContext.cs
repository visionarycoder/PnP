namespace Snippets.DesignPatterns.Behavioral.Strategy;

/// <summary>
/// Validation context for applying different validation strategies
/// </summary>
public class ValidationContext<T>
{
    public ValidationSummary ValidateWithMultipleStrategies(T data, params IValidationStrategy<T>[] strategies)
    {
        var results = new List<ValidationResult>();

        foreach (var strategy in strategies)
        {
            var result = strategy.Validate(data);
            results.Add(result);

            Console.WriteLine($"🔍 {strategy.Name}: {(result.IsValid ? "✅ VALID" : "❌ INVALID")}");
            Console.WriteLine($"   Message: {result.Message}");
            if (result.HasWarnings && result.Warnings != null)
            {
                foreach (var warning in result.Warnings)
                {
                    Console.WriteLine($"   ⚠️  Warning: {warning}");
                }
            }
        }

        var allValid = results.All(r => r.IsValid);
        var allWarnings = results.SelectMany(r => r.Warnings ?? []).ToArray();

        return new ValidationSummary(results.ToArray(), allValid, allWarnings);
    }
}