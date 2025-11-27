namespace Aspire.DeploymentStrategies;

public class EnvironmentManager
{
    private readonly Dictionary<string, EnvironmentConfiguration> environments;

    public EnvironmentManager()
    {
        environments = new Dictionary<string, EnvironmentConfiguration>
        {
            ["Development"] = new(
                "Development",
                "Feature development and initial testing",
                false,
                TestingLevel.Unit,
                DeploymentFrequency.OnDemand,
                RollbackStrategy.Immediate,
                InfrastructureType.Shared,
                DataStrategy.Synthetic,
                SecurityLevel.Development),
                
            ["Testing"] = new(
                "Testing",
                "Integration and system testing",
                false,
                TestingLevel.Integration,
                DeploymentFrequency.Continuous,
                RollbackStrategy.Immediate,
                InfrastructureType.Shared,
                DataStrategy.Anonymized,
                SecurityLevel.Testing),
                
            ["Staging"] = new(
                "Staging",
                "Production-like testing and validation",
                true,
                TestingLevel.EndToEnd,
                DeploymentFrequency.Scheduled,
                RollbackStrategy.Planned,
                InfrastructureType.Production,
                DataStrategy.ProductionSubset,
                SecurityLevel.Production),
                
            ["Production"] = new(
                "Production",
                "Live customer-facing environment",
                true,
                TestingLevel.Smoke,
                DeploymentFrequency.Controlled,
                RollbackStrategy.ChangeControl,
                InfrastructureType.Production,
                DataStrategy.Production,
                SecurityLevel.Production)
        };
    }

    public EnvironmentConfiguration GetEnvironment(string name)
    {
        if (environments.TryGetValue(name, out var config))
        {
            return config;
        }
        
        throw new ArgumentException($"Environment '{name}' not configured", nameof(name));
    }

    public List<EnvironmentConfiguration> GetAllEnvironments() => environments.Values.ToList();
}