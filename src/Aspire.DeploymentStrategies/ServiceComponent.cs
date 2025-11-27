namespace Aspire.DeploymentStrategies;

public record ServiceComponent(
    string Name,
    string Type, // API, Worker, Database, Cache
    string CurrentVersion,
    string TargetVersion,
    List<string> Dependencies,
    bool SupportsZeroDowntime);