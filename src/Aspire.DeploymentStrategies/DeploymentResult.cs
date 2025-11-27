namespace Aspire.DeploymentStrategies;

public record DeploymentResult(
    bool Success,
    string Message,
    TimeSpan Duration,
    List<string> ExecutedSteps);