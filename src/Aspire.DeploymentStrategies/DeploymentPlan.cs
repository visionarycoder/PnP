namespace Aspire.DeploymentStrategies;

public record DeploymentPlan(
    Guid Id,
    string StrategyName,
    List<string> Steps,
    TimeSpan EstimatedDuration,
    RiskLevel RiskLevel);