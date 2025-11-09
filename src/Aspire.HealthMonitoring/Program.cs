namespace Aspire.HealthMonitoring;

// Health monitoring supporting classes and enums
public enum HealthStatus
{
    Healthy,
    Degraded,
    Unhealthy
}

public record HealthCheckResult(HealthStatus Status, string Description)
{
    public static HealthCheckResult Healthy(string description) => new(HealthStatus.Healthy, description);
    public static HealthCheckResult Degraded(string description) => new(HealthStatus.Degraded, description);
    public static HealthCheckResult Unhealthy(string description) => new(HealthStatus.Unhealthy, description);
}

public record ComponentHealthStatus(string Name, HealthStatus Status, string Description, DateTime LastChecked, bool IsCritical);

public record OverallHealthStatus(HealthStatus Status, int TotalComponents, int HealthyComponents, int DegradedComponents, int UnhealthyComponents);

public record HealthStatusUpdate(string ComponentName, HealthStatus Status, string Message, DateTime Timestamp);

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

/// <summary>
/// Demonstrates enterprise health monitoring and observability patterns with .NET Aspire
/// Including predictive analytics, intelligent alerting, SLA monitoring, automated remediation,
/// and compliance reporting for distributed applications
/// </summary>
public static class Program
{
    public static void Main()
    {
        Console.WriteLine("Enterprise Health Monitoring & Observability with .NET Aspire");
        Console.WriteLine("=============================================================");

        HealthMonitoringServiceExample();
        ComponentHealthCheckExample();
        RealTimeHealthWatchingExample();
        HealthHistoryTrackingExample();
        MetricsAndObservabilityExample();
        AlertingAndRemediationExample();
        SLAMonitoringExample();
        ComplianceReportingExample();

        Console.WriteLine("\nHealth monitoring demonstration completed!");
        Console.WriteLine("Key patterns demonstrated:");
        Console.WriteLine("- Distributed health monitoring with .NET Aspire");
        Console.WriteLine("- Real-time health status watching");
        Console.WriteLine("- Health history tracking and trend analysis");
        Console.WriteLine("- OpenTelemetry metrics integration");
        Console.WriteLine("- Intelligent alerting and automated remediation");
        Console.WriteLine("- SLA monitoring and compliance reporting");
    }

    private static void HealthMonitoringServiceExample()
    {
        Console.WriteLine("1. Health Monitoring Service:");
        Console.WriteLine("   // Distributed health monitoring service");
        Console.WriteLine("   builder.Services.AddSingleton<IHealthMonitoringService, DistributedHealthMonitoringService>();");

        var healthMonitor = new HealthMonitoringService();
        
        healthMonitor.RegisterComponent("DocumentAPI", () => HealthCheckResult.Healthy("API is responsive"));
        healthMonitor.RegisterComponent("Database", () => HealthCheckResult.Degraded("High latency detected"));
        healthMonitor.RegisterComponent("Cache", () => HealthCheckResult.Healthy("Cache hit ratio: 95%"));
        healthMonitor.RegisterComponent("MessageQueue", () => HealthCheckResult.Unhealthy("Queue overflow detected"));

        var overallHealth = healthMonitor.GetOverallHealth();
        Console.WriteLine($"   Overall Health Status: {overallHealth.Status}");
        Console.WriteLine($"   Total Components: {overallHealth.TotalComponents}");
        Console.WriteLine($"   Healthy: {overallHealth.HealthyComponents}, Degraded: {overallHealth.DegradedComponents}, Unhealthy: {overallHealth.UnhealthyComponents}");
        Console.WriteLine("");
    }

    private static void ComponentHealthCheckExample()
    {
        Console.WriteLine("2. Component Health Checks:");
        
        var components = new Dictionary<string, ComponentHealthStatus>
        {
            ["DocumentAPI"] = new("DocumentAPI", HealthStatus.Healthy, "Response time: 45ms", DateTime.Now, true),
            ["PostgreSQL"] = new("PostgreSQL", HealthStatus.Degraded, "High CPU usage: 85%", DateTime.Now, true),
            ["Redis"] = new("Redis", HealthStatus.Healthy, "Memory usage: 60%", DateTime.Now, false),
            ["RabbitMQ"] = new("RabbitMQ", HealthStatus.Unhealthy, "Connection timeout", DateTime.Now, true),
            ["BlobStorage"] = new("BlobStorage", HealthStatus.Healthy, "Available space: 2TB", DateTime.Now, false)
        };

        Console.WriteLine("   Component Status:");
        foreach (var (name, status) in components)
        {
            var icon = status.Status switch
            {
                HealthStatus.Healthy => "✅",
                HealthStatus.Degraded => "⚠️",
                HealthStatus.Unhealthy => "❌",
                _ => "❓"
            };
            var critical = status.IsCritical ? " [CRITICAL]" : "";
            Console.WriteLine($"     {icon} {name}: {status.Status} - {status.Description}{critical}");
        }
        Console.WriteLine("");
    }

    private static void RealTimeHealthWatchingExample()
    {
        Console.WriteLine("3. Real-Time Health Watching:");
        
        var healthUpdates = new[]
        {
            new HealthStatusUpdate("DocumentAPI", HealthStatus.Healthy, "Normal operation", DateTime.Now),
            new HealthStatusUpdate("Database", HealthStatus.Degraded, "Query timeout increased", DateTime.Now.AddSeconds(5)),
            new HealthStatusUpdate("MessageQueue", HealthStatus.Unhealthy, "Queue overflow", DateTime.Now.AddSeconds(10)),
            new HealthStatusUpdate("Database", HealthStatus.Healthy, "Performance improved", DateTime.Now.AddSeconds(15))
        };

        Console.WriteLine("   Recent Health Updates:");
        foreach (var update in healthUpdates)
        {
            var icon = update.Status switch
            {
                HealthStatus.Healthy => "✅",
                HealthStatus.Degraded => "⚠️",
                HealthStatus.Unhealthy => "❌",
                _ => "❓"
            };
            Console.WriteLine($"     {update.Timestamp:HH:mm:ss} | {icon} {update.ComponentName}: {update.Message}");
        }
        Console.WriteLine("");
    }

    private static void HealthHistoryTrackingExample()
    {
        Console.WriteLine("4. Health History Tracking:");
        
        var healthHistory = new List<(DateTime Time, string Component, HealthStatus Status, double ResponseTime)>
        {
            (DateTime.Now.AddHours(-24), "DocumentAPI", HealthStatus.Healthy, 42.5),
            (DateTime.Now.AddHours(-20), "DocumentAPI", HealthStatus.Healthy, 38.2),
            (DateTime.Now.AddHours(-16), "DocumentAPI", HealthStatus.Degraded, 156.8),
            (DateTime.Now.AddHours(-12), "DocumentAPI", HealthStatus.Unhealthy, 2500.0),
            (DateTime.Now.AddHours(-8), "DocumentAPI", HealthStatus.Degraded, 89.3),
            (DateTime.Now.AddHours(-4), "DocumentAPI", HealthStatus.Healthy, 45.1),
            (DateTime.Now, "DocumentAPI", HealthStatus.Healthy, 41.8)
        };

        Console.WriteLine("   DocumentAPI Health History (24h):");
        foreach (var (time, component, status, responseTime) in healthHistory)
        {
            var icon = status switch
            {
                HealthStatus.Healthy => "✅",
                HealthStatus.Degraded => "⚠️",
                HealthStatus.Unhealthy => "❌",
                _ => "❓"
            };
            Console.WriteLine($"     {time:MM-dd HH:mm} | {icon} {status} | Response: {responseTime:F1}ms");
        }

        var avgResponseTime = healthHistory.Average(h => h.ResponseTime);
        var healthyPercentage = (double)healthHistory.Count(h => h.Status == HealthStatus.Healthy) / healthHistory.Count * 100;
        Console.WriteLine($"   Analysis: Avg response time: {avgResponseTime:F1}ms, Healthy uptime: {healthyPercentage:F1}%");
        Console.WriteLine("");
    }

    private static void MetricsAndObservabilityExample()
    {
        Console.WriteLine("5. Metrics and Observability:");
        Console.WriteLine("   // OpenTelemetry integration for distributed tracing and metrics");
        
        var metrics = new Dictionary<string, object>
        {
            ["http_requests_total"] = 15420,
            ["http_request_duration_avg"] = 0.045,
            ["database_connections_active"] = 8,
            ["cache_hit_ratio"] = 0.94,
            ["memory_usage_bytes"] = 512_000_000,
            ["cpu_usage_percent"] = 23.5,
            ["queue_messages_pending"] = 156,
            ["error_rate_percent"] = 0.12
        };

        Console.WriteLine("   Current Metrics:");
        foreach (var (name, value) in metrics)
        {
            var unit = name switch
            {
                var n when n.Contains("bytes") => "bytes",
                var n when n.Contains("percent") => "%",
                var n when n.Contains("duration") => "s",
                var n when n.Contains("ratio") => "",
                _ => ""
            };
            Console.WriteLine($"     {name}: {value}{unit}");
        }
        Console.WriteLine("");
    }

    private static void AlertingAndRemediationExample()
    {
        Console.WriteLine("6. Intelligent Alerting & Automated Remediation:");
        
        var alertRules = new List<(string Name, string Metric, string Condition, string Action)>
        {
            ("High CPU Usage", "cpu_usage_percent > 80", "for 5 minutes", "Scale up instances"),
            ("Database Connection Pool", "db_connections_active > 95%", "for 2 minutes", "Restart connection pool"),
            ("Memory Usage Critical", "memory_usage > 90%", "for 1 minute", "Trigger garbage collection"),
            ("Error Rate Spike", "error_rate > 5%", "for 30 seconds", "Route traffic to backup"),
            ("Queue Backup", "queue_messages > 1000", "for 10 minutes", "Add queue consumers")
        };

        var activeAlerts = new List<(string Alert, string Status, DateTime Triggered, string Action)>
        {
            ("Database Connection Pool", "FIRING", DateTime.Now.AddMinutes(-3), "Auto-restart initiated"),
            ("Queue Backup", "RESOLVED", DateTime.Now.AddMinutes(-15), "Additional consumers added"),
            ("Error Rate Spike", "FIRING", DateTime.Now.AddMinutes(-1), "Traffic rerouting activated")
        };

        Console.WriteLine("   Alert Rules:");
        foreach (var (name, metric, condition, action) in alertRules)
        {
            Console.WriteLine($"     • {name}: {metric} {condition} → {action}");
        }

        Console.WriteLine("   Active Alerts:");
        foreach (var (alert, status, triggered, action) in activeAlerts)
        {
            var icon = status == "FIRING" ? "🔥" : "✅";
            Console.WriteLine($"     {icon} {alert} - {status} (triggered {triggered:HH:mm}) → {action}");
        }
        Console.WriteLine("");
    }

    private static void SLAMonitoringExample()
    {
        Console.WriteLine("7. SLA Monitoring & Reporting:");
        
        var slaReports = new List<(string Service, double UptimeTarget, double UptimeActual, double ResponseTarget, double ResponseActual, double ErrorTarget, double ErrorActual)>
        {
            ("DocumentAPI", 99.9, 99.95, 200, 145.2, 0.1, 0.08),
            ("UserService", 99.9, 99.87, 150, 178.9, 0.1, 0.15),
            ("NotificationService", 99.5, 99.12, 500, 234.1, 0.5, 0.31),
            ("SearchService", 99.9, 99.98, 100, 67.8, 0.1, 0.05)
        };

        Console.WriteLine("   SLA Compliance Report (30 days):");
        Console.WriteLine("   Service              | Uptime Target | Actual | Response Target | Actual | Error Target | Actual | Status");
        Console.WriteLine("   ---------------------|---------------|--------|-----------------|--------|--------------|--------|--------");
        
        foreach (var (service, uptimeTarget, uptimeActual, responseTarget, responseActual, errorTarget, errorActual) in slaReports)
        {
            var uptimeStatus = uptimeActual >= uptimeTarget ? "✅" : "❌";
            var responseStatus = responseActual <= responseTarget ? "✅" : "❌";
            var errorStatus = errorActual <= errorTarget ? "✅" : "❌";
            var overallStatus = uptimeStatus == "✅" && responseStatus == "✅" && errorStatus == "✅" ? "PASS" : "FAIL";
            
            Console.WriteLine($"   {service,-20} | {uptimeTarget,11:F1}% | {uptimeActual,4:F2}% | {responseTarget,13:F0}ms | {responseActual,4:F1}ms | {errorTarget,10:F1}% | {errorActual,4:F2}% | {overallStatus}");
        }
        Console.WriteLine("");
    }

    private static void ComplianceReportingExample()
    {
        Console.WriteLine("8. Compliance Reporting:");
        
        var complianceChecks = new List<(string Standard, string Control, string Description, string Status, DateTime LastCheck)>
        {
            ("SOX", "ITGC-01", "Access controls for financial systems", "COMPLIANT", DateTime.Now.AddDays(-7)),
            ("SOX", "ITGC-02", "Change management procedures", "COMPLIANT", DateTime.Now.AddDays(-14)),
            ("GDPR", "ART-32", "Security of personal data processing", "NON-COMPLIANT", DateTime.Now.AddDays(-2)),
            ("HIPAA", "164.312", "Technical safeguards for PHI", "COMPLIANT", DateTime.Now.AddDays(-10)),
            ("ISO27001", "A.12.6.1", "Management of technical vulnerabilities", "COMPLIANT", DateTime.Now.AddDays(-5)),
            ("PCI-DSS", "REQ-06", "Secure system development", "UNDER_REVIEW", DateTime.Now.AddDays(-1))
        };

        Console.WriteLine("   Regulatory Compliance Status:");
        foreach (var (standard, control, description, status, lastCheck) in complianceChecks)
        {
            var icon = status switch
            {
                "COMPLIANT" => "✅",
                "NON-COMPLIANT" => "❌",
                "UNDER_REVIEW" => "🔍",
                _ => "❓"
            };
            var daysSince = (DateTime.Now - lastCheck).Days;
            Console.WriteLine($"     {icon} {standard} {control}: {description}");
            Console.WriteLine($"        Status: {status} (last checked {daysSince} days ago)");
        }

        var compliantCount = complianceChecks.Count(c => c.Status == "COMPLIANT");
        var totalCount = complianceChecks.Count;
        var complianceRate = (double)compliantCount / totalCount * 100;
        
        Console.WriteLine($"   Overall Compliance Rate: {complianceRate:F1}% ({compliantCount}/{totalCount} controls)");
        Console.WriteLine("");
    }
}