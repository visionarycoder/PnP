using System.Runtime.Serialization;

namespace CSharp.ExceptionHandling;

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