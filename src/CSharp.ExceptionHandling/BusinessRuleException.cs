using System.Runtime.Serialization;

namespace CSharp.ExceptionHandling;

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