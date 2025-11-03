# .NET Aspire Audit and Compliance

**Description**: Cloud-native audit trail patterns for .NET Aspire applications with distributed tracing, compliance reporting, regulatory requirements (SOX, GDPR, HIPAA), and automated compliance monitoring integrated with OpenTelemetry and service orchestration.

**Language/Technology**: C#, .NET Aspire, .NET 9.0, OpenTelemetry

**Code**:

## Aspire-Integrated Audit Trail System

```csharp
namespace DocumentProcessor.Aspire.Compliance;

/// <summary>
/// Audit event types specific to Aspire cloud-native applications
/// </summary>
public enum AspireAuditEventType
{
    ServiceStartup,
    ServiceShutdown,
    ResourceAllocation,
    ServiceScaling,
    ConfigurationChange,
    HealthCheckFailure,
    DistributedTraceAnomaly,
    UserAuthentication,
    DataAccess,
    DataModification,
    ComplianceViolation,
    SecurityIncident,
    PerformanceAnomaly
}

/// <summary>
/// Aspire audit event with distributed tracing correlation
/// </summary>
public record AspireAuditEvent(
    Guid Id,
    AspireAuditEventType EventType,
    DateTime Timestamp,
    string ServiceName,
    string ServiceVersion,
    string? TraceId,
    string? SpanId,
    string? UserId,
    string Action,
    Dictionary<string, object?> Properties,
    string? ResourceName,
    bool IsSuccessful,
    string? ErrorDetails,
    Dictionary<string, string> Tags)
{
    /// <summary>
    /// Creates correlation ID for distributed audit tracking
    /// </summary>
    public string CorrelationId => TraceId ?? Id.ToString();
}

/// <summary>
/// Aspire-specific audit service with OpenTelemetry integration
/// </summary>
public class AspireAuditService(
    IAuditRepository auditRepository,
    ILogger<AspireAuditService> logger,
    AspireServiceContext serviceContext) : IAspireAuditService
{
    /// <summary>
    /// Logs audit event with distributed tracing context
    /// </summary>
    public async Task LogAuditEventAsync(AspireAuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        using var activity = Activity.Current?.Source.StartActivity("AspireAudit.LogEvent");
        activity?.SetTag("audit.event_type", auditEvent.EventType.ToString());
        activity?.SetTag("audit.service_name", auditEvent.ServiceName);
        activity?.SetTag("audit.correlation_id", auditEvent.CorrelationId);

        try
        {
            // Enrich with Aspire context
            var enrichedEvent = auditEvent with
            {
                TraceId = Activity.Current?.TraceId.ToString(),
                SpanId = Activity.Current?.SpanId.ToString(),
                Tags = auditEvent.Tags.Concat(serviceContext.GetServiceTags()).ToDictionary(x => x.Key, x => x.Value)
            };

            await auditRepository.SaveAuditEventAsync(enrichedEvent, cancellationToken).ConfigureAwait(false);

            // Emit telemetry metrics
            serviceContext.AuditEventCounter.Add(1, 
                new KeyValuePair<string, object?>("event_type", auditEvent.EventType),
                new KeyValuePair<string, object?>("service_name", auditEvent.ServiceName),
                new KeyValuePair<string, object?>("success", auditEvent.IsSuccessful));

            logger.LogInformation("Audit event logged: {EventType} for service {ServiceName} with correlation {CorrelationId}",
                auditEvent.EventType, auditEvent.ServiceName, auditEvent.CorrelationId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to log audit event: {EventType} for service {ServiceName}",
                auditEvent.EventType, auditEvent.ServiceName);
            
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Logs service lifecycle events with proper Aspire context
    /// </summary>
    public async Task LogServiceEventAsync(string action, Dictionary<string, object?>? properties = null, CancellationToken cancellationToken = default)
    {
        var auditEvent = new AspireAuditEvent(
            Id: Guid.NewGuid(),
            EventType: DetermineEventType(action),
            Timestamp: DateTime.UtcNow,
            ServiceName: serviceContext.ServiceName,
            ServiceVersion: serviceContext.ServiceVersion,
            TraceId: Activity.Current?.TraceId.ToString(),
            SpanId: Activity.Current?.SpanId.ToString(),
            UserId: serviceContext.GetCurrentUserId(),
            Action: action,
            Properties: properties ?? new Dictionary<string, object?>(),
            ResourceName: serviceContext.ResourceName,
            IsSuccessful: true,
            ErrorDetails: null,
            Tags: serviceContext.GetServiceTags()
        );

        await LogAuditEventAsync(auditEvent, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Logs compliance-related events with regulatory framework context
    /// </summary>
    public async Task LogComplianceEventAsync(string framework, string requirement, bool isCompliant, 
        Dictionary<string, object?>? evidence = null, CancellationToken cancellationToken = default)
    {
        var properties = new Dictionary<string, object?>
        {
            ["compliance_framework"] = framework,
            ["requirement"] = requirement,
            ["is_compliant"] = isCompliant,
            ["evidence"] = evidence ?? new Dictionary<string, object?>()
        };

        var auditEvent = new AspireAuditEvent(
            Id: Guid.NewGuid(),
            EventType: AspireAuditEventType.ComplianceViolation,
            Timestamp: DateTime.UtcNow,
            ServiceName: serviceContext.ServiceName,
            ServiceVersion: serviceContext.ServiceVersion,
            TraceId: Activity.Current?.TraceId.ToString(),
            SpanId: Activity.Current?.SpanId.ToString(),
            UserId: serviceContext.GetCurrentUserId(),
            Action: $"ComplianceCheck_{framework}",
            Properties: properties,
            ResourceName: serviceContext.ResourceName,
            IsSuccessful: isCompliant,
            ErrorDetails: isCompliant ? null : $"Compliance violation: {framework} - {requirement}",
            Tags: serviceContext.GetServiceTags()
        );

        await LogAuditEventAsync(auditEvent, cancellationToken).ConfigureAwait(false);

        // Alert on compliance violations
        if (!isCompliant)
        {
            logger.LogWarning("Compliance violation detected: {Framework} - {Requirement} in service {ServiceName}",
                framework, requirement, serviceContext.ServiceName);
        }
    }

    private static AspireAuditEventType DetermineEventType(string action) => action.ToLowerInvariant() switch
    {
        "startup" or "start" => AspireAuditEventType.ServiceStartup,
        "shutdown" or "stop" => AspireAuditEventType.ServiceShutdown,
        "scale" or "scaling" => AspireAuditEventType.ServiceScaling,
        "config" or "configuration" => AspireAuditEventType.ConfigurationChange,
        "health" or "healthcheck" => AspireAuditEventType.HealthCheckFailure,
        _ => AspireAuditEventType.DataAccess
    };
}

/// <summary>
/// Aspire service context for audit enrichment
/// </summary>
public class AspireServiceContext(
    IConfiguration configuration,
    IHostEnvironment environment,
    IHttpContextAccessor? httpContextAccessor = null)
{
    public string ServiceName { get; } = configuration["OTEL_SERVICE_NAME"] ?? environment.ApplicationName;
    public string ServiceVersion { get; } = configuration["OTEL_SERVICE_VERSION"] ?? "1.0.0";
    public string ResourceName { get; } = configuration["OTEL_RESOURCE_ATTRIBUTES"] ?? "unknown";
    
    private readonly Counter<long> _auditEventCounter = Metrics.CreateCounter<long>("aspire.audit.events.total", 
        description: "Total number of audit events logged");
    
    public Counter<long> AuditEventCounter => _auditEventCounter;

    /// <summary>
    /// Gets service-specific tags for audit enrichment
    /// </summary>
    public Dictionary<string, string> GetServiceTags() => new()
    {
        ["service.name"] = ServiceName,
        ["service.version"] = ServiceVersion,
        ["service.environment"] = environment.EnvironmentName,
        ["service.instance"] = Environment.MachineName,
        ["aspire.deployment"] = configuration["ASPIRE_DEPLOYMENT"] ?? "local"
    };

    /// <summary>
    /// Gets current user ID from HTTP context or service context
    /// </summary>
    public string? GetCurrentUserId() =>
        httpContextAccessor?.HttpContext?.User?.FindFirst("sub")?.Value ??
        httpContextAccessor?.HttpContext?.User?.Identity?.Name;
}

/// <summary>
/// Compliance reporting service for Aspire applications
/// </summary>
public class AspireComplianceReporter(
    IAuditRepository auditRepository,
    ILogger<AspireComplianceReporter> logger,
    AspireServiceContext serviceContext) : IComplianceReporter
{
    /// <summary>
    /// Generates compliance report across distributed Aspire services
    /// </summary>
    public async Task<ComplianceReport> GenerateComplianceReportAsync(
        string framework, 
        DateTime from, 
        DateTime to, 
        CancellationToken cancellationToken = default)
    {
        using var activity = Activity.Current?.Source.StartActivity("AspireCompliance.GenerateReport");
        activity?.SetTag("compliance.framework", framework);
        activity?.SetTag("report.date_range", $"{from:yyyy-MM-dd} to {to:yyyy-MM-dd}");

        try
        {
            var events = await auditRepository.GetComplianceEventsAsync(framework, from, to, cancellationToken)
                .ConfigureAwait(false);

            var serviceBreakdown = events
                .GroupBy(e => e.ServiceName)
                .ToDictionary(g => g.Key, g => new ServiceComplianceMetrics
                {
                    TotalEvents = g.Count(),
                    CompliantEvents = g.Count(e => e.IsSuccessful),
                    ViolationCount = g.Count(e => !e.IsSuccessful),
                    ComplianceRate = g.Any() ? (double)g.Count(e => e.IsSuccessful) / g.Count() * 100 : 100
                });

            var report = new ComplianceReport
            {
                Framework = framework,
                ReportPeriod = new DateRange(from, to),
                GeneratedAt = DateTime.UtcNow,
                OverallComplianceRate = CalculateOverallComplianceRate(events),
                ServiceBreakdown = serviceBreakdown,
                Violations = events.Where(e => !e.IsSuccessful).ToList(),
                Summary = GenerateComplianceSummary(events, framework),
                GeneratedBy = serviceContext.ServiceName
            };

            logger.LogInformation("Compliance report generated for {Framework}: {ComplianceRate:F2}% compliance across {ServiceCount} services",
                framework, report.OverallComplianceRate, serviceBreakdown.Count);

            return report;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate compliance report for framework: {Framework}", framework);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    private static double CalculateOverallComplianceRate(List<AspireAuditEvent> events) =>
        events.Count == 0 ? 100.0 : (double)events.Count(e => e.IsSuccessful) / events.Count * 100;

    private static string GenerateComplianceSummary(List<AspireAuditEvent> events, string framework)
    {
        var violations = events.Where(e => !e.IsSuccessful).ToList();
        var complianceRate = CalculateOverallComplianceRate(events);
        
        return $"{framework} Compliance Summary: {complianceRate:F2}% compliance rate with {violations.Count} violations identified across {events.GroupBy(e => e.ServiceName).Count()} services.";
    }
}

// Supporting interfaces and models
public interface IAspireAuditService
{
    Task LogAuditEventAsync(AspireAuditEvent auditEvent, CancellationToken cancellationToken = default);
    Task LogServiceEventAsync(string action, Dictionary<string, object?>? properties = null, CancellationToken cancellationToken = default);
    Task LogComplianceEventAsync(string framework, string requirement, bool isCompliant, Dictionary<string, object?>? evidence = null, CancellationToken cancellationToken = default);
}

public interface IComplianceReporter
{
    Task<ComplianceReport> GenerateComplianceReportAsync(string framework, DateTime from, DateTime to, CancellationToken cancellationToken = default);
}

public record DateRange(DateTime From, DateTime To);

public record ServiceComplianceMetrics
{
    public int TotalEvents { get; init; }
    public int CompliantEvents { get; init; }
    public int ViolationCount { get; init; }
    public double ComplianceRate { get; init; }
}

public record ComplianceReport
{
    public string Framework { get; init; } = string.Empty;
    public DateRange ReportPeriod { get; init; } = new(DateTime.MinValue, DateTime.MaxValue);
    public DateTime GeneratedAt { get; init; }
    public double OverallComplianceRate { get; init; }
    public Dictionary<string, ServiceComplianceMetrics> ServiceBreakdown { get; init; } = new();
    public List<AspireAuditEvent> Violations { get; init; } = new();
    public string Summary { get; init; } = string.Empty;
    public string GeneratedBy { get; init; } = string.Empty;
}
```

**Usage**:

```csharp
// Program.cs - Aspire Host Configuration
var builder = DistributedApplication.CreateBuilder(args);

// Add audit and compliance services
builder.Services.AddSingleton<AspireServiceContext>();
builder.Services.AddScoped<IAspireAuditService, AspireAuditService>();
builder.Services.AddScoped<IComplianceReporter, AspireComplianceReporter>();

// Add distributed tracing for audit correlation
builder.Services.AddOpenTelemetry()
    .WithTracing(tracingBuilder =>
    {
        tracingBuilder.AddSource("AspireAudit.*");
    });

var app = builder.Build();

// Service implementation example
public class DocumentService(IAspireAuditService auditService)
{
    public async Task ProcessDocumentAsync(Guid documentId, string action)
    {
        // Log service action with audit trail
        await auditService.LogServiceEventAsync($"ProcessDocument_{action}", new Dictionary<string, object?>
        {
            ["document_id"] = documentId,
            ["processing_timestamp"] = DateTime.UtcNow
        });

        // Compliance check example
        await auditService.LogComplianceEventAsync(
            framework: "GDPR",
            requirement: "DataProcessingConsent",
            isCompliant: await ValidateGDPRConsentAsync(documentId),
            evidence: new Dictionary<string, object?>
            {
                ["consent_timestamp"] = DateTime.UtcNow,
                ["document_id"] = documentId
            });
    }

    private async Task<bool> ValidateGDPRConsentAsync(Guid documentId)
    {
        // Implementation for GDPR consent validation
        return true;
    }
}

// Compliance reporting example
public async Task GenerateMonthlyComplianceReport()
{
    var reporter = serviceProvider.GetRequiredService<IComplianceReporter>();
    
    var report = await reporter.GenerateComplianceReportAsync(
        framework: "SOX",
        from: DateTime.UtcNow.AddMonths(-1),
        to: DateTime.UtcNow);
    
    Console.WriteLine($"SOX Compliance Rate: {report.OverallComplianceRate:F2}%");
    Console.WriteLine($"Violations: {report.Violations.Count}");
}
```

**Notes**:

- **Distributed Tracing Integration**: Correlates audit events across Aspire services using OpenTelemetry
- **Service Orchestration**: Integrates with Aspire service discovery and orchestration patterns  
- **Cloud-Native Observability**: Uses structured logging, metrics, and distributed tracing
- **Compliance Frameworks**: Supports SOX, GDPR, HIPAA, and custom regulatory requirements
- **Performance**: Optimized for high-throughput cloud environments with async patterns
- **Security**: Implements secure audit trail patterns with correlation IDs
- **Scalability**: Designed for distributed microservices architecture with Aspire
- **Configuration**: Uses Aspire configuration patterns and environment detection
- **Modern C# Features**: Leverages primary constructors, records, and expression-bodied members
