namespace Aspire.DeploymentStrategies;

public interface IDeploymentStrategy
{
    string StrategyName { get; }
    DeploymentComplexity Complexity { get; }
    TimeSpan TypicalDeploymentTime { get; }
    RiskLevel RiskProfile { get; }
    Task<DeploymentPlan> CreateDeploymentPlanAsync(DeploymentContext context);
    Task<bool> ValidatePrerequisitesAsync(DeploymentContext context);
    Task<DeploymentResult> ExecuteAsync(DeploymentPlan plan, CancellationToken cancellationToken = default);
}