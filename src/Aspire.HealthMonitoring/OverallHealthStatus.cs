namespace Aspire.HealthMonitoring;

public record OverallHealthStatus(HealthStatus Status, int TotalComponents, int HealthyComponents, int DegradedComponents, int UnhealthyComponents);