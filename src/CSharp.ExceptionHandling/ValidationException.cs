using System.Runtime.Serialization;
using System.Text.Json;

namespace CSharp.ExceptionHandling;

/// <summary>
/// Exception for validation errors with detailed error information.
/// </summary>
[Serializable]
public class ValidationException : DomainException
{
    public IReadOnlyList<ValidationError> ValidationErrors { get; }

    public ValidationException(
        string message, 
        IEnumerable<ValidationError>? validationErrors = null,
        Exception? innerException = null,
        string? correlationId = null)
        : base("VALIDATION_ERROR", "Validation", message, innerException, correlationId)
    {
        ValidationErrors = validationErrors?.ToList() ?? new List<ValidationError>();
    }

    protected ValidationException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
        var errorsJson = info.GetString(nameof(ValidationErrors));
        ValidationErrors = string.IsNullOrEmpty(errorsJson) 
            ? new List<ValidationError>()
            : JsonSerializer.Deserialize<List<ValidationError>>(errorsJson) ?? new List<ValidationError>();
    }

    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);
        info.AddValue(nameof(ValidationErrors), JsonSerializer.Serialize(ValidationErrors));
    }
}