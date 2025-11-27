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