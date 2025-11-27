namespace Aspire.DeploymentStrategies;

public class DeploymentStrategyRegistry
{
    private readonly Dictionary<string, IDeploymentStrategy> strategies = new();

    public DeploymentStrategyRegistry(IEnumerable<IDeploymentStrategy> availableStrategies)
    {
        foreach (var strategy in availableStrategies)
        {
            strategies[strategy.StrategyName] = strategy;
        }
    }

    public IDeploymentStrategy GetStrategy(string name)
    {
        if (strategies.TryGetValue(name, out var strategy))
        {
            return strategy;
        }
        
        throw new InvalidOperationException($"Deployment strategy '{name}' not found");
    }

    public IDeploymentStrategy RecommendStrategy(DeploymentContext context)
    {
        return context switch
        {
            { Environment: "Production" } when context.Compliance.RequiresApprovalGates => 
                GetStrategy("BlueGreen"),
            { Environment: "Production" } when context.Components.Any(c => !c.SupportsZeroDowntime) => 
                GetStrategy("Rolling"),
            { Environment: "Production" } => 
                GetStrategy("Canary"),
            { Environment: "Staging" } => 
                GetStrategy("BlueGreen"),
            { Environment: "Development" } => 
                GetStrategy("Rolling"),
            _ => GetStrategy("Rolling")
        };
    }

    public List<IDeploymentStrategy> GetAvailableStrategies() => 
        strategies.Values.ToList();
}