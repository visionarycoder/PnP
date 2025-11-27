namespace Aspire.DeploymentStrategies;

public record InfrastructureRequirements(
    CloudProvider Provider,
    string Region,
    bool RequiresMultiRegion);