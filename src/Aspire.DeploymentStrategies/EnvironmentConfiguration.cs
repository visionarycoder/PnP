namespace Aspire.DeploymentStrategies;

public record EnvironmentConfiguration(
    string Name,
    string Purpose,
    bool ApprovalRequired,
    TestingLevel AutomatedTesting,
    DeploymentFrequency DeploymentFrequency,
    RollbackStrategy RollbackStrategy,
    InfrastructureType InfrastructureType,
    DataStrategy DataStrategy,
    SecurityLevel SecurityLevel);