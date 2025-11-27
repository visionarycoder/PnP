namespace Aspire.HealthMonitoring;

public class HealthMonitoringService
{
    private readonly Dictionary<string, Func<HealthCheckResult>> _healthChecks = new();

    public void RegisterComponent(string name, Func<HealthCheckResult> healthCheck)
    {
        _healthChecks[name] = healthCheck;
    }

    public OverallHealthStatus GetOverallHealth()
    {
        var results = _healthChecks.Values.Select(check => check()).ToList();
        var total = results.Count;
        var healthy = results.Count(r => r.Status == HealthStatus.Healthy);
        var degraded = results.Count(r => r.Status == HealthStatus.Degraded);
        var unhealthy = results.Count(r => r.Status == HealthStatus.Unhealthy);

        var overallStatus = unhealthy > 0 ? HealthStatus.Unhealthy :
            degraded > 0 ? HealthStatus.Degraded : HealthStatus.Healthy;

        return new OverallHealthStatus(overallStatus, total, healthy, degraded, unhealthy);
    }
}