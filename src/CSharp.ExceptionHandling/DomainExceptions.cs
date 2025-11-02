using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Runtime.Serialization;
using System.Text.Json;

namespace CSharp.ExceptionHandling;

/// <summary>
/// Base exception class for structured error handling with rich diagnostic information.
/// </summary>
[Serializable]
public abstract class DomainException : Exception
{
    public string ErrorCode { get; }
    public string ErrorCategory { get; }
    public Dictionary<string, object> ErrorData { get; }
    public DateTime Timestamp { get; }
    public string CorrelationId { get; }

    protected DomainException(
        string errorCode, 
        string errorCategory, 
        string message, 
        Exception? innerException = null,
        string? correlationId = null) 
        : base(message, innerException)
    {
        ErrorCode = errorCode ?? throw new ArgumentNullException(nameof(errorCode));
        ErrorCategory = errorCategory ?? throw new ArgumentNullException(nameof(errorCategory));
        ErrorData = new Dictionary<string, object>();
        Timestamp = DateTime.UtcNow;
        CorrelationId = correlationId ?? Guid.NewGuid().ToString();
    }

    protected DomainException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
        ErrorCode = info.GetString(nameof(ErrorCode)) ?? "";
        ErrorCategory = info.GetString(nameof(ErrorCategory)) ?? "";
        Timestamp = info.GetDateTime(nameof(Timestamp));
        CorrelationId = info.GetString(nameof(CorrelationId)) ?? "";
        
        var errorDataJson = info.GetString(nameof(ErrorData));
        ErrorData = string.IsNullOrEmpty(errorDataJson) 
            ? new Dictionary<string, object>() 
            : JsonSerializer.Deserialize<Dictionary<string, object>>(errorDataJson) ?? new Dictionary<string, object>();
    }

    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);
        info.AddValue(nameof(ErrorCode), ErrorCode);
        info.AddValue(nameof(ErrorCategory), ErrorCategory);
        info.AddValue(nameof(Timestamp), Timestamp);
        info.AddValue(nameof(CorrelationId), CorrelationId);
        info.AddValue(nameof(ErrorData), JsonSerializer.Serialize(ErrorData));
    }

    public DomainException WithData(string key, object value)
    {
        ErrorData[key] = value;
        return this;
    }

    public T GetData<T>(string key, T defaultValue = default(T)!)
    {
        if (ErrorData.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return defaultValue;
    }
}

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

/// <summary>
/// Represents a single validation error.
/// </summary>
public record ValidationError(
    string PropertyName,
    string ErrorMessage,
    object? AttemptedValue = null,
    string? ErrorCode = null);

/// <summary>
/// Exception for business rule violations.
/// </summary>
[Serializable]
public class BusinessRuleException : DomainException
{
    public string RuleName { get; }

    public BusinessRuleException(
        string ruleName,
        string message,
        Exception? innerException = null,
        string? correlationId = null)
        : base("BUSINESS_RULE_VIOLATION", "BusinessRule", message, innerException, correlationId)
    {
        RuleName = ruleName ?? throw new ArgumentNullException(nameof(ruleName));
    }

    protected BusinessRuleException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
        RuleName = info.GetString(nameof(RuleName)) ?? "";
    }

    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);
        info.AddValue(nameof(RuleName), RuleName);
    }
}

/// <summary>
/// Exception for external service failures.
/// </summary>
[Serializable]
public class ExternalServiceException : DomainException
{
    public string ServiceName { get; }
    public int? HttpStatusCode { get; }
    public string? ServiceResponse { get; }

    public ExternalServiceException(
        string serviceName,
        string message,
        int? httpStatusCode = null,
        string? serviceResponse = null,
        Exception? innerException = null,
        string? correlationId = null)
        : base("EXTERNAL_SERVICE_ERROR", "ExternalService", message, innerException, correlationId)
    {
        ServiceName = serviceName ?? throw new ArgumentNullException(nameof(serviceName));
        HttpStatusCode = httpStatusCode;
        ServiceResponse = serviceResponse;
    }

    protected ExternalServiceException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
        ServiceName = info.GetString(nameof(ServiceName)) ?? "";
        HttpStatusCode = info.GetInt32(nameof(HttpStatusCode));
        ServiceResponse = info.GetString(nameof(ServiceResponse));
    }

    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);
        info.AddValue(nameof(ServiceName), ServiceName);
        info.AddValue(nameof(HttpStatusCode), HttpStatusCode ?? 0);
        info.AddValue(nameof(ServiceResponse), ServiceResponse);
    }
}