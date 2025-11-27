namespace Aspire.DeploymentStrategies;

public record PromotionRequest(
    string SourceEnvironment,
    string TargetEnvironment,
    string ApplicationVersion,
    string RequestedBy);