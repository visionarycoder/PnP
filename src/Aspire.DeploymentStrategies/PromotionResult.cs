namespace Aspire.DeploymentStrategies;

public record PromotionResult(
    Guid PromotionId,
    string SourceEnvironment,
    string TargetEnvironment,
    PromotionStatus Status,
    string Message,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    DeploymentResult? DeploymentResult);