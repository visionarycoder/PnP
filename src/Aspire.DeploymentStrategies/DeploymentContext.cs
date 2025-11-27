namespace Aspire.DeploymentStrategies;

public record DeploymentContext(
    string Environment,
    string ApplicationVersion,
    Dictionary<string, string> Configuration,
    List<ServiceComponent> Components,
    InfrastructureRequirements Infrastructure,
    ComplianceRequirements Compliance);