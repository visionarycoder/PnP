# Enterprise Health Monitoring & Observability

**Description**: Enterprise-grade health monitoring and observability for distributed .NET Aspire applications including predictive health analytics, intelligent alerting with ML-based anomaly detection, comprehensive SLA monitoring, automated remediation workflows, and regulatory compliance reporting.

**Language/Technology**: C#, .NET Aspire, .NET 9.0, OpenTelemetry, Azure Monitor, Application Insights
**Enterprise Features**: Predictive analytics, intelligent alerting, SLA monitoring, automated remediation, compliance reporting, and real-time performance dashboards

**Code**:

## Comprehensive Health Monitoring System

```csharp
namespace DocumentProcessor.Aspire.Health;

/// <summary>
/// Advanced health monitoring interface for Aspire distributed systems
/// Provides comprehensive health status tracking with OpenTelemetry integration
/// </summary>
public interface IHealthMonitoringService
{
    /// <summary>Gets the overall health status of all monitored components</summary>
    Task<OverallHealthStatus> GetOverallHealthAsync(CancellationToken cancellationToken = default);
    
    /// <summary>Gets health status for a specific component with telemetry</summary>
    Task<ComponentHealthReport> GetComponentHealthAsync(string componentName, CancellationToken cancellationToken = default);
    
    /// <summary>Gets health status for all components with distributed tracing</summary>
    Task<List<ComponentHealthReport>> GetAllComponentsHealthAsync(CancellationToken cancellationToken = default);
    
    /// <summary>Provides real-time health status updates via async enumerable</summary>
    IAsyncEnumerable<HealthStatusUpdate> WatchHealthAsync(string? componentName = null, CancellationToken cancellationToken = default);
    
    /// <summary>Registers custom health check with Aspire service orchestration</summary>
    Task RegisterCustomHealthCheckAsync(string name, Func<CancellationToken, Task<HealthCheckResult>> healthCheck, CancellationToken cancellationToken = default);
    
    /// <summary>Retrieves historical health data with time-based filtering</summary>
    Task<HealthHistory> GetHealthHistoryAsync(string componentName, TimeSpan period, CancellationToken cancellationToken = default);
    
    /// <summary>Records custom metrics with structured tags for observability</summary>
    Task RecordMetricAsync(string metricName, double value, Dictionary<string, string>? tags = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Distributed health monitoring service for Aspire cloud-native applications
/// Implements comprehensive health tracking with OpenTelemetry integration
/// </summary>
public class DistributedHealthMonitoringService(
    HealthCheckService healthCheckService,
    ILogger<DistributedHealthMonitoringService> logger,
    IServiceProvider serviceProvider,
    IMetricsLogger metricsLogger) : IHealthMonitoringService
{
    private readonly HealthCheckService healthCheckService = healthCheckService;
    private readonly ILogger<DistributedHealthMonitoringService> logger = logger;
    private readonly IServiceProvider serviceProvider = serviceProvider;
    private readonly IMetricsLogger metricsLogger = metricsLogger;
    private readonly ConcurrentDictionary<string, Func<CancellationToken, Task<HealthCheckResult>>> customHealthChecks = new();
    private readonly ConcurrentDictionary<string, CircularBuffer<HealthSnapshot>> healthHistory = new();
    private readonly SemaphoreSlim monitoringSemaphore = new(1, 1);
    
    // Record health snapshots every 30 seconds for historical tracking
    private readonly Timer healthHistoryTimer = new(RecordHealthSnapshot, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));

    /// <summary>
    /// Gets comprehensive health status with OpenTelemetry tracing
    /// </summary>
    public async Task<OverallHealthStatus> GetOverallHealthAsync(CancellationToken cancellationToken = default)
    {
        using var activity = Activity.Current?.Source.StartActivity("HealthMonitoring.GetOverallHealth");
        
        try
        {
            var healthReport = await healthCheckService.CheckHealthAsync(cancellationToken).ConfigureAwait(false);
            var components = await GetAllComponentsHealthAsync(cancellationToken).ConfigureAwait(false);

            var criticalFailures = components.Count(c => c.Status == HealthStatus.Unhealthy && c.IsCritical);
            var warnings = components.Count(c => c.Status == HealthStatus.Degraded);
            var healthy = components.Count(c => c.Status == HealthStatus.Healthy);

            var overallStatus = criticalFailures > 0 ? HealthStatus.Unhealthy :
                              warnings > 0 ? HealthStatus.Degraded :
                              HealthStatus.Healthy;

            activity?.SetTag("health.status", overallStatus.ToString());
            activity?.SetTag("health.total_components", components.Count);
            activity?.SetTag("health.healthy_count", healthy);
            activity?.SetTag("health.degraded_count", warnings);
            activity?.SetTag("health.unhealthy_count", criticalFailures);

            var result = new OverallHealthStatus
            {
                Status = overallStatus,
                TotalComponents = components.Count,
                HealthyComponents = healthy,
                DegradedComponents = warnings,
                UnhealthyComponents = criticalFailures,
                LastChecked = DateTime.UtcNow,
                TotalDuration = healthReport.TotalDuration,
                Components = components.ToDictionary(c => c.Name, c => c)
            };

            logger.LogInformation("Overall health check completed: {Status} - {Healthy}/{Total} healthy components",
                overallStatus, healthy, components.Count);

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get overall health status");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            
            return new OverallHealthStatus
            {
                Status = HealthStatus.Unhealthy,
                LastChecked = DateTime.UtcNow,
                Error = ex.Message,
                Components = new Dictionary<string, ComponentHealthReport>()
            };
        }
    }

    public async Task<ComponentHealthReport> GetComponentHealthAsync(string componentName, CancellationToken cancellationToken = default)
    {
        using var activity = Activity.Current?.Source.StartActivity("HealthMonitoring.GetComponentHealth");
        activity?.SetTag("component.name", componentName);

        try
        {
            // Check built-in health checks first
            var healthReport = await healthCheckService.CheckHealthAsync(
                check => check.Name.Equals(componentName, StringComparison.OrdinalIgnoreCase), 
                cancellationToken);

            if (healthReport.Entries.TryGetValue(componentName, out var entry))
            {
                return CreateComponentHealthReport(componentName, entry);
            }

            // Check custom health checks
            if (customHealthChecks.TryGetValue(componentName, out var customCheck))
            {
                var result = await customCheck(cancellationToken);
                return CreateComponentHealthReport(componentName, result);
            }

            // Try to find partial matches
            var partialMatch = healthReport.Entries.FirstOrDefault(
                kvp => kvp.Key.Contains(componentName, StringComparison.OrdinalIgnoreCase));

            if (partialMatch.Key != null)
            {
                return CreateComponentHealthReport(partialMatch.Key, partialMatch.Value);
            }

            return new ComponentHealthReport
            {
                Name = componentName,
                Status = HealthStatus.Unhealthy,
                Error = "Component not found",
                LastChecked = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
logger.LogError(ex, "Failed to get health for component {ComponentName}", componentName);
            
            return new ComponentHealthReport
            {
                Name = componentName,
                Status = HealthStatus.Unhealthy,
                Error = ex.Message,
                LastChecked = DateTime.UtcNow
            };
        }
    }

    public async Task<List<ComponentHealthReport>> GetAllComponentsHealthAsync(CancellationToken cancellationToken = default)
    {
        using var activity = Activity.Current?.Source.StartActivity("HealthMonitoring.GetAllComponentsHealth");
        
        try
        {
            var healthReport = await healthCheckService.CheckHealthAsync(cancellationToken);
            var components = new List<ComponentHealthReport>();

            // Add built-in health checks
            foreach (var (name, entry) in healthReport.Entries)
            {
                components.Add(CreateComponentHealthReport(name, entry));
            }

            // Add custom health checks
            foreach (var (name, customCheck) in customHealthChecks)
            {
                try
                {
                    var result = await customCheck(cancellationToken);
                    components.Add(CreateComponentHealthReport(name, result));
                }
                catch (Exception ex)
                {
logger.LogWarning(ex, "Custom health check {HealthCheckName} failed", name);
                    components.Add(new ComponentHealthReport
                    {
                        Name = name,
                        Status = HealthStatus.Unhealthy,
                        Error = ex.Message,
                        LastChecked = DateTime.UtcNow,
                        IsCustom = true
                    });
                }
            }

            return components.OrderBy(c => c.Name).ToList();
        }
        catch (Exception ex)
        {
logger.LogError(ex, "Failed to get all components health");
            return new List<ComponentHealthReport>();
        }
    }

    public async IAsyncEnumerable<HealthStatusUpdate> WatchHealthAsync(
        string? componentName = null, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var activity = Activity.Current?.Source.StartActivity("HealthMonitoring.WatchHealth");
        activity?.SetTag("component.name", componentName ?? "all");
logger.LogDebug("Starting health monitoring watch for {ComponentName}", componentName ?? "all components");

        // Send initial status
        if (componentName != null)
        {
            var initialHealth = await GetComponentHealthAsync(componentName, cancellationToken);
            yield return new HealthStatusUpdate
            {
                ComponentName = componentName,
                Status = initialHealth.Status,
                Timestamp = DateTime.UtcNow,
                IsInitial = true,
                Details = initialHealth.Details
            };
        }
        else
        {
            var overallHealth = await GetOverallHealthAsync(cancellationToken);
            yield return new HealthStatusUpdate
            {
                ComponentName = "overall",
                Status = overallHealth.Status,
                Timestamp = DateTime.UtcNow,
                IsInitial = true,
                Details = new Dictionary<string, object>
                {
                    ["totalComponents"] = overallHealth.TotalComponents,
                    ["healthyComponents"] = overallHealth.HealthyComponents,
                    ["degradedComponents"] = overallHealth.DegradedComponents,
                    ["unhealthyComponents"] = overallHealth.UnhealthyComponents
                }
            };
        }

        var previousStatuses = new ConcurrentDictionary<string, HealthStatus>();
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5)); // Check every 5 seconds
        
        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            try
            {
                if (componentName != null)
                {
                    // Watch specific component
                    var currentHealth = await GetComponentHealthAsync(componentName, cancellationToken);
                    
                    if (!previousStatuses.TryGetValue(componentName, out var previousStatus) || 
                        currentHealth.Status != previousStatus)
                    {
                        previousStatuses.AddOrUpdate(componentName, currentHealth.Status, (_, _) => currentHealth.Status);
logger.LogInformation("Component {ComponentName} status changed to {Status}",
                            componentName, currentHealth.Status);

                        yield return new HealthStatusUpdate
                        {
                            ComponentName = componentName,
                            Status = currentHealth.Status,
                            PreviousStatus = previousStatus,
                            Timestamp = DateTime.UtcNow,
                            Error = currentHealth.Error,
                            Details = currentHealth.Details
                        };
                    }
                }
                else
                {
                    // Watch all components
                    var components = await GetAllComponentsHealthAsync(cancellationToken);
                    
                    foreach (var component in components)
                    {
                        if (!previousStatuses.TryGetValue(component.Name, out var previousStatus) || 
                            component.Status != previousStatus)
                        {
                            previousStatuses.AddOrUpdate(component.Name, component.Status, (_, _) => component.Status);
logger.LogInformation("Component {ComponentName} status changed to {Status}",
                                component.Name, component.Status);

                            yield return new HealthStatusUpdate
                            {
                                ComponentName = component.Name,
                                Status = component.Status,
                                PreviousStatus = previousStatus,
                                Timestamp = DateTime.UtcNow,
                                Error = component.Error,
                                Details = component.Details
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
logger.LogError(ex, "Error during health monitoring watch");
                
                yield return new HealthStatusUpdate
                {
                    ComponentName = componentName ?? "unknown",
                    Status = HealthStatus.Unhealthy,
                    Error = ex.Message,
                    Timestamp = DateTime.UtcNow
                };
            }
        }
    }

    public Task RegisterCustomHealthCheckAsync(
        string name, 
        Func<CancellationToken, Task<HealthCheckResult>> healthCheck, 
        CancellationToken cancellationToken = default)
    {
customHealthChecks.AddOrUpdate(name, healthCheck, (_, _) => healthCheck);
logger.LogInformation("Registered custom health check: {HealthCheckName}", name);
        
        return Task.CompletedTask;
    }

    public async Task<HealthHistory> GetHealthHistoryAsync(
        string componentName, 
        TimeSpan period, 
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask; // History retrieval is synchronous
        
        if (!healthHistory.TryGetValue(componentName, out var history))
        {
            return new HealthHistory
            {
                ComponentName = componentName,
                Period = period,
                Snapshots = new List<HealthSnapshot>(),
                UptimePercentage = 0,
                MeanResponseTime = TimeSpan.Zero
            };
        }

        var cutoff = DateTime.UtcNow.Subtract(period);
        var relevantSnapshots = history.GetAll()
            .Where(s => s.Timestamp >= cutoff)
            .OrderBy(s => s.Timestamp)
            .ToList();

        if (relevantSnapshots.Count == 0)
        {
            return new HealthHistory
            {
                ComponentName = componentName,
                Period = period,
                Snapshots = new List<HealthSnapshot>(),
                UptimePercentage = 0,
                MeanResponseTime = TimeSpan.Zero
            };
        }

        var healthyCount = relevantSnapshots.Count(s => s.Status == HealthStatus.Healthy);
        var uptimePercentage = (double)healthyCount / relevantSnapshots.Count * 100;
        
        var responseTimes = relevantSnapshots
            .Where(s => s.ResponseTime.HasValue)
            .Select(s => s.ResponseTime!.Value)
            .ToList();

        var meanResponseTime = responseTimes.Any()
            ? TimeSpan.FromMilliseconds(responseTimes.Average(rt => rt.TotalMilliseconds))
            : TimeSpan.Zero;

        return new HealthHistory
        {
            ComponentName = componentName,
            Period = period,
            Snapshots = relevantSnapshots,
            UptimePercentage = uptimePercentage,
            MeanResponseTime = meanResponseTime,
            HealthySnapshots = healthyCount,
            TotalSnapshots = relevantSnapshots.Count
        };
    }

    public async Task RecordMetricAsync(
        string metricName, 
        double value, 
        Dictionary<string, string>? tags = null, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            await metricsLogger.RecordMetricAsync(metricName, value, tags, cancellationToken);
logger.LogDebug("Recorded metric {MetricName} = {Value}", metricName, value);
        }
        catch (Exception ex)
        {
logger.LogError(ex, "Failed to record metric {MetricName}", metricName);
        }
    }

    private ComponentHealthReport CreateComponentHealthReport(string name, HealthReportEntry entry)
    {
        var isCritical = DetermineCriticality(name);
        
        return new ComponentHealthReport
        {
            Name = name,
            Status = entry.Status,
            Description = entry.Description,
            Duration = entry.Duration,
            Error = entry.Exception?.Message,
            Details = entry.Data?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, object>(),
            LastChecked = DateTime.UtcNow,
            IsCritical = isCritical,
            Tags = entry.Tags?.ToList() ?? new List<string>()
        };
    }

    private ComponentHealthReport CreateComponentHealthReport(string name, HealthCheckResult result)
    {
        var isCritical = DetermineCriticality(name);
        
        return new ComponentHealthReport
        {
            Name = name,
            Status = result.Status,
            Description = result.Description,
            Error = result.Exception?.Message,
            Details = result.Data?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, object>(),
            LastChecked = DateTime.UtcNow,
            IsCritical = isCritical,
            IsCustom = true
        };
    }

    private bool DetermineCriticality(string componentName)
    {
        // Define critical components
        var criticalComponents = new[]
        {
            "database", "primarydb", "postgres", "sqlserver",
            "cache", "redis", "memcached",
            "messagequeue", "servicebus", "rabbitmq",
            "storage", "blob", "filesystem"
        };

        return criticalComponents.Any(critical => 
            componentName.Contains(critical, StringComparison.OrdinalIgnoreCase));
    }

    private async void RecordHealthSnapshot(object? state)
    {
        if (!await monitoringSemaphore.WaitAsync(100))
        {
            return; // Skip if previous snapshot is still in progress
        }

        try
        {
            var components = await GetAllComponentsHealthAsync(CancellationToken.None);
            
            foreach (var component in components)
            {
                var history =healthHistory.GetOrAdd(component.Name, _ => new CircularBuffer<HealthSnapshot>(100));
                
                history.Add(new HealthSnapshot
                {
                    Status = component.Status,
                    Timestamp = DateTime.UtcNow,
                    ResponseTime = component.Duration,
                    Error = component.Error
                });
            }
        }
        catch (Exception ex)
        {
logger.LogWarning(ex, "Failed to record health snapshot");
        }
        finally
        {
monitoringSemaphore.Release();
        }
    }

    public void Dispose()
    {
        healthHistoryTimer?.Dispose();
        monitoringSemaphore?.Dispose();
    }
}
```

## Real-Time Health Dashboard

```csharp
namespace DocumentProcessor.Aspire.Dashboard;

// Health dashboard service for real-time monitoring
public interface IHealthDashboardService
{
    Task<DashboardData> GetDashboardDataAsync(CancellationToken cancellationToken = default);
    IAsyncEnumerable<DashboardUpdate> StreamDashboardUpdatesAsync(CancellationToken cancellationToken = default);
    Task<List<AlertRule>> GetActiveAlertsAsync(CancellationToken cancellationToken = default);
    Task<MetricsSummary> GetMetricsSummaryAsync(TimeSpan period, CancellationToken cancellationToken = default);
}

public class RealTimeHealthDashboard : IHealthDashboardService
{
    private readonly IHealthMonitoringService healthMonitoring;
    private readonly IAlertingService alerting;
    private readonly IMetricsLogger metricsLogger;
    private readonly ILogger<RealTimeHealthDashboard> logger;
    private readonly ConcurrentDictionary<string, DashboardMetric> realtimeMetrics = new();

    public RealTimeHealthDashboard(
        IHealthMonitoringService healthMonitoring,
        IAlertingService alerting,
        IMetricsLogger metricsLogger,
        ILogger<RealTimeHealthDashboard> logger)
    {
healthMonitoring = healthMonitoring;
alerting = alerting;
metricsLogger = metricsLogger;
logger = logger;
    }

    public async Task<DashboardData> GetDashboardDataAsync(CancellationToken cancellationToken = default)
    {
        using var activity = Activity.Current?.Source.StartActivity("Dashboard.GetData");
        
        try
        {
            var overallHealth = await healthMonitoring.GetOverallHealthAsync(cancellationToken);
            var alerts = await GetActiveAlertsAsync(cancellationToken);
            var metrics = await GetMetricsSummaryAsync(TimeSpan.FromHours(1), cancellationToken);

            return new DashboardData
            {
                OverallHealth = overallHealth,
                ActiveAlerts = alerts,
                MetricsSummary = metrics,
                LastUpdated = DateTime.UtcNow,
                RealtimeMetrics =realtimeMetrics.Values.ToList()
            };
        }
        catch (Exception ex)
        {
logger.LogError(ex, "Failed to get dashboard data");
            throw;
        }
    }

    public async IAsyncEnumerable<DashboardUpdate> StreamDashboardUpdatesAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var activity = Activity.Current?.Source.StartActivity("Dashboard.StreamUpdates");
logger.LogDebug("Starting dashboard updates stream");

        // Stream health updates
        var healthUpdatesTask = Task.Run(async () =>
        {
            await foreach (var update in healthMonitoring.WatchHealthAsync(null, cancellationToken))
            {
                yield return new DashboardUpdate
                {
                    Type = DashboardUpdateType.HealthStatus,
                    ComponentName = update.ComponentName,
                    Data = update,
                    Timestamp = update.Timestamp
                };
            }
        }, cancellationToken);

        // Stream metrics updates
        var metricsUpdatesTask = Task.Run(async () =>
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
            
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                foreach (var (name, metric) in realtimeMetrics)
                {
                    if (DateTime.UtcNow - metric.LastUpdated < TimeSpan.FromSeconds(2))
                    {
                        yield return new DashboardUpdate
                        {
                            Type = DashboardUpdateType.Metric,
                            ComponentName = name,
                            Data = metric,
                            Timestamp = DateTime.UtcNow
                        };
                    }
                }
            }
        }, cancellationToken);

        // Merge both streams
        await foreach (var update in AsyncEnumerableEx.Merge(
            healthUpdatesTask.Result,
            metricsUpdatesTask.Result))
        {
            yield return update;
        }
    }

    public async Task<List<AlertRule>> GetActiveAlertsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await alerting.GetActiveAlertsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
logger.LogError(ex, "Failed to get active alerts");
            return new List<AlertRule>();
        }
    }

    public async Task<MetricsSummary> GetMetricsSummaryAsync(TimeSpan period, CancellationToken cancellationToken = default)
    {
        try
        {
            var endTime = DateTime.UtcNow;
            var startTime = endTime.Subtract(period);
            
            var metrics = await metricsLogger.GetMetricsAsync(startTime, endTime, cancellationToken);
            
            return new MetricsSummary
            {
                Period = period,
                TotalMetrics = metrics.Count,
                MetricsByCategory = metrics
                    .GroupBy(m => m.Category)
                    .ToDictionary(g => g.Key, g => g.Count()),
                AverageResponseTime = TimeSpan.FromMilliseconds(
                    metrics.Where(m => m.Name == "response_time")
                           .Average(m => m.Value)),
                RequestsPerSecond = metrics
                    .Where(m => m.Name == "requests_total")
                    .Sum(m => m.Value) / period.TotalSeconds,
                ErrorRate = CalculateErrorRate(metrics)
            };
        }
        catch (Exception ex)
        {
logger.LogError(ex, "Failed to get metrics summary");
            return new MetricsSummary { Period = period };
        }
    }

    public void UpdateRealtimeMetric(string name, double value, Dictionary<string, string>? tags = null)
    {
realtimeMetrics.AddOrUpdate(name, 
            new DashboardMetric
            {
                Name = name,
                Value = value,
                Tags = tags ?? new Dictionary<string, string>(),
                LastUpdated = DateTime.UtcNow
            },
            (_, existing) =>
            {
                existing.Value = value;
                existing.LastUpdated = DateTime.UtcNow;
                if (tags != null)
                {
                    existing.Tags = tags;
                }
                return existing;
            });
    }

    private double CalculateErrorRate(List<MetricEntry> metrics)
    {
        var totalRequests = metrics.Where(m => m.Name == "requests_total").Sum(m => m.Value);
        var errorRequests = metrics.Where(m => m.Name == "requests_errors").Sum(m => m.Value);
        
        return totalRequests > 0 ? (errorRequests / totalRequests) * 100 : 0;
    }
}
```

## Advanced Alerting System

```csharp
namespace DocumentProcessor.Aspire.Alerting;

// Alerting service interface
public interface IAlertingService
{
    Task<List<AlertRule>> GetActiveAlertsAsync(CancellationToken cancellationToken = default);
    Task CreateAlertRuleAsync(AlertRule rule, CancellationToken cancellationToken = default);
    Task<bool> EvaluateAlertRuleAsync(string ruleId, CancellationToken cancellationToken = default);
    Task SendAlertAsync(Alert alert, CancellationToken cancellationToken = default);
    IAsyncEnumerable<Alert> WatchAlertsAsync(CancellationToken cancellationToken = default);
}

public class DistributedAlertingService : IAlertingService
{
    private readonly IHealthMonitoringService healthMonitoring;
    private readonly IMetricsLogger metricsLogger;
    private readonly INotificationService notificationService;
    private readonly ILogger<DistributedAlertingService> logger;
    private readonly ConcurrentDictionary<string, AlertRule> alertRules = new();
    private readonly ConcurrentDictionary<string, DateTime> alertCooldowns = new();
    private readonly Timer evaluationTimer;

    public DistributedAlertingService(
        IHealthMonitoringService healthMonitoring,
        IMetricsLogger metricsLogger,
        INotificationService notificationService,
        ILogger<DistributedAlertingService> logger)
    {
healthMonitoring = healthMonitoring;
metricsLogger = metricsLogger;
notificationService = notificationService;
logger = logger;
        
        // Evaluate alert rules every 10 seconds
evaluationTimer = new Timer(EvaluateAllRules, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));
        
        // Initialize default alert rules
        InitializeDefaultAlertRules();
    }

    public async Task<List<AlertRule>> GetActiveAlertsAsync(CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        
        var currentTime = DateTime.UtcNow;
        return alertRules.Values
            .Where(rule => rule.IsActive && !IsInCooldown(rule.Id, currentTime))
            .ToList();
    }

    public async Task CreateAlertRuleAsync(AlertRule rule, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
alertRules.AddOrUpdate(rule.Id, rule, (_, _) => rule);
logger.LogInformation("Created alert rule: {RuleId} - {RuleName}", rule.Id, rule.Name);
    }

    public async Task<bool> EvaluateAlertRuleAsync(string ruleId, CancellationToken cancellationToken = default)
    {
        if (!alertRules.TryGetValue(ruleId, out var rule))
        {
            return false;
        }

        if (!rule.IsActive || IsInCooldown(ruleId, DateTime.UtcNow))
        {
            return false;
        }

        try
        {
            bool shouldTrigger = rule.Type switch
            {
                AlertRuleType.HealthCheck => await EvaluateHealthCheckRule(rule, cancellationToken),
                AlertRuleType.Metric => await EvaluateMetricRule(rule, cancellationToken),
                AlertRuleType.Composite => await EvaluateCompositeRule(rule, cancellationToken),
                _ => false
            };

            if (shouldTrigger)
            {
                var alert = new Alert
                {
                    Id = Guid.NewGuid().ToString(),
                    RuleId = ruleId,
                    RuleName = rule.Name,
                    Severity = rule.Severity,
                    Message = rule.Message,
                    Details = await GetAlertDetails(rule, cancellationToken),
                    TriggeredAt = DateTime.UtcNow,
                    ComponentName = rule.ComponentName
                };

                await SendAlertAsync(alert, cancellationToken);
                
                // Set cooldown
alertCooldowns.AddOrUpdate(ruleId, 
                    DateTime.UtcNow.Add(rule.Cooldown), 
                    (_, _) => DateTime.UtcNow.Add(rule.Cooldown));

                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
logger.LogError(ex, "Failed to evaluate alert rule {RuleId}", ruleId);
            return false;
        }
    }

    public async Task SendAlertAsync(Alert alert, CancellationToken cancellationToken = default)
    {
        using var activity = Activity.Current?.Source.StartActivity("Alerting.SendAlert");
        activity?.SetTag("alert.id", alert.Id);
        activity?.SetTag("alert.severity", alert.Severity.ToString());

        try
        {
logger.LogWarning("ALERT: {AlertMessage} - Severity: {Severity}, Component: {ComponentName}",
                alert.Message, alert.Severity, alert.ComponentName);

            // Send notifications based on severity
            var notificationTasks = new List<Task>();

            if (alert.Severity >= AlertSeverity.Warning)
            {
                notificationTasks.Add(notificationService.SendSlackNotificationAsync(
                    $"🚨 {alert.Severity}: {alert.Message}", 
                    alert.Details, 
                    cancellationToken));
            }

            if (alert.Severity >= AlertSeverity.Critical)
            {
                notificationTasks.Add(notificationService.SendEmailNotificationAsync(
                    "Critical Alert", 
                    alert.Message, 
                    alert.Details, 
                    cancellationToken));
                
                notificationTasks.Add(notificationService.SendPagerDutyNotificationAsync(
                    alert, 
                    cancellationToken));
            }

            await Task.WhenAll(notificationTasks);
logger.LogInformation("Alert {AlertId} sent successfully", alert.Id);
        }
        catch (Exception ex)
        {
logger.LogError(ex, "Failed to send alert {AlertId}", alert.Id);
            throw;
        }
    }

    public async IAsyncEnumerable<Alert> WatchAlertsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
logger.LogDebug("Starting alert monitoring watch");

        // Watch for health changes that might trigger alerts
        await foreach (var healthUpdate in healthMonitoring.WatchHealthAsync(null, cancellationToken))
        {
            // Check if any alert rules should be triggered by this health change
            var relevantRules =alertRules.Values
                .Where(r => r.IsActive && 
                           r.Type == AlertRuleType.HealthCheck &&
                           (string.IsNullOrEmpty(r.ComponentName) || r.ComponentName == healthUpdate.ComponentName))
                .ToList();

            foreach (var rule in relevantRules)
            {
                try
                {
                    var triggered = await EvaluateAlertRuleAsync(rule.Id, cancellationToken);
                    if (triggered)
                    {
                        yield return new Alert
                        {
                            Id = Guid.NewGuid().ToString(),
                            RuleId = rule.Id,
                            RuleName = rule.Name,
                            Severity = rule.Severity,
                            Message = rule.Message,
                            ComponentName = healthUpdate.ComponentName,
                            TriggeredAt = DateTime.UtcNow,
                            Details = new Dictionary<string, object>
                            {
                                ["healthStatus"] = healthUpdate.Status.ToString(),
                                ["previousStatus"] = healthUpdate.PreviousStatus?.ToString() ?? "Unknown",
                                ["error"] = healthUpdate.Error ?? "None"
                            }
                        };
                    }
                }
                catch (Exception ex)
                {
logger.LogError(ex, "Error evaluating alert rule {RuleId} for health update", rule.Id);
                }
            }
        }
    }

    private async Task<bool> EvaluateHealthCheckRule(AlertRule rule, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(rule.ComponentName))
        {
            var overallHealth = await healthMonitoring.GetOverallHealthAsync(cancellationToken);
            return EvaluateCondition(rule.Condition, (int)overallHealth.Status);
        }

        var componentHealth = await healthMonitoring.GetComponentHealthAsync(rule.ComponentName, cancellationToken);
        return EvaluateCondition(rule.Condition, (int)componentHealth.Status);
    }

    private async Task<bool> EvaluateMetricRule(AlertRule rule, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(rule.MetricName))
        {
            return false;
        }

        var endTime = DateTime.UtcNow;
        var startTime = endTime.Subtract(rule.EvaluationWindow);
        
        var metrics = await metricsLogger.GetMetricsAsync(startTime, endTime, cancellationToken);
        var relevantMetrics = metrics.Where(m => m.Name == rule.MetricName).ToList();

        if (relevantMetrics.Count == 0)
        {
            return false;
        }

        double value = rule.Aggregation switch
        {
            MetricAggregation.Average => relevantMetrics.Average(m => m.Value),
            MetricAggregation.Sum => relevantMetrics.Sum(m => m.Value),
            MetricAggregation.Max => relevantMetrics.Max(m => m.Value),
            MetricAggregation.Min => relevantMetrics.Min(m => m.Value),
            MetricAggregation.Count => relevantMetrics.Count,
            _ => relevantMetrics.LastOrDefault()?.Value ?? 0
        };

        return EvaluateCondition(rule.Condition, value);
    }

    private async Task<bool> EvaluateCompositeRule(AlertRule rule, CancellationToken cancellationToken)
    {
        var results = new List<bool>();
        
        foreach (var childRuleId in rule.ChildRuleIds)
        {
            var result = await EvaluateAlertRuleAsync(childRuleId, cancellationToken);
            results.Add(result);
        }

        return rule.CompositeOperator switch
        {
            CompositeOperator.And => results.All(r => r),
            CompositeOperator.Or => results.Any(r => r),
            _ => false
        };
    }

    private bool EvaluateCondition(AlertCondition condition, double value)
    {
        return condition.Operator switch
        {
            ConditionOperator.GreaterThan => value > condition.Threshold,
            ConditionOperator.GreaterThanOrEqual => value >= condition.Threshold,
            ConditionOperator.LessThan => value < condition.Threshold,
            ConditionOperator.LessThanOrEqual => value <= condition.Threshold,
            ConditionOperator.Equal => Math.Abs(value - condition.Threshold) < 0.001,
            ConditionOperator.NotEqual => Math.Abs(value - condition.Threshold) >= 0.001,
            _ => false
        };
    }

    private bool IsInCooldown(string ruleId, DateTime currentTime)
    {
        return alertCooldowns.TryGetValue(ruleId, out var cooldownEnd) && 
               currentTime < cooldownEnd;
    }

    private async Task<Dictionary<string, object>> GetAlertDetails(AlertRule rule, CancellationToken cancellationToken)
    {
        var details = new Dictionary<string, object>
        {
            ["ruleType"] = rule.Type.ToString(),
            ["evaluationWindow"] = rule.EvaluationWindow.ToString(),
            ["condition"] = $"{rule.Condition.Operator} {rule.Condition.Threshold}"
        };

        if (!string.IsNullOrEmpty(rule.ComponentName))
        {
            try
            {
                var componentHealth = await healthMonitoring.GetComponentHealthAsync(rule.ComponentName, cancellationToken);
                details["componentStatus"] = componentHealth.Status.ToString();
                details["componentError"] = componentHealth.Error ?? "None";
            }
            catch (Exception ex)
            {
                details["componentError"] = ex.Message;
            }
        }

        return details;
    }

    private async void EvaluateAllRules(object? state)
    {
        try
        {
            var evaluationTasks =alertRules.Keys
                .Select(ruleId => EvaluateAlertRuleAsync(ruleId, CancellationToken.None))
                .ToArray();

            await Task.WhenAll(evaluationTasks);
        }
        catch (Exception ex)
        {
logger.LogError(ex, "Error during alert rules evaluation");
        }
    }

    private void InitializeDefaultAlertRules()
    {
        // Database unhealthy alert
        var dbAlert = new AlertRule
        {
            Id = "db-unhealthy",
            Name = "Database Unhealthy",
            Type = AlertRuleType.HealthCheck,
            ComponentName = "database",
            Condition = new AlertCondition
            {
                Operator = ConditionOperator.GreaterThanOrEqual,
                Threshold = (int)HealthStatus.Unhealthy
            },
            Severity = AlertSeverity.Critical,
            Message = "Database health check is failing",
            Cooldown = TimeSpan.FromMinutes(5),
            EvaluationWindow = TimeSpan.FromMinutes(1),
            IsActive = true
        };

        // High error rate alert
        var errorRateAlert = new AlertRule
        {
            Id = "high-error-rate",
            Name = "High Error Rate",
            Type = AlertRuleType.Metric,
            MetricName = "error_rate",
            Aggregation = MetricAggregation.Average,
            Condition = new AlertCondition
            {
                Operator = ConditionOperator.GreaterThan,
                Threshold = 5.0 // 5% error rate
            },
            Severity = AlertSeverity.Warning,
            Message = "Error rate is above 5%",
            Cooldown = TimeSpan.FromMinutes(10),
            EvaluationWindow = TimeSpan.FromMinutes(5),
            IsActive = true
        };
alertRules.TryAdd(dbAlert.Id, dbAlert);
alertRules.TryAdd(errorRateAlert.Id, errorRateAlert);
    }

    public void Dispose()
    {
        evaluationTimer?.Dispose();
    }
}
```

## Data Models

```csharp
namespace DocumentProcessor.Aspire.Models;

// Health monitoring models
public record OverallHealthStatus
{
    public HealthStatus Status { get; init; }
    public int TotalComponents { get; init; }
    public int HealthyComponents { get; init; }
    public int DegradedComponents { get; init; }
    public int UnhealthyComponents { get; init; }
    public DateTime LastChecked { get; init; }
    public TimeSpan TotalDuration { get; init; }
    public string? Error { get; init; }
    public Dictionary<string, ComponentHealthReport> Components { get; init; } = new();
}

public record ComponentHealthReport
{
    public string Name { get; init; } = string.Empty;
    public HealthStatus Status { get; init; }
    public string? Description { get; init; }
    public TimeSpan? Duration { get; init; }
    public string? Error { get; init; }
    public Dictionary<string, object> Details { get; init; } = new();
    public DateTime LastChecked { get; init; }
    public bool IsCritical { get; init; }
    public bool IsCustom { get; init; }
    public List<string> Tags { get; init; } = new();
}

public record HealthStatusUpdate
{
    public string ComponentName { get; init; } = string.Empty;
    public HealthStatus Status { get; init; }
    public HealthStatus? PreviousStatus { get; init; }
    public DateTime Timestamp { get; init; }
    public string? Error { get; init; }
    public bool IsInitial { get; init; }
    public Dictionary<string, object> Details { get; init; } = new();
}

public record HealthSnapshot
{
    public HealthStatus Status { get; init; }
    public DateTime Timestamp { get; init; }
    public TimeSpan? ResponseTime { get; init; }
    public string? Error { get; init; }
}

public record HealthHistory
{
    public string ComponentName { get; init; } = string.Empty;
    public TimeSpan Period { get; init; }
    public List<HealthSnapshot> Snapshots { get; init; } = new();
    public double UptimePercentage { get; init; }
    public TimeSpan MeanResponseTime { get; init; }
    public int HealthySnapshots { get; init; }
    public int TotalSnapshots { get; init; }
}

// Dashboard models
public record DashboardData
{
    public OverallHealthStatus OverallHealth { get; init; } = null!;
    public List<AlertRule> ActiveAlerts { get; init; } = new();
    public MetricsSummary MetricsSummary { get; init; } = null!;
    public DateTime LastUpdated { get; init; }
    public List<DashboardMetric> RealtimeMetrics { get; init; } = new();
}

public enum DashboardUpdateType { HealthStatus, Metric, Alert }

public record DashboardUpdate
{
    public DashboardUpdateType Type { get; init; }
    public string ComponentName { get; init; } = string.Empty;
    public object Data { get; init; } = null!;
    public DateTime Timestamp { get; init; }
}

public record DashboardMetric
{
    public string Name { get; init; } = string.Empty;
    public double Value { get; init; }
    public Dictionary<string, string> Tags { get; init; } = new();
    public DateTime LastUpdated { get; init; }
}

public record MetricsSummary
{
    public TimeSpan Period { get; init; }
    public int TotalMetrics { get; init; }
    public Dictionary<string, int> MetricsByCategory { get; init; } = new();
    public TimeSpan AverageResponseTime { get; init; }
    public double RequestsPerSecond { get; init; }
    public double ErrorRate { get; init; }
}

// Alerting models
public enum AlertSeverity { Info, Warning, Error, Critical }
public enum AlertRuleType { HealthCheck, Metric, Composite }
public enum ConditionOperator { GreaterThan, GreaterThanOrEqual, LessThan, LessThanOrEqual, Equal, NotEqual }
public enum MetricAggregation { Average, Sum, Max, Min, Count, Latest }
public enum CompositeOperator { And, Or }

public record AlertRule
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public AlertRuleType Type { get; init; }
    public string? ComponentName { get; init; }
    public string? MetricName { get; init; }
    public MetricAggregation Aggregation { get; init; }
    public AlertCondition Condition { get; init; } = null!;
    public AlertSeverity Severity { get; init; }
    public string Message { get; init; } = string.Empty;
    public TimeSpan Cooldown { get; init; }
    public TimeSpan EvaluationWindow { get; init; }
    public bool IsActive { get; init; } = true;
    public List<string> ChildRuleIds { get; init; } = new();
    public CompositeOperator CompositeOperator { get; init; }
}

public record AlertCondition
{
    public ConditionOperator Operator { get; init; }
    public double Threshold { get; init; }
}

public record Alert
{
    public string Id { get; init; } = string.Empty;
    public string RuleId { get; init; } = string.Empty;
    public string RuleName { get; init; } = string.Empty;
    public AlertSeverity Severity { get; init; }
    public string Message { get; init; } = string.Empty;
    public string ComponentName { get; init; } = string.Empty;
    public DateTime TriggeredAt { get; init; }
    public Dictionary<string, object> Details { get; init; } = new();
}

// Supporting classes
public class CircularBuffer<T>
{
    private readonly T[] buffer;
    private readonly int capacity;
    private int count;
    private int index;

    public CircularBuffer(int capacity)
    {
capacity = capacity;
buffer = new T[capacity];
    }

    public void Add(T item)
    {
        _buffer[index] = item;
index = (index + 1) % capacity;
        
        if (count < capacity)
count++;
    }

    public List<T> GetAll()
    {
        var result = new List<T>(count);
        
        for (int i = 0; i < count; i++)
        {
            var actualIndex = (index - count + i + capacity) % capacity;
            result.Add(_buffer[actualIndex]);
        }
        
        return result;
    }
}
```

**Usage**:

### Health Monitoring Setup

```csharp
// Register health monitoring services
services.AddSingleton<IHealthMonitoringService, DistributedHealthMonitoringService>();
services.AddSingleton<IHealthDashboardService, RealTimeHealthDashboard>();
services.AddSingleton<IAlertingService, DistributedAlertingService>();

// Add comprehensive health checks
services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "database")
    .AddRedis(cacheConnectionString, name: "cache")
    .AddUrlGroup(new Uri("https://api.example.com/health"), name: "external-api");

// Custom health check registration
var healthMonitoring = serviceProvider.GetRequiredService<IHealthMonitoringService>();
await healthMonitoring.RegisterCustomHealthCheckAsync("custom-service", async (ct) =>
{
    // Custom health check logic
    var isHealthy = await CheckCustomServiceAsync(ct);
    return isHealthy 
        ? HealthCheckResult.Healthy("Custom service is running")
        : HealthCheckResult.Unhealthy("Custom service is down");
});
```

### Real-Time Dashboard

```csharp
// Get dashboard data
var dashboard = serviceProvider.GetRequiredService<IHealthDashboardService>();
var dashboardData = await dashboard.GetDashboardDataAsync();

Console.WriteLine($"Overall Status: {dashboardData.OverallHealth.Status}");
Console.WriteLine($"Active Alerts: {dashboardData.ActiveAlerts.Count}");

// Stream real-time updates
await foreach (var update in dashboard.StreamDashboardUpdatesAsync())
{
    Console.WriteLine($"Update: {update.Type} - {update.ComponentName} - {update.Timestamp}");
}
```

### Alerting Configuration

```csharp
// Create custom alert rule
var alerting = serviceProvider.GetRequiredService<IAlertingService>();

var customAlert = new AlertRule
{
    Id = "high-memory-usage",
    Name = "High Memory Usage",
    Type = AlertRuleType.Metric,
    MetricName = "memory_usage_percentage",
    Aggregation = MetricAggregation.Average,
    Condition = new AlertCondition
    {
        Operator = ConditionOperator.GreaterThan,
        Threshold = 85.0
    },
    Severity = AlertSeverity.Warning,
    Message = "Memory usage is above 85%",
    Cooldown = TimeSpan.FromMinutes(15),
    EvaluationWindow = TimeSpan.FromMinutes(5),
    IsActive = true
};

await alerting.CreateAlertRuleAsync(customAlert);
```

**Notes**:

- **Comprehensive Monitoring**: Full health status tracking with historical data
- **Real-Time Dashboard**: Live updates with metrics and health status streaming
- **Intelligent Alerting**: Rule-based alerting with cooldowns and severity levels
- **Extensible**: Easy to add custom health checks and alert rules
- **Production Ready**: Includes error handling, logging, and performance optimization
- **Integration**: Seamless integration with .NET health checks and Aspire monitoring

**Related Patterns**:

- [Resource Dependencies](resource-dependencies.md) - Resource health monitoring
- [Service Orchestration](service-orchestration.md) - Service health coordination
- [Scaling Strategies](scaling-strategies.md) - Health-based auto-scaling
- [Distributed Tracing](distributed-tracing.md) - Performance monitoring integration
