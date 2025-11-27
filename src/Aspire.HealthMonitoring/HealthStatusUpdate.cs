namespace Aspire.HealthMonitoring;

public record HealthStatusUpdate(string ComponentName, HealthStatus Status, string Message, DateTime Timestamp);