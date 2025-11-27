namespace Aspire.HealthMonitoring;

public record ComponentHealthStatus(string Name, HealthStatus Status, string Description, DateTime LastChecked, bool IsCritical);