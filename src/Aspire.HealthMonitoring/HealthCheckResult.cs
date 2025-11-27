namespace Aspire.HealthMonitoring;

public record HealthCheckResult(HealthStatus Status, string Description)
{
    public static HealthCheckResult Healthy(string description) => new(HealthStatus.Healthy, description);
    public static HealthCheckResult Degraded(string description) => new(HealthStatus.Degraded, description);
    public static HealthCheckResult Unhealthy(string description) => new(HealthStatus.Unhealthy, description);
}