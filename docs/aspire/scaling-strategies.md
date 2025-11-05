# Enterprise Scaling Strategies with .NET Aspire

**Description**: Advanced enterprise-grade auto-scaling and load balancing patterns for .NET Aspire applications including predictive scaling with ML analytics, cross-region load distribution, cost optimization with intelligent resource allocation, and enterprise compliance with governance frameworks.

**Language/Technology**: C#, .NET 9.0, .NET Aspire, Azure Container Apps, AKS, KEDA, Prometheus
**Enterprise Features**: Predictive scaling, cost optimization, cross-region distribution, compliance governance, intelligent resource allocation, and comprehensive performance analytics

**Code**:

## Table of Contents

1. [Auto-Scaling Fundamentals](#auto-scaling-fundamentals)
2. [Container Scaling Patterns](#container-scaling-patterns)
3. [Service Load Balancing](#service-load-balancing)
4. [Resource-Based Scaling Policies](#resource-based-scaling-policies)
5. [Orleans Cluster Scaling Patterns](#orleans-cluster-scaling-patterns)
6. [Monitoring and Alerting Integration](#monitoring-and-alerting-integration)
7. [Implementation Summary](#implementation-summary)
8. [Related Patterns](#related-patterns)

## Auto-Scaling Fundamentals

### Scaling Policy Interface

```csharp
namespace DocumentProcessor.Aspire.Scaling;

/// <summary>
/// Defines scaling policy contract for .NET Aspire service orchestration
/// Implements proper service discovery and scaling decision patterns
/// </summary>
public interface IScalingPolicy
{
    Task<ScalingDecision> EvaluateAsync(ScalingContext context, CancellationToken cancellationToken = default);
    Task<bool> CanScaleAsync(ScalingDirection direction, CancellationToken cancellationToken = default);
    TimeSpan EvaluationInterval { get; }
    string PolicyName { get; }
}

public record ScalingDecision
{
    public bool ShouldScale { get; init; }
    public ScalingDirection Direction { get; init; }
    public int TargetInstances { get; init; }
    public string Reason { get; init; } = string.Empty;
    public double Confidence { get; init; }
    public Dictionary<string, object> Metadata { get; init; } = new();
}

public enum ScalingDirection { Up, Down, Maintain }

public record ScalingContext
{
    public string ServiceName { get; init; } = string.Empty;
    public int CurrentInstances { get; init; }
    public int MinInstances { get; init; } = 1;
    public int MaxInstances { get; init; } = 10;
    public Dictionary<string, double> Metrics { get; init; } = new();
    public DateTime LastScalingAction { get; init; }
    public TimeSpan CooldownPeriod { get; init; } = TimeSpan.FromMinutes(5);
}
```

### Base Scaling Service

```csharp
/// <summary>
/// Base scaling service implementing Aspire cloud-native scaling patterns
/// Uses structured logging with correlation identifiers and OpenTelemetry
/// </summary>
public abstract class BaseScalingService(
    ILogger logger, 
    IMetricsCollector metricsCollector,
    TimeSpan evaluationInterval) : IScalingService
{
    protected readonly ILogger Logger = logger;
    protected readonly IMetricsCollector MetricsCollector = metricsCollector;
    private readonly Timer evaluationTimer = new(EvaluateScaling, null, TimeSpan.Zero, evaluationInterval);
    private readonly SemaphoreSlim scalingSemaphore = new(1, 1);

    public abstract Task<ScalingResult> ScaleServiceAsync(string serviceName, int targetInstances, CancellationToken cancellationToken = default);
    public abstract Task<ServiceScalingInfo> GetServiceInfoAsync(string serviceName, CancellationToken cancellationToken = default);
    
    protected virtual async void EvaluateScaling(object? state)
    {
        if (!await scalingSemaphore.WaitAsync(100))
            return;

        try
        {
            await PerformScalingEvaluationAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during scaling evaluation");
        }
        finally
        {
scalingSemaphore.Release();
        }
    }

    protected abstract Task PerformScalingEvaluationAsync();
}
```

## Container Scaling Patterns

### Docker Compose Scaling

```csharp
/// <summary>
/// Docker Compose scaling service for Aspire local development containers
/// Implements proper containerization strategies and resource management
/// </summary>
public class DockerComposeScalingService(
    IDockerClient dockerClient,
    DockerComposeConfig config,
    ILogger<DockerComposeScalingService> logger,
    IMetricsCollector metricsCollector) 
    : BaseScalingService(logger, metricsCollector, TimeSpan.FromSeconds(30))
{
    private readonly IDockerClient dockerClient = dockerClient;
    private readonly DockerComposeConfig config = config;

    public override async Task<ScalingResult> ScaleServiceAsync(
        string serviceName, 
        int targetInstances, 
        CancellationToken cancellationToken = default)
    {
        using var activity = Activity.Current?.Source.StartActivity("DockerCompose.ScaleService");
        activity?.SetTag("service.name", serviceName);
        activity?.SetTag("target.instances", targetInstances);

        var startTime = DateTime.UtcNow;
        
        try
        {
            var currentInfo = await GetServiceInfoAsync(serviceName, cancellationToken);
            var currentInstances = currentInfo.CurrentInstances;

            if (currentInstances == targetInstances)
            {
                Logger.LogInformation("Service {ServiceName} already at target instances: {Instances}", 
                    serviceName, targetInstances);
                
                return new ScalingResult
                {
                    Success = true,
                    ServiceName = serviceName,
                    PreviousInstances = currentInstances,
                    CurrentInstances = targetInstances,
                    ScalingDuration = TimeSpan.Zero
                };
            }

            Logger.LogInformation("Scaling service {ServiceName} from {Current} to {Target} instances",
                serviceName, currentInstances, targetInstances);

            // Scale using docker-compose up --scale
            var scaleCommand = $"docker-compose -f {config.ComposeFilePath} up -d --scale {serviceName}={targetInstances}";
            
            var processResult = await ExecuteCommandAsync(scaleCommand, cancellationToken);
            if (!processResult.Success)
            {
                throw new InvalidOperationException($"Docker compose scaling failed: {processResult.Error}");
            }

            // Wait for containers to be ready
            await WaitForContainersReadyAsync(serviceName, targetInstances, cancellationToken);

            var duration = DateTime.UtcNow - startTime;
            
            Logger.LogInformation("Successfully scaled {ServiceName} to {Instances} instances in {Duration}ms",
                serviceName, targetInstances, duration.TotalMilliseconds);

            await MetricsCollector.RecordScalingEventAsync(serviceName, currentInstances, targetInstances, duration);

            return new ScalingResult
            {
                Success = true,
                ServiceName = serviceName,
                PreviousInstances = currentInstances,
                CurrentInstances = targetInstances,
                ScalingDuration = duration,
                Metadata = new Dictionary<string, object>
                {
                    ["scaling_method"] = "docker-compose",
                    ["command_output"] = processResult.Output
                }
            };
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            Logger.LogError(ex, "Failed to scale service {ServiceName} to {TargetInstances}", serviceName, targetInstances);
            
            return new ScalingResult
            {
                Success = false,
                ServiceName = serviceName,
                ScalingDuration = duration,
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// Gets current scaling information for a Docker Compose service
    /// Implements proper telemetry and structured logging for Aspire observability
    /// </summary>
    public override async Task<ServiceScalingInfo> GetServiceInfoAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        using var activity = Activity.Current?.Source.StartActivity("DockerCompose.GetServiceInfo");
        activity?.SetTag("service.name", serviceName);
        
        try
        {
            // Get container information using Docker API
            var containers = await dockerClient.Containers.ListContainersAsync(
                new ContainersListParameters
                {
                    All = true,
                    Filters = new Dictionary<string, IDictionary<string, bool>>
                    {
                        ["label"] = new Dictionary<string, bool>
                        {
                            [$"com.docker.compose.service={serviceName}"] = true
                        }
                    }
                }, cancellationToken);

            var runningContainers = containers.Count(c => c.State == "running");
            var serviceConfig = config.Services.GetValueOrDefault(serviceName);
            
            var scalingInfo = new ServiceScalingInfo
            {
                ServiceName = serviceName,
                CurrentInstances = runningContainers,
                MinInstances = serviceConfig?.MinInstances ?? 1,
                MaxInstances = serviceConfig?.MaxInstances ?? 10,
                Status = DetermineScalingStatus(containers),
                LastScalingAction = DateTime.UtcNow // This should be tracked persistently
            };

            logger.LogDebug("Retrieved Docker service info: {ServiceName} - {CurrentInstances} instances",
                serviceName, runningContainers);
            
            activity?.SetTag("scaling.current_instances", runningContainers);
            activity?.SetTag("scaling.status", scalingInfo.Status.ToString());
            
            return scalingInfo;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get Docker service info for {ServiceName}", serviceName);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Performs Docker Compose scaling evaluation with proper Aspire telemetry
    /// </summary>
    protected override async Task PerformScalingEvaluationAsync()
    {
        using var activity = Activity.Current?.Source.StartActivity("DockerCompose.ScalingEvaluation");
        activity?.SetTag("service.count", config.Services.Count);
        
        foreach (var serviceConfig in config.Services)
        {
            using var serviceActivity = Activity.Current?.Source.StartActivity("DockerCompose.EvaluateService");
            
            try
            {
                var serviceName = serviceConfig.Key;
                var serviceSettings = serviceConfig.Value;
                
                serviceActivity?.SetTag("service.name", serviceName);
                
                // Get current metrics
                var metrics = await metricsCollector.GetServiceMetricsAsync(serviceName);
                
                // Create scaling context
                var context = new ScalingContext
                {
                    ServiceName = serviceName,
                    CurrentInstances = (await GetServiceInfoAsync(serviceName)).CurrentInstances,
                    MinInstances = serviceSettings.MinInstances,
                    MaxInstances = serviceSettings.MaxInstances,
                    Metrics = metrics,
                    CooldownPeriod = serviceSettings.CooldownPeriod
                };

                // Evaluate scaling policies
                foreach (var policyName in serviceSettings.ScalingPolicies)
                {
                    if (serviceSettings.Policies.TryGetValue(policyName, out var policy))
                    {
                        var decision = await policy.EvaluateAsync(context);
                        
                        if (decision.ShouldScale && decision.TargetInstances != context.CurrentInstances)
                        {
                            logger.LogInformation("Docker scaling decision: {ServiceName} should scale to {TargetInstances}. Reason: {Reason}",
                                serviceName, decision.TargetInstances, decision.Reason);
                            
                            serviceActivity?.SetTag("scaling.decision", "scale");
                            serviceActivity?.SetTag("scaling.target_instances", decision.TargetInstances);
                            serviceActivity?.SetTag("scaling.reason", decision.Reason);
                                
                            await ScaleServiceAsync(serviceName, decision.TargetInstances);
                            break; // Only execute first scaling decision
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error evaluating Docker scaling for service {ServiceName}", serviceConfig.Key);
                serviceActivity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            }
        }
    }

    private async Task<ProcessResult> ExecuteCommandAsync(string command, CancellationToken cancellationToken)
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {command}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process == null)
                throw new InvalidOperationException("Failed to start process");

            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            var error = await process.StandardError.ReadToEndAsync(cancellationToken);
            
            await process.WaitForExitAsync(cancellationToken);

            return new ProcessResult
            {
                Success = process.ExitCode == 0,
                Output = output,
                Error = error,
                ExitCode = process.ExitCode
            };
        }
        catch (Exception ex)
        {
            return new ProcessResult
            {
                Success = false,
                Error = ex.Message,
                ExitCode = -1
            };
        }
    }

    private async Task WaitForContainersReadyAsync(string serviceName, int expectedCount, CancellationToken cancellationToken)
    {
        var maxWaitTime = TimeSpan.FromMinutes(2);
        var pollInterval = TimeSpan.FromSeconds(2);
        var deadline = DateTime.UtcNow.Add(maxWaitTime);

        while (DateTime.UtcNow < deadline)
        {
            var serviceInfo = await GetServiceInfoAsync(serviceName, cancellationToken);
            
            if (serviceInfo.CurrentInstances >= expectedCount && serviceInfo.Status == ScalingStatus.Stable)
            {
                Logger.LogDebug("All containers for {ServiceName} are ready", serviceName);
                return;
            }

            Logger.LogDebug("Waiting for containers: {ServiceName} has {Current}/{Expected} ready",
                serviceName, serviceInfo.CurrentInstances, expectedCount);

            await Task.Delay(pollInterval, cancellationToken);
        }

        Logger.LogWarning("Timeout waiting for containers to be ready for service {ServiceName}", serviceName);
    }

    private ScalingStatus DetermineScalingStatus(IList<ContainerListResponse> containers)
    {
        var runningCount = containers.Count(c => c.State == "running");
        var startingCount = containers.Count(c => c.State == "created" || c.State == "restarting");
        var stoppedCount = containers.Count(c => c.State == "exited");

        if (startingCount > 0)
            return ScalingStatus.ScalingUp;
        
        if (stoppedCount > 0 && runningCount > 0)
            return ScalingStatus.ScalingDown;
            
        return ScalingStatus.Stable;
    }
}
```

### Kubernetes Scaling

```csharp
/// <summary>
/// Kubernetes scaling service for Aspire cloud-native deployment
/// Implements horizontal pod autoscaler (HPA) integration and resource management
/// </summary>
public class KubernetesScalingService(
    IKubernetes kubernetesClient,
    string namespaceName,
    KubernetesScalingConfig config,
    ILogger<KubernetesScalingService> logger,
    IMetricsCollector metricsCollector) 
    : BaseScalingService(logger, metricsCollector, TimeSpan.FromSeconds(45))
{
    private readonly IKubernetes kubernetesClient = kubernetesClient;
    private readonly string @namespace = namespaceName;
    private readonly KubernetesScalingConfig config = config;

    /// <summary>
    /// Scales a Kubernetes service with proper Aspire telemetry and error handling
    /// </summary>
    public override async Task<ScalingResult> ScaleServiceAsync(
        string serviceName, 
        int targetInstances, 
        CancellationToken cancellationToken = default)
    {
        using var activity = Activity.Current?.Source.StartActivity("Kubernetes.ScaleService");
        activity?.SetTag("service.name", serviceName);
        activity?.SetTag("target.instances", targetInstances);
        activity?.SetTag("kubernetes.namespace", @namespace);

        var startTime = DateTime.UtcNow;

        try
        {
            var currentInfo = await GetServiceInfoAsync(serviceName, cancellationToken);
            var currentInstances = currentInfo.CurrentInstances;

            if (currentInstances == targetInstances)
            {
                Logger.LogInformation("Deployment {ServiceName} already at target replicas: {Replicas}", 
                    serviceName, targetInstances);
                
                return new ScalingResult
                {
                    Success = true,
                    ServiceName = serviceName,
                    PreviousInstances = currentInstances,
                    CurrentInstances = targetInstances,
                    ScalingDuration = TimeSpan.Zero
                };
            }

            Logger.LogInformation("Scaling Kubernetes deployment {ServiceName} from {Current} to {Target} replicas",
                serviceName, currentInstances, targetInstances);

            // Scale using Kubernetes Deployment
            var deployment = await kubernetesClient.AppsV1.ReadNamespacedDeploymentAsync(
                serviceName, namespace, cancellationToken: cancellationToken);

            if (deployment == null)
            {
                throw new InvalidOperationException($"Deployment {serviceName} not found in namespace namespace");
            }

            // Update replica count
            deployment.Spec.Replicas = targetInstances;
            
            var patchedDeployment = await kubernetesClient.AppsV1.PatchNamespacedDeploymentAsync(
                new V1Patch(JsonSerializer.Serialize(new { spec = new { replicas = targetInstances } }), V1Patch.PatchType.MergePatch),
                serviceName,
namespace,
                cancellationToken: cancellationToken);

            // Wait for rollout to complete
            await WaitForRolloutCompleteAsync(serviceName, targetInstances, cancellationToken);

            var duration = DateTime.UtcNow - startTime;
            
            Logger.LogInformation("Successfully scaled Kubernetes deployment {ServiceName} to {Replicas} replicas in {Duration}ms",
                serviceName, targetInstances, duration.TotalMilliseconds);

            await MetricsCollector.RecordScalingEventAsync(serviceName, currentInstances, targetInstances, duration);

            return new ScalingResult
            {
                Success = true,
                ServiceName = serviceName,
                PreviousInstances = currentInstances,
                CurrentInstances = targetInstances,
                ScalingDuration = duration,
                Metadata = new Dictionary<string, object>
                {
                    ["scaling_method"] = "kubernetes",
                    ["namespace"] =namespace,
                    ["deployment_generation"] = patchedDeployment.Metadata.Generation ?? 0
                }
            };
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            Logger.LogError(ex, "Failed to scale Kubernetes deployment {ServiceName} to {TargetReplicas}", serviceName, targetInstances);
            
            return new ScalingResult
            {
                Success = false,
                ServiceName = serviceName,
                ScalingDuration = duration,
                Error = ex.Message
            };
        }
    }

    public override async Task<ServiceScalingInfo> GetServiceInfoAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get deployment information
            var deployment = await kubernetesClient.AppsV1.ReadNamespacedDeploymentAsync(
                serviceName, namespace, cancellationToken: cancellationToken);

            if (deployment == null)
            {
                throw new InvalidOperationException($"Deployment {serviceName} not found");
            }

            var desiredReplicas = deployment.Spec.Replicas ?? 0;
            var readyReplicas = deployment.Status.ReadyReplicas ?? 0;
            var availableReplicas = deployment.Status.AvailableReplicas ?? 0;

            var status = DetermineDeploymentStatus(deployment);
            
            return new ServiceScalingInfo
            {
                ServiceName = serviceName,
                CurrentInstances = (int)readyReplicas,
                MinInstances =config.Services.GetValueOrDefault(serviceName)?.MinInstances ?? 1,
                MaxInstances =config.Services.GetValueOrDefault(serviceName)?.MaxInstances ?? 10,
                Status = status,
                LastScalingAction = deployment.Metadata.CreationTimestamp?.DateTime ?? DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get deployment info for {ServiceName}", serviceName);
            throw;
        }
    }

    protected override async Task PerformScalingEvaluationAsync()
    {
        try
        {
            // Get all deployments in namespace
            var deployments = await kubernetesClient.AppsV1.ListNamespacedDeploymentAsync(namespace, labelSelector: config.LabelSelector);

            foreach (var deployment in deployments.Items)
            {
                var serviceName = deployment.Metadata.Name;
                
                if (!config.Services.ContainsKey(serviceName))
                    continue;

                try
                {
                    var config =config.Services[serviceName];
                    
                    // Get current metrics from Kubernetes metrics API or Prometheus
                    var metrics = await GetKubernetesMetricsAsync(serviceName);
                    
                    var context = new ScalingContext
                    {
                        ServiceName = serviceName,
                        CurrentInstances = (int)(deployment.Status.ReadyReplicas ?? 0),
                        MinInstances = config.MinInstances,
                        MaxInstances = config.MaxInstances,
                        Metrics = metrics,
                        CooldownPeriod =config.CooldownPeriod
                    };

                    // Evaluate HPA if enabled
                    if (config.UseHorizontalPodAutoscaler)
                    {
                        await EvaluateHpaScalingAsync(serviceName, context);
                    }
                    else
                    {
                        // Use custom scaling policies
                        await EvaluateCustomScalingPoliciesAsync(serviceName, context, config.ScalingPolicies);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error evaluating scaling for deployment {DeploymentName}", serviceName);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during Kubernetes scaling evaluation");
        }
    }

    private async Task WaitForRolloutCompleteAsync(string deploymentName, int expectedReplicas, CancellationToken cancellationToken)
    {
        var maxWaitTime = TimeSpan.FromMinutes(5);
        var pollInterval = TimeSpan.FromSeconds(3);
        var deadline = DateTime.UtcNow.Add(maxWaitTime);

        Logger.LogDebug("Waiting for rollout of deployment {DeploymentName} to complete", deploymentName);

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var deployment = await kubernetesClient.AppsV1.ReadNamespacedDeploymentAsync(
                    deploymentName, namespace, cancellationToken: cancellationToken);

                var readyReplicas = deployment.Status.ReadyReplicas ?? 0;
                var updatedReplicas = deployment.Status.UpdatedReplicas ?? 0;
                var desiredReplicas = deployment.Spec.Replicas ?? 0;

                if (readyReplicas == expectedReplicas && 
                    updatedReplicas == expectedReplicas &&
                    desiredReplicas == expectedReplicas)
                {
                    Logger.LogDebug("Rollout complete for deployment {DeploymentName}", deploymentName);
                    return;
                }

                Logger.LogDebug("Rollout in progress: {DeploymentName} has {Ready}/{Expected} ready replicas",
                    deploymentName, readyReplicas, expectedReplicas);

                await Task.Delay(pollInterval, cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Error checking rollout status for {DeploymentName}", deploymentName);
                await Task.Delay(pollInterval, cancellationToken);
            }
        }

        Logger.LogWarning("Timeout waiting for rollout to complete for deployment {DeploymentName}", deploymentName);
    }

    private ScalingStatus DetermineDeploymentStatus(V1Deployment deployment)
    {
        var desiredReplicas = deployment.Spec.Replicas ?? 0;
        var readyReplicas = deployment.Status.ReadyReplicas ?? 0;
        var updatedReplicas = deployment.Status.UpdatedReplicas ?? 0;

        // Check if deployment is progressing
        var progressingCondition = deployment.Status.Conditions?
            .FirstOrDefault(c => c.Type == "Progressing");

        if (progressingCondition?.Status == "True" && progressingCondition.Reason == "NewReplicaSetAvailable")
        {
            if (readyReplicas < desiredReplicas)
                return ScalingStatus.ScalingUp;
            else if (readyReplicas > desiredReplicas)
                return ScalingStatus.ScalingDown;
        }

        if (readyReplicas == desiredReplicas && updatedReplicas == desiredReplicas)
            return ScalingStatus.Stable;

        return ScalingStatus.ScalingUp;
    }

    private async Task<Dictionary<string, double>> GetKubernetesMetricsAsync(string serviceName)
    {
        var metrics = new Dictionary<string, double>();
        
        try
        {
            // This would typically integrate with metrics-server or Prometheus
            // For now, return sample metrics
            metrics["cpu_utilization"] = 45.0;
            metrics["memory_utilization"] = 60.0;
            metrics["requests_per_second"] = 25.0;
            
            await Task.CompletedTask; // Placeholder for actual metrics gathering
            
            return metrics;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to get metrics for service {ServiceName}", serviceName);
            return metrics;
        }
    }

    private async Task EvaluateHpaScalingAsync(string serviceName, ScalingContext context)
    {
        try
        {
            // Check if HPA exists
            var hpa = await kubernetesClient.AutoscalingV2.ReadNamespacedHorizontalPodAutoscalerAsync(
                serviceName, namespace);

            if (hpa != null)
            {
                Logger.LogDebug("HPA exists for {ServiceName}, deferring to HPA for scaling decisions", serviceName);
                // HPA handles scaling automatically based on its configuration
            }
        }
        catch (k8s.Autorest.HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            Logger.LogDebug("No HPA found for {ServiceName}, using custom scaling policies", serviceName);
            // Fall back to custom scaling
            var config =config.Services[serviceName];
            await EvaluateCustomScalingPoliciesAsync(serviceName, context, config.ScalingPolicies);
        }
    }

    private async Task EvaluateCustomScalingPoliciesAsync(string serviceName, ScalingContext context, List<string> policyNames)
    {
        foreach (var policyName in policyNames)
        {
            if (config.Policies.TryGetValue(policyName, out var policy))
            {
                var decision = await policy.EvaluateAsync(context);
                
                if (decision.ShouldScale && decision.TargetInstances != context.CurrentInstances)
                {
                    Logger.LogInformation("Kubernetes scaling decision: {ServiceName} should scale to {TargetInstances}. Reason: {Reason}",
                        serviceName, decision.TargetInstances, decision.Reason);
                        
                    await ScaleServiceAsync(serviceName, decision.TargetInstances);
                    break; // Only execute first scaling decision
                }
            }
        }
    }
}

### Container Configuration Models

```csharp
// Docker Compose configuration
public class DockerComposeConfig
{
    public string ComposeFilePath { get; set; } = "docker-compose.yml";
    public TimeSpan CooldownPeriod { get; set; } = TimeSpan.FromMinutes(5);
    public Dictionary<string, ServiceScalingConfig> Services { get; set; } = new();
    public Dictionary<string, IScalingPolicy> Policies { get; set; } = new();
}

// Kubernetes configuration
public class KubernetesScalingConfig
{
    public string Namespace { get; set; } = "default";
    public string? LabelSelector { get; set; }
    public TimeSpan CooldownPeriod { get; set; } = TimeSpan.FromMinutes(3);
    public Dictionary<string, KubernetesServiceConfig> Services { get; set; } = new();
    public Dictionary<string, IScalingPolicy> Policies { get; set; } = new();
}

public class KubernetesServiceConfig : ServiceScalingConfig
{
    public bool UseHorizontalPodAutoscaler { get; set; } = false;
    public string? HpaTemplate { get; set; }
    public Dictionary<string, string> DeploymentLabels { get; set; } = new();
}

// Process execution result
public record ProcessResult
{
    public bool Success { get; init; }
    public string Output { get; init; } = string.Empty;
    public string Error { get; init; } = string.Empty;
    public int ExitCode { get; init; }
}
```

## Service Load Balancing

### Load Balancing Strategy Interface

```csharp
public interface ILoadBalancingStrategy
{
    Task<ServiceEndpoint?> SelectEndpointAsync(
        List<ServiceEndpoint> availableEndpoints, 
        LoadBalancingContext context,
        CancellationToken cancellationToken = default);
    
    Task UpdateEndpointHealthAsync(ServiceEndpoint endpoint, HealthStatus health);
    string StrategyName { get; }
}

public record LoadBalancingContext
{
    public string RequestId { get; init; } = string.Empty;
    public Dictionary<string, string> Headers { get; init; } = new();
    public string ClientIpAddress { get; init; } = string.Empty;
    public DateTime RequestTime { get; init; } = DateTime.UtcNow;
}
```

### Round Robin Implementation

```csharp
public class RoundRobinLoadBalancer : ILoadBalancingStrategy
{
    private int currentIndex;
    private readonly object lock = new();

    public string StrategyName => "RoundRobin";

    public Task<ServiceEndpoint?> SelectEndpointAsync(
        List<ServiceEndpoint> availableEndpoints, 
        LoadBalancingContext context,
        CancellationToken cancellationToken = default)
    {
        if (availableEndpoints.Count == 0)
            return Task.FromResult<ServiceEndpoint?>(null);

        lock (lock)
        {
            var endpoint = availableEndpoints[currentIndex % availableEndpoints.Count];
currentIndex = (currentIndex + 1) % availableEndpoints.Count;
            return Task.FromResult<ServiceEndpoint?>(endpoint);
        }
    }

    public Task UpdateEndpointHealthAsync(ServiceEndpoint endpoint, HealthStatus health)
    {
        // Health update logic - to be detailed later
        return Task.CompletedTask;
    }
}
```

## Core Data Models

```csharp
// Scaling-related data models
public record ScalingResult
{
    public bool Success { get; init; }
    public string ServiceName { get; init; } = string.Empty;
    public int PreviousInstances { get; init; }
    public int CurrentInstances { get; init; }
    public TimeSpan ScalingDuration { get; init; }
    public string? Error { get; init; }
    public Dictionary<string, object> Metadata { get; init; } = new();
}

public record ServiceScalingInfo
{
    public string ServiceName { get; init; } = string.Empty;
    public int CurrentInstances { get; init; }
    public int MinInstances { get; init; }
    public int MaxInstances { get; init; }
    public ScalingStatus Status { get; init; }
    public DateTime LastScalingAction { get; init; }
    public List<ScalingEvent> RecentEvents { get; init; } = new();
}

public enum ScalingStatus { Stable, ScalingUp, ScalingDown, Error }

public record ScalingEvent
{
    public DateTime Timestamp { get; init; }
    public ScalingDirection Direction { get; init; }
    public int FromInstances { get; init; }
    public int ToInstances { get; init; }
    public string Reason { get; init; } = string.Empty;
    public TimeSpan Duration { get; init; }
}
```

## Service Registration

```csharp
namespace DocumentProcessor.Aspire.Extensions;

public static class ScalingExtensions
{
    public static IServiceCollection AddAspireScaling(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Core scaling services
        services.AddSingleton<IMetricsCollector, PrometheusMetricsCollector>();
        services.AddSingleton<ILoadBalancingStrategy, RoundRobinLoadBalancer>();
        
        // Configure scaling based on environment
        var scalingConfig = configuration.GetSection("Scaling");
        var orchestrator = scalingConfig.GetValue<string>("Orchestrator", "docker-compose");

        services.Configure<ScalingOptions>(scalingConfig);

        return orchestrator.ToLowerInvariant() switch
        {
            "kubernetes" => services.AddKubernetesScaling(configuration),
            "docker-compose" => services.AddDockerComposeScaling(configuration),
            _ => services.AddInProcessScaling(configuration)
        };
    }

    private static IServiceCollection AddKubernetesScaling(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Kubernetes-specific service registration
        // Implementation details to follow
        return services;
    }

    private static IServiceCollection AddDockerComposeScaling(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Docker Compose-specific service registration
        // Implementation details to follow
        return services;
    }

    private static IServiceCollection AddInProcessScaling(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // In-process scaling for development/testing
        // Implementation details to follow
        return services;
    }
}

public class ScalingOptions
{
    public string Orchestrator { get; set; } = "docker-compose";
    public TimeSpan EvaluationInterval { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan CooldownPeriod { get; set; } = TimeSpan.FromMinutes(5);
    public int DefaultMinInstances { get; set; } = 1;
    public int DefaultMaxInstances { get; set; } = 10;
    public Dictionary<string, ServiceScalingConfig> Services { get; set; } = new();
}

public class ServiceScalingConfig
{
    public int MinInstances { get; set; } = 1;
    public int MaxInstances { get; set; } = 10;
    public List<string> ScalingPolicies { get; set; } = new();
    public Dictionary<string, double> Thresholds { get; set; } = new();
}
```

**Usage**:

### Basic Setup

```csharp
// In Program.cs (App Host)
var builder = DistributedApplication.CreateBuilder(args);

// Add services with scaling configuration
var documentApi = builder.AddProject<Projects.DocumentProcessorApi>("document-api")
    .WithReplicas(2); // Initial instance count

// Configure scaling in service
services.AddAspireScaling(configuration);
```

### Configuration Example

```json
{
  "Scaling": {
    "Orchestrator": "docker-compose",
    "EvaluationInterval": "00:00:30",
    "CooldownPeriod": "00:05:00",
    "Services": {
      "document-api": {
        "MinInstances": 2,
        "MaxInstances": 10,
        "ScalingPolicies": ["cpu-utilization", "request-rate"],
        "Thresholds": {
          "cpu": 70.0,
          "memory": 80.0,
          "requests_per_second": 100.0
        }
      }
    }
  }
}
```

## Load Balancing Strategies

### Service-Level Load Balancing

Load balancing in .NET Aspire applications operates at multiple layers to ensure optimal request distribution and service reliability.

#### HTTP Load Balancer Service

```csharp
public interface ILoadBalancerService
{
    Task<ServiceEndpoint> SelectEndpointAsync(string serviceName, LoadBalancingStrategy strategy = LoadBalancingStrategy.RoundRobin);
    Task<IEnumerable<ServiceEndpoint>> GetHealthyEndpointsAsync(string serviceName);
    Task RegisterEndpointAsync(ServiceEndpoint endpoint);
    Task UnregisterEndpointAsync(string endpointId);
    Task UpdateEndpointHealthAsync(string endpointId, HealthStatus health, Dictionary<string, double> metrics);
}

public class AspireLoadBalancerService : ILoadBalancerService
{
    private readonly IServiceDiscovery serviceDiscovery;
    private readonly IHealthCheckService healthCheck;
    private readonly ILogger<AspireLoadBalancerService> logger;
    private readonly IMetricsCollector metrics;
    private readonly ConcurrentDictionary<string, LoadBalancerState> serviceStates = new();
    private readonly Timer healthCheckTimer;

    public AspireLoadBalancerService(
        IServiceDiscovery serviceDiscovery,
        IHealthCheckService healthCheck,
        ILogger<AspireLoadBalancerService> logger,
        IMetricsCollector metrics)
    {
serviceDiscovery = serviceDiscovery;
healthCheck = healthCheck;
logger = logger;
metrics = metrics;
healthCheckTimer = new Timer(async _ => await PerformHealthChecksAsync(), 
            null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
    }

    public async Task<ServiceEndpoint> SelectEndpointAsync(
        string serviceName, 
        LoadBalancingStrategy strategy = LoadBalancingStrategy.RoundRobin)
    {
        using var activity = Activity.Current?.Source.StartActivity("LoadBalancer.SelectEndpoint");
        activity?.SetTag("service.name", serviceName);
        activity?.SetTag("strategy", strategy.ToString());

        var endpoints = await GetHealthyEndpointsAsync(serviceName);
        var healthyEndpoints = endpoints.Where(e => e.Health == HealthStatus.Healthy).ToList();

        if (!healthyEndpoints.Any())
        {
logger.LogWarning("No healthy endpoints available for service {ServiceName}", serviceName);
            throw new InvalidOperationException($"No healthy endpoints available for service {serviceName}");
        }

        var selectedEndpoint = strategy switch
        {
            LoadBalancingStrategy.RoundRobin => SelectRoundRobin(serviceName, healthyEndpoints),
            LoadBalancingStrategy.LeastConnections => SelectLeastConnections(healthyEndpoints),
            LoadBalancingStrategy.WeightedRoundRobin => SelectWeightedRoundRobin(healthyEndpoints),
            LoadBalancingStrategy.IpHash => SelectIpHash(serviceName, healthyEndpoints),
            LoadBalancingStrategy.HealthBased => SelectHealthBased(healthyEndpoints),
            LoadBalancingStrategy.ResponseTime => SelectByResponseTime(healthyEndpoints),
            _ => SelectRoundRobin(serviceName, healthyEndpoints)
        };

        // Update connection count
        Interlocked.Increment(ref selectedEndpoint.ActiveConnections);
logger.LogDebug("Selected endpoint {EndpointId} for service {ServiceName} using {Strategy}",
            selectedEndpoint.Id, serviceName, strategy);

        await metrics.RecordLoadBalancingDecisionAsync(serviceName, selectedEndpoint.Id, strategy.ToString());

        return selectedEndpoint;
    }

    public async Task<IEnumerable<ServiceEndpoint>> GetHealthyEndpointsAsync(string serviceName)
    {
        if (!serviceStates.TryGetValue(serviceName, out var state))
        {
            // Initialize service state if not exists
            var endpoints = await serviceDiscovery.GetServiceEndpointsAsync(serviceName);
            state = new LoadBalancerState
            {
                ServiceName = serviceName,
                Endpoints = new ConcurrentDictionary<string, ServiceEndpoint>(
                    endpoints.ToDictionary(e => e.Id, e => e))
            };
serviceStates.TryAdd(serviceName, state);
        }

        return state.Endpoints.Values.Where(e => e.Health != HealthStatus.Unhealthy);
    }

    private ServiceEndpoint SelectRoundRobin(string serviceName, List<ServiceEndpoint> endpoints)
    {
        var state =serviceStates.GetOrAdd(serviceName, _ => new LoadBalancerState { ServiceName = serviceName });
        var index = (int)(Interlocked.Increment(ref state.RoundRobinIndex) % endpoints.Count);
        return endpoints[index];
    }

    private ServiceEndpoint SelectLeastConnections(List<ServiceEndpoint> endpoints)
    {
        return endpoints.OrderBy(e => e.ActiveConnections).ThenBy(e => e.ResponseTimeMs).First();
    }

    private ServiceEndpoint SelectWeightedRoundRobin(List<ServiceEndpoint> endpoints)
    {
        var totalWeight = endpoints.Sum(e => e.Weight);
        var random = new Random().NextDouble() * totalWeight;
        var currentWeight = 0.0;

        foreach (var endpoint in endpoints)
        {
            currentWeight += endpoint.Weight;
            if (random <= currentWeight)
                return endpoint;
        }

        return endpoints.Last();
    }

    private ServiceEndpoint SelectIpHash(string serviceName, List<ServiceEndpoint> endpoints)
    {
        // Use consistent hashing based on client IP or request context
        var hash = serviceName.GetHashCode();
        var index = Math.Abs(hash) % endpoints.Count;
        return endpoints[index];
    }

    private ServiceEndpoint SelectHealthBased(List<ServiceEndpoint> endpoints)
    {
        // Prioritize endpoints with best health metrics
        return endpoints
            .OrderByDescending(e => e.HealthScore)
            .ThenBy(e => e.ResponseTimeMs)
            .ThenBy(e => e.ActiveConnections)
            .First();
    }

    private ServiceEndpoint SelectByResponseTime(List<ServiceEndpoint> endpoints)
    {
        return endpoints.OrderBy(e => e.ResponseTimeMs).ThenBy(e => e.ActiveConnections).First();
    }

    private async Task PerformHealthChecksAsync()
    {
        var tasks =serviceStates.Values.SelectMany(state => 
            state.Endpoints.Values.Select(endpoint => CheckEndpointHealthAsync(endpoint)));
        
        await Task.WhenAll(tasks);
    }

    private async Task CheckEndpointHealthAsync(ServiceEndpoint endpoint)
    {
        try
        {
            var healthResult = await healthCheck.CheckHealthAsync(endpoint.Uri);
            var previousHealth = endpoint.Health;
            
            endpoint.Health = healthResult.Status;
            endpoint.HealthScore = healthResult.Score;
            endpoint.LastHealthCheck = DateTime.UtcNow;

            if (previousHealth != healthResult.Status)
            {
logger.LogInformation("Endpoint {EndpointId} health changed from {PreviousHealth} to {CurrentHealth}",
                    endpoint.Id, previousHealth, healthResult.Status);
            }
        }
        catch (Exception ex)
        {
logger.LogError(ex, "Health check failed for endpoint {EndpointId}", endpoint.Id);
            endpoint.Health = HealthStatus.Unhealthy;
            endpoint.HealthScore = 0.0;
        }
    }
}
```

#### Load Balancing Models

```csharp
public class ServiceEndpoint
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ServiceName { get; set; } = string.Empty;
    public Uri Uri { get; set; } = default!;
    public double Weight { get; set; } = 1.0;
    public int ActiveConnections { get; set; }
    public double ResponseTimeMs { get; set; }
    public HealthStatus Health { get; set; } = HealthStatus.Unknown;
    public double HealthScore { get; set; } = 1.0;
    public DateTime LastHealthCheck { get; set; }
    public Dictionary<string, double> Metrics { get; set; } = new();
    public Dictionary<string, string> Tags { get; set; } = new();
}

public class LoadBalancerState
{
    public string ServiceName { get; set; } = string.Empty;
    public ConcurrentDictionary<string, ServiceEndpoint> Endpoints { get; set; } = new();
    public long RoundRobinIndex { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

public enum LoadBalancingStrategy
{
    RoundRobin,
    LeastConnections,
    WeightedRoundRobin,
    IpHash,
    HealthBased,
    ResponseTime,
    Random
}

public enum HealthStatus
{
    Unknown,
    Healthy,
    Degraded,
    Unhealthy
}

public class HealthCheckResult
{
    public HealthStatus Status { get; set; }
    public double Score { get; set; } = 1.0;
    public TimeSpan ResponseTime { get; set; }
    public string? Details { get; set; }
    public Dictionary<string, double> Metrics { get; set; } = new();
}
```

### Circuit Breaker Integration

```csharp
public class CircuitBreakerLoadBalancer : ILoadBalancerService
{
    private readonly ILoadBalancerService innerLoadBalancer;
    private readonly ConcurrentDictionary<string, CircuitBreakerState> circuitStates = new();
    private readonly CircuitBreakerOptions options;
    private readonly ILogger<CircuitBreakerLoadBalancer> logger;

    public CircuitBreakerLoadBalancer(
        ILoadBalancerService innerLoadBalancer,
        CircuitBreakerOptions options,
        ILogger<CircuitBreakerLoadBalancer> logger)
    {
innerLoadBalancer = innerLoadBalancer;
options = options;
logger = logger;
    }

    public async Task<ServiceEndpoint> SelectEndpointAsync(string serviceName, LoadBalancingStrategy strategy = LoadBalancingStrategy.RoundRobin)
    {
        var circuitState =circuitStates.GetOrAdd(serviceName, _ => new CircuitBreakerState());
        
        if (circuitState.State == CircuitState.Open)
        {
            if (DateTime.UtcNow - circuitState.LastFailureTime < options.OpenTimeout)
            {
logger.LogWarning("Circuit breaker is OPEN for service {ServiceName}", serviceName);
                throw new CircuitBreakerOpenException($"Circuit breaker is open for service {serviceName}");
            }
            
            // Try to transition to half-open
            circuitState.State = CircuitState.HalfOpen;
logger.LogInformation("Circuit breaker transitioning to HALF-OPEN for service {ServiceName}", serviceName);
        }

        try
        {
            var endpoint = await innerLoadBalancer.SelectEndpointAsync(serviceName, strategy);
            
            if (circuitState.State == CircuitState.HalfOpen)
            {
                // Success in half-open state - close the circuit
                circuitState.State = CircuitState.Closed;
                circuitState.FailureCount = 0;
logger.LogInformation("Circuit breaker CLOSED for service {ServiceName}", serviceName);
            }
            
            return endpoint;
        }
        catch (Exception ex)
        {
            HandleFailure(serviceName, circuitState, ex);
            throw;
        }
    }

    private void HandleFailure(string serviceName, CircuitBreakerState circuitState, Exception exception)
    {
        circuitState.FailureCount++;
        circuitState.LastFailureTime = DateTime.UtcNow;

        if (circuitState.FailureCount >=options.FailureThreshold)
        {
            circuitState.State = CircuitState.Open;
logger.LogError(exception, "Circuit breaker OPENED for service {ServiceName} after {FailureCount} failures",
                serviceName, circuitState.FailureCount);
        }
    }

    // Implement other ILoadBalancerService methods by delegating to inner service
    public Task<IEnumerable<ServiceEndpoint>> GetHealthyEndpointsAsync(string serviceName) =>
innerLoadBalancer.GetHealthyEndpointsAsync(serviceName);

    public Task RegisterEndpointAsync(ServiceEndpoint endpoint) =>
innerLoadBalancer.RegisterEndpointAsync(endpoint);

    public Task UnregisterEndpointAsync(string endpointId) =>
innerLoadBalancer.UnregisterEndpointAsync(endpointId);

    public Task UpdateEndpointHealthAsync(string endpointId, HealthStatus health, Dictionary<string, double> metrics) =>
innerLoadBalancer.UpdateEndpointHealthAsync(endpointId, health, metrics);
}

public class CircuitBreakerState
{
    public CircuitState State { get; set; } = CircuitState.Closed;
    public int FailureCount { get; set; }
    public DateTime LastFailureTime { get; set; }
}

public enum CircuitState
{
    Closed,
    Open,
    HalfOpen
}

public class CircuitBreakerOptions
{
    public int FailureThreshold { get; set; } = 5;
    public TimeSpan OpenTimeout { get; set; } = TimeSpan.FromSeconds(30);
}

public class CircuitBreakerOpenException : Exception
{
    public CircuitBreakerOpenException(string message) : base(message) { }
}
```

### Service Mesh Integration

```csharp
public class ServiceMeshLoadBalancer : ILoadBalancerService
{
    private readonly IServiceMeshClient meshClient;
    private readonly ILogger<ServiceMeshLoadBalancer> logger;

    public ServiceMeshLoadBalancer(
        IServiceMeshClient meshClient,
        ILogger<ServiceMeshLoadBalancer> logger)
    {
meshClient = meshClient;
logger = logger;
    }

    public async Task<ServiceEndpoint> SelectEndpointAsync(string serviceName, LoadBalancingStrategy strategy = LoadBalancingStrategy.RoundRobin)
    {
        // Delegate to service mesh (Istio, Linkerd, Consul Connect)
        var meshEndpoint = await meshClient.GetServiceEndpointAsync(serviceName, new LoadBalancingPolicy
        {
            Strategy = strategy,
            EnableCircuitBreaker = true,
            EnableRetries = true,
            RetryPolicy = new RetryPolicy
            {
                MaxRetries = 3,
                BackoffPolicy = BackoffPolicy.Exponential
            }
        });

        return new ServiceEndpoint
        {
            Id = meshEndpoint.Id,
            ServiceName = serviceName,
            Uri = meshEndpoint.Uri,
            Health = MapHealthStatus(meshEndpoint.HealthStatus),
            Weight = meshEndpoint.Weight,
            ResponseTimeMs = meshEndpoint.Latency.TotalMilliseconds
        };
    }

    private HealthStatus MapHealthStatus(string meshHealthStatus) => meshHealthStatus.ToLowerInvariant() switch
    {
        "healthy" => HealthStatus.Healthy,
        "warning" => HealthStatus.Degraded,
        "critical" => HealthStatus.Unhealthy,
        _ => HealthStatus.Unknown
    };

    // Other methods delegate to service mesh
    public async Task<IEnumerable<ServiceEndpoint>> GetHealthyEndpointsAsync(string serviceName)
    {
        var meshEndpoints = await meshClient.GetAllServiceEndpointsAsync(serviceName);
        return meshEndpoints
            .Where(e => e.HealthStatus == "healthy")
            .Select(e => new ServiceEndpoint
            {
                Id = e.Id,
                ServiceName = serviceName,
                Uri = e.Uri,
                Health = HealthStatus.Healthy,
                Weight = e.Weight
            });
    }

    public Task RegisterEndpointAsync(ServiceEndpoint endpoint) =>
meshClient.RegisterServiceAsync(endpoint.ServiceName, endpoint.Uri.ToString(), endpoint.Weight);

    public Task UnregisterEndpointAsync(string endpointId) =>
meshClient.DeregisterServiceAsync(endpointId);

    public Task UpdateEndpointHealthAsync(string endpointId, HealthStatus health, Dictionary<string, double> metrics) =>
meshClient.UpdateHealthStatusAsync(endpointId, health.ToString().ToLowerInvariant(), metrics);
}
```

### Load Balancing Configuration

```csharp
public class LoadBalancingConfiguration
{
    public const string SectionName = "AspireLoadBalancing";
    
    public LoadBalancingStrategy DefaultStrategy { get; set; } = LoadBalancingStrategy.RoundRobin;
    public TimeSpan HealthCheckInterval { get; set; } = TimeSpan.FromSeconds(30);
    public int UnhealthyThreshold { get; set; } = 3;
    public int HealthyThreshold { get; set; } = 2;
    public CircuitBreakerOptions CircuitBreaker { get; set; } = new();
    public Dictionary<string, ServiceLoadBalancingConfig> Services { get; set; } = new();
    public ServiceMeshOptions? ServiceMesh { get; set; }
}

public class ServiceLoadBalancingConfig
{
    public LoadBalancingStrategy Strategy { get; set; } = LoadBalancingStrategy.RoundRobin;
    public bool EnableCircuitBreaker { get; set; } = true;
    public Dictionary<string, double> EndpointWeights { get; set; } = new();
    public HealthCheckConfig HealthCheck { get; set; } = new();
}

public class HealthCheckConfig
{
    public string Path { get; set; } = "/health";
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);
    public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(30);
    public Dictionary<string, string> Headers { get; set; } = new();
}

public class ServiceMeshOptions
{
    public string Provider { get; set; } = "istio"; // istio, linkerd, consul
    public string ConfigPath { get; set; } = string.Empty;
    public Dictionary<string, string> Settings { get; set; } = new();
}
```

### Dependency Injection Setup

```csharp
public static class LoadBalancingServiceExtensions
{
    public static IServiceCollection AddAspireLoadBalancing(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var config = configuration.GetSection(LoadBalancingConfiguration.SectionName)
            .Get<LoadBalancingConfiguration>() ?? new LoadBalancingConfiguration();

        services.Configure<LoadBalancingConfiguration>(
            configuration.GetSection(LoadBalancingConfiguration.SectionName));

        // Register core services
        services.AddSingleton<IServiceDiscovery, AspireServiceDiscovery>();
        services.AddSingleton<IHealthCheckService, HttpHealthCheckService>();
        services.AddSingleton<IMetricsCollector, PrometheusMetricsCollector>();

        // Register load balancer based on configuration
        if (config.ServiceMesh != null)
        {
            services.AddSingleton<ILoadBalancerService, ServiceMeshLoadBalancer>();
            services.AddSingleton<IServiceMeshClient>(provider => 
                CreateServiceMeshClient(config.ServiceMesh, provider));
        }
        else
        {
            services.AddSingleton<AspireLoadBalancerService>();
            
            if (config.CircuitBreaker != null)
            {
                services.AddSingleton<ILoadBalancerService>(provider =>
                {
                    var innerService = provider.GetRequiredService<AspireLoadBalancerService>();
                    var logger = provider.GetRequiredService<ILogger<CircuitBreakerLoadBalancer>>();
                    return new CircuitBreakerLoadBalancer(innerService, config.CircuitBreaker, logger);
                });
            }
            else
            {
                services.AddSingleton<ILoadBalancerService>(provider => 
                    provider.GetRequiredService<AspireLoadBalancerService>());
            }
        }

        return services;
    }

    private static IServiceMeshClient CreateServiceMeshClient(ServiceMeshOptions options, IServiceProvider provider) =>
        options.Provider.ToLowerInvariant() switch
        {
            "istio" => new IstioServiceMeshClient(options, provider.GetRequiredService<ILogger<IstioServiceMeshClient>>()),
            "linkerd" => new LinkerdServiceMeshClient(options, provider.GetRequiredService<ILogger<LinkerdServiceMeshClient>>()),
            "consul" => new ConsulServiceMeshClient(options, provider.GetRequiredService<ILogger<ConsulServiceMeshClient>>()),
            _ => throw new NotSupportedException($"Service mesh provider '{options.Provider}' is not supported")
        };
}
```

### Usage Examples

```csharp
// Basic load balancing
public class DocumentProcessingController : ControllerBase
{
    private readonly ILoadBalancerService loadBalancer;
    private readonly HttpClient httpClient;

    public DocumentProcessingController(
        ILoadBalancerService loadBalancer,
        HttpClient httpClient)
    {
loadBalancer = loadBalancer;
httpClient = httpClient;
    }

    [HttpPost("process")]
    public async Task<IActionResult> ProcessDocument([FromBody] DocumentRequest request)
    {
        // Select best endpoint for document processing service
        var endpoint = await loadBalancer.SelectEndpointAsync(
            "document-processing", 
            LoadBalancingStrategy.LeastConnections);

        try
        {
            var response = await httpClient.PostAsJsonAsync(
                new Uri(endpoint.Uri, "api/process"), 
                request);

            return Ok(await response.Content.ReadFromJsonAsync<DocumentResponse>());
        }
        finally
        {
            // Decrement connection count
            Interlocked.Decrement(ref endpoint.ActiveConnections);
        }
    }
}

// Configuration example
{
  "AspireLoadBalancing": {
    "DefaultStrategy": "HealthBased",
    "HealthCheckInterval": "00:00:15",
    "CircuitBreaker": {
      "FailureThreshold": 3,
      "OpenTimeout": "00:00:30"
    },
    "Services": {
      "document-processing": {
        "Strategy": "LeastConnections",
        "EnableCircuitBreaker": true,
        "EndpointWeights": {
          "processing-1": 1.0,
          "processing-2": 2.0,
          "processing-gpu": 3.0
        }
      },
      "ml-inference": {
        "Strategy": "ResponseTime",
        "HealthCheck": {
          "Path": "/health/ready",
          "Timeout": "00:00:10"
        }
      }
    }
  }
}
```

## Resource-Based Scaling Policies

### Resource Metrics Collection

Resource-based scaling relies on real-time metrics collection from various sources to make informed scaling decisions.

#### Metrics Collection Service

```csharp
public interface IResourceMetricsCollector
{
    Task<ResourceMetrics> GetResourceMetricsAsync(string serviceName, CancellationToken cancellationToken = default);
    Task<IEnumerable<ResourceMetrics>> GetAllServiceMetricsAsync(CancellationToken cancellationToken = default);
    Task StartCollectionAsync(TimeSpan interval, CancellationToken cancellationToken = default);
    Task StopCollectionAsync();
    event EventHandler<MetricsCollectedEventArgs> MetricsCollected;
}

public class AspireResourceMetricsCollector : IResourceMetricsCollector, IDisposable
{
    private readonly IServiceDiscovery serviceDiscovery;
    private readonly ILogger<AspireResourceMetricsCollector> logger;
    private readonly HttpClient httpClient;
    private readonly Timer? collectionTimer;
    private readonly ConcurrentDictionary<string, ResourceMetrics> latestMetrics = new();
    
    public event EventHandler<MetricsCollectedEventArgs>? MetricsCollected;

    public AspireResourceMetricsCollector(
        IServiceDiscovery serviceDiscovery,
        ILogger<AspireResourceMetricsCollector> logger,
        HttpClient httpClient)
    {
serviceDiscovery = serviceDiscovery;
logger = logger;
httpClient = httpClient;
    }

    public async Task<ResourceMetrics> GetResourceMetricsAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        using var activity = Activity.Current?.Source.StartActivity("ResourceMetrics.Collect");
        activity?.SetTag("service.name", serviceName);

        try
        {
            var endpoints = await serviceDiscovery.GetServiceEndpointsAsync(serviceName);
            var metricsData = new List<ServiceInstanceMetrics>();

            foreach (var endpoint in endpoints)
            {
                try
                {
                    var instanceMetrics = await CollectInstanceMetricsAsync(endpoint, cancellationToken);
                    metricsData.Add(instanceMetrics);
                }
                catch (Exception ex)
                {
logger.LogWarning(ex, "Failed to collect metrics from endpoint {EndpointId}", endpoint.Id);
                }
            }

            var aggregatedMetrics = AggregateMetrics(serviceName, metricsData);
latestMetrics.AddOrUpdate(serviceName, aggregatedMetrics, (_, _) => aggregatedMetrics);

            return aggregatedMetrics;
        }
        catch (Exception ex)
        {
logger.LogError(ex, "Failed to collect resource metrics for service {ServiceName}", serviceName);
            return latestMetrics.GetValueOrDefault(serviceName) ?? CreateEmptyMetrics(serviceName);
        }
    }

    private async Task<ServiceInstanceMetrics> CollectInstanceMetricsAsync(
        ServiceEndpoint endpoint, 
        CancellationToken cancellationToken)
    {
        var metricsUri = new Uri(endpoint.Uri, "/metrics");
        
        try
        {
            // Collect Prometheus-style metrics
            var response = await httpClient.GetStringAsync(metricsUri, cancellationToken);
            var metrics = ParsePrometheusMetrics(response);

            // Also collect system metrics via custom endpoint
            var systemMetricsUri = new Uri(endpoint.Uri, "/api/system/metrics");
            var systemResponse = await httpClient.GetFromJsonAsync<SystemMetrics>(systemMetricsUri, cancellationToken);

            return new ServiceInstanceMetrics
            {
                EndpointId = endpoint.Id,
                ServiceName = endpoint.ServiceName,
                Timestamp = DateTime.UtcNow,
                CpuUsagePercent = systemResponse?.CpuUsage ?? metrics.GetValueOrDefault("cpu_usage_percent", 0),
                MemoryUsageMB = systemResponse?.MemoryUsageMB ?? metrics.GetValueOrDefault("memory_usage_mb", 0),
                MemoryUsagePercent = systemResponse?.MemoryUsagePercent ?? metrics.GetValueOrDefault("memory_usage_percent", 0),
                RequestsPerSecond = metrics.GetValueOrDefault("requests_per_second", 0),
                ResponseTimeMs = metrics.GetValueOrDefault("response_time_ms", 0),
                ErrorRate = metrics.GetValueOrDefault("error_rate", 0),
                ActiveConnections = (int)metrics.GetValueOrDefault("active_connections", 0),
                QueueLength = (int)metrics.GetValueOrDefault("queue_length", 0),
                DiskUsagePercent = systemResponse?.DiskUsagePercent ?? metrics.GetValueOrDefault("disk_usage_percent", 0),
                NetworkBytesPerSecond = metrics.GetValueOrDefault("network_bytes_per_second", 0),
                CustomMetrics = metrics.Where(kvp => !IsStandardMetric(kvp.Key))
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            };
        }
        catch (Exception ex)
        {
logger.LogWarning(ex, "Failed to collect metrics from endpoint {EndpointUri}", metricsUri);
            return CreateEmptyInstanceMetrics(endpoint.Id, endpoint.ServiceName);
        }
    }

    private ResourceMetrics AggregateMetrics(string serviceName, List<ServiceInstanceMetrics> instanceMetrics)
    {
        if (!instanceMetrics.Any())
            return CreateEmptyMetrics(serviceName);

        return new ResourceMetrics
        {
            ServiceName = serviceName,
            Timestamp = DateTime.UtcNow,
            InstanceCount = instanceMetrics.Count,
            
            // Average metrics across instances
            AverageCpuUsage = instanceMetrics.Average(m => m.CpuUsagePercent),
            AverageMemoryUsage = instanceMetrics.Average(m => m.MemoryUsagePercent),
            AverageResponseTime = instanceMetrics.Average(m => m.ResponseTimeMs),
            
            // Sum metrics that should be aggregated
            TotalRequestsPerSecond = instanceMetrics.Sum(m => m.RequestsPerSecond),
            TotalActiveConnections = instanceMetrics.Sum(m => m.ActiveConnections),
            TotalQueueLength = instanceMetrics.Sum(m => m.QueueLength),
            
            // Max values for resource usage
            MaxCpuUsage = instanceMetrics.Max(m => m.CpuUsagePercent),
            MaxMemoryUsage = instanceMetrics.Max(m => m.MemoryUsagePercent),
            MaxResponseTime = instanceMetrics.Max(m => m.ResponseTimeMs),
            
            // Error rate (average)
            ErrorRate = instanceMetrics.Average(m => m.ErrorRate),
            
            // Instance details
            InstanceMetrics = instanceMetrics
        };
    }

    public async Task StartCollectionAsync(TimeSpan interval, CancellationToken cancellationToken = default)
    {
logger.LogInformation("Starting resource metrics collection with interval {Interval}", interval);
        
        var timer = new Timer(async _ =>
        {
            try
            {
                var services = await serviceDiscovery.GetServicesAsync();
                var tasks = services.Select(async serviceName =>
                {
                    try
                    {
                        var metrics = await GetResourceMetricsAsync(serviceName, CancellationToken.None);
                        MetricsCollected?.Invoke(this, new MetricsCollectedEventArgs(metrics));
                    }
                    catch (Exception ex)
                    {
logger.LogError(ex, "Error collecting metrics for service {ServiceName}", serviceName);
                    }
                });
                
                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
logger.LogError(ex, "Error during metrics collection cycle");
            }
        }, null, TimeSpan.Zero, interval);
    }

    private Dictionary<string, double> ParsePrometheusMetrics(string prometheusData)
    {
        var metrics = new Dictionary<string, double>();
        
        foreach (var line in prometheusData.Split('\n'))
        {
            if (line.StartsWith('#') || string.IsNullOrWhiteSpace(line))
                continue;

            var parts = line.Split(' ');
            if (parts.Length >= 2 && double.TryParse(parts[1], out var value))
            {
                metrics[parts[0]] = value;
            }
        }

        return metrics;
    }

    private bool IsStandardMetric(string metricName) => metricName switch
    {
        "cpu_usage_percent" or "memory_usage_percent" or "memory_usage_mb" or
        "requests_per_second" or "response_time_ms" or "error_rate" or
        "active_connections" or "queue_length" or "disk_usage_percent" or
        "network_bytes_per_second" => true,
        _ => false
    };

    public void Dispose()
    {
        collectionTimer?.Dispose();
        httpClient?.Dispose();
    }
}
```

#### Resource Scaling Policies

```csharp
public interface IResourceScalingPolicy
{
    Task<ScalingDecision> EvaluateAsync(ResourceScalingContext context);
    string PolicyName { get; }
    int Priority { get; }
}

public class CpuBasedScalingPolicy : IResourceScalingPolicy
{
    private readonly CpuScalingOptions options;
    private readonly ILogger<CpuBasedScalingPolicy> logger;

    public string PolicyName => "CPU-Based Scaling";
    public int Priority => 100;

    public CpuBasedScalingPolicy(CpuScalingOptions options, ILogger<CpuBasedScalingPolicy> logger)
    {
options = options;
logger = logger;
    }

    public async Task<ScalingDecision> EvaluateAsync(ResourceScalingContext context)
    {
        var metrics = context.ResourceMetrics;
        var currentInstances = context.CurrentInstances;
        
        await Task.CompletedTask; // Placeholder for async operations

        // Check if we're in cooldown period
        if (DateTime.UtcNow - context.LastScalingAction < options.CooldownPeriod)
        {
            return ScalingDecision.NoAction("CPU scaling in cooldown period");
        }

        var avgCpu = metrics.AverageCpuUsage;
        var maxCpu = metrics.MaxCpuUsage;
        
        // Scale up conditions
        if (avgCpu > options.ScaleUpThreshold || maxCpu > options.ScaleUpMaxThreshold)
        {
            var targetInstances = CalculateTargetInstances(
                currentInstances, avgCpu, options.ScaleUpThreshold, options.ScaleUpFactor);
                
            targetInstances = Math.Min(targetInstances, context.MaxInstances);
            
            if (targetInstances > currentInstances)
            {
logger.LogInformation("CPU-based scale up: avg={AvgCpu}%, max={MaxCpu}%, target={Target} instances",
                    avgCpu, maxCpu, targetInstances);
                    
                return ScalingDecision.ScaleUp(targetInstances, 
                    $"CPU usage avg={avgCpu:F1}%, max={maxCpu:F1}% exceeds thresholds");
            }
        }
        
        // Scale down conditions
        if (avgCpu < options.ScaleDownThreshold && maxCpu < options.ScaleDownMaxThreshold)
        {
            var targetInstances = CalculateTargetInstances(
                currentInstances, avgCpu, options.ScaleDownThreshold, options.ScaleDownFactor);
                
            targetInstances = Math.Max(targetInstances, context.MinInstances);
            
            if (targetInstances < currentInstances)
            {
logger.LogInformation("CPU-based scale down: avg={AvgCpu}%, max={MaxCpu}%, target={Target} instances",
                    avgCpu, maxCpu, targetInstances);
                    
                return ScalingDecision.ScaleDown(targetInstances,
                    $"CPU usage avg={avgCpu:F1}%, max={maxCpu:F1}% below thresholds");
            }
        }

        return ScalingDecision.NoAction($"CPU usage avg={avgCpu:F1}%, max={maxCpu:F1}% within acceptable range");
    }

    private int CalculateTargetInstances(int currentInstances, double currentUsage, double targetUsage, double scaleFactor)
    {
        // Calculate target based on utilization ratio
        var utilizationRatio = currentUsage / targetUsage;
        var targetInstancesFloat = currentInstances * utilizationRatio * scaleFactor;
        
        // Round to nearest integer, but ensure at least 1 instance change
        var targetInstances = (int)Math.Round(targetInstancesFloat);
        
        if (targetInstances == currentInstances)
        {
            targetInstances = currentUsage > targetUsage ? currentInstances + 1 : currentInstances - 1;
        }
        
        return Math.Max(1, targetInstances);
    }
}

public class MemoryBasedScalingPolicy : IResourceScalingPolicy
{
    private readonly MemoryScalingOptions options;
    private readonly ILogger<MemoryBasedScalingPolicy> logger;

    public string PolicyName => "Memory-Based Scaling";
    public int Priority => 90;

    public MemoryBasedScalingPolicy(MemoryScalingOptions options, ILogger<MemoryBasedScalingPolicy> logger)
    {
options = options;
logger = logger;
    }

    public async Task<ScalingDecision> EvaluateAsync(ResourceScalingContext context)
    {
        var metrics = context.ResourceMetrics;
        var currentInstances = context.CurrentInstances;

        await Task.CompletedTask;

        if (DateTime.UtcNow - context.LastScalingAction < options.CooldownPeriod)
        {
            return ScalingDecision.NoAction("Memory scaling in cooldown period");
        }

        var avgMemory = metrics.AverageMemoryUsage;
        var maxMemory = metrics.MaxMemoryUsage;

        // Scale up conditions (memory pressure)
        if (avgMemory > options.ScaleUpThreshold || maxMemory > options.ScaleUpMaxThreshold)
        {
            var targetInstances = Math.Min(
                currentInstances + options.ScaleUpStep,
                context.MaxInstances);

            if (targetInstances > currentInstances)
            {
logger.LogInformation("Memory-based scale up: avg={AvgMemory}%, max={MaxMemory}%, target={Target} instances",
                    avgMemory, maxMemory, targetInstances);

                return ScalingDecision.ScaleUp(targetInstances,
                    $"Memory usage avg={avgMemory:F1}%, max={maxMemory:F1}% exceeds thresholds");
            }
        }

        // Scale down conditions
        if (avgMemory < options.ScaleDownThreshold && maxMemory < options.ScaleDownMaxThreshold)
        {
            var targetInstances = Math.Max(
                currentInstances - options.ScaleDownStep,
                context.MinInstances);

            if (targetInstances < currentInstances)
            {
logger.LogInformation("Memory-based scale down: avg={AvgMemory}%, max={MaxMemory}%, target={Target} instances",
                    avgMemory, maxMemory, targetInstances);

                return ScalingDecision.ScaleDown(targetInstances,
                    $"Memory usage avg={avgMemory:F1}%, max={maxMemory:F1}% below thresholds");
            }
        }

        return ScalingDecision.NoAction($"Memory usage avg={avgMemory:F1}%, max={maxMemory:F1}% within acceptable range");
    }
}

public class RequestRateScalingPolicy : IResourceScalingPolicy
{
    private readonly RequestRateScalingOptions options;
    private readonly ILogger<RequestRateScalingPolicy> logger;

    public string PolicyName => "Request Rate Scaling";
    public int Priority => 80;

    public RequestRateScalingPolicy(RequestRateScalingOptions options, ILogger<RequestRateScalingPolicy> logger)
    {
options = options;
logger = logger;
    }

    public async Task<ScalingDecision> EvaluateAsync(ResourceScalingContext context)
    {
        var metrics = context.ResourceMetrics;
        var currentInstances = context.CurrentInstances;

        await Task.CompletedTask;

        if (DateTime.UtcNow - context.LastScalingAction < options.CooldownPeriod)
        {
            return ScalingDecision.NoAction("Request rate scaling in cooldown period");
        }

        var totalRps = metrics.TotalRequestsPerSecond;
        var avgRpsPerInstance = currentInstances > 0 ? totalRps / currentInstances : 0;

        // Scale up conditions
        if (avgRpsPerInstance > options.MaxRequestsPerInstance)
        {
            var targetInstances = (int)Math.Ceiling(totalRps / options.TargetRequestsPerInstance);
            targetInstances = Math.Min(targetInstances, context.MaxInstances);

            if (targetInstances > currentInstances)
            {
logger.LogInformation("Request rate scale up: {TotalRps} RPS, {AvgRps} per instance, target={Target} instances",
                    totalRps, avgRpsPerInstance, targetInstances);

                return ScalingDecision.ScaleUp(targetInstances,
                    $"Request rate {totalRps:F1} RPS ({avgRpsPerInstance:F1} per instance) exceeds capacity");
            }
        }

        // Scale down conditions
        if (avgRpsPerInstance < options.MinRequestsPerInstance && currentInstances > context.MinInstances)
        {
            var targetInstances = Math.Max(
                (int)Math.Ceiling(totalRps / options.TargetRequestsPerInstance),
                context.MinInstances);

            if (targetInstances < currentInstances)
            {
logger.LogInformation("Request rate scale down: {TotalRps} RPS, {AvgRps} per instance, target={Target} instances",
                    totalRps, avgRpsPerInstance, targetInstances);

                return ScalingDecision.ScaleDown(targetInstances,
                    $"Request rate {totalRps:F1} RPS ({avgRpsPerInstance:F1} per instance) below minimum threshold");
            }
        }

        return ScalingDecision.NoAction($"Request rate {totalRps:F1} RPS ({avgRpsPerInstance:F1} per instance) within acceptable range");
    }
}

public class CompositeScalingPolicy : IResourceScalingPolicy
{
    private readonly List<IResourceScalingPolicy> policies;
    private readonly CompositeScalingOptions options;
    private readonly ILogger<CompositeScalingPolicy> logger;

    public string PolicyName => "Composite Scaling";
    public int Priority => 200;

    public CompositeScalingPolicy(
        IEnumerable<IResourceScalingPolicy> policies,
        CompositeScalingOptions options,
        ILogger<CompositeScalingPolicy> logger)
    {
policies = policies.Where(p => p != this).OrderByDescending(p => p.Priority).ToList();
options = options;
logger = logger;
    }

    public async Task<ScalingDecision> EvaluateAsync(ResourceScalingContext context)
    {
        var decisions = new List<PolicyDecision>();

        foreach (var policy in policies)
        {
            try
            {
                var decision = await policy.EvaluateAsync(context);
                decisions.Add(new PolicyDecision
                {
                    PolicyName = policy.PolicyName,
                    Priority = policy.Priority,
                    Decision = decision
                });
            }
            catch (Exception ex)
            {
logger.LogError(ex, "Error evaluating scaling policy {PolicyName}", policy.PolicyName);
            }
        }

        return options.DecisionStrategy switch
        {
            CompositeDecisionStrategy.HighestPriority => decisions
                .Where(d => d.Decision.ShouldScale)
                .OrderByDescending(d => d.Priority)
                .FirstOrDefault()?.Decision ?? ScalingDecision.NoAction("No policies recommend scaling"),

            CompositeDecisionStrategy.MostAggressive => decisions
                .Where(d => d.Decision.ShouldScale)
                .OrderByDescending(d => Math.Abs(d.Decision.TargetInstances - context.CurrentInstances))
                .FirstOrDefault()?.Decision ?? ScalingDecision.NoAction("No policies recommend scaling"),

            CompositeDecisionStrategy.Consensus => EvaluateConsensus(decisions, context),

            CompositeDecisionStrategy.WeightedAverage => EvaluateWeightedAverage(decisions, context),

            _ => ScalingDecision.NoAction("Unknown composite decision strategy")
        };
    }

    private ScalingDecision EvaluateConsensus(List<PolicyDecision> decisions, ResourceScalingContext context)
    {
        var scalingDecisions = decisions.Where(d => d.Decision.ShouldScale).ToList();
        
        if (scalingDecisions.Count < options.MinimumConsensus)
        {
            return ScalingDecision.NoAction($"Consensus not reached ({scalingDecisions.Count}/{options.MinimumConsensus} required)");
        }

        var scaleUpCount = scalingDecisions.Count(d => d.Decision.TargetInstances > context.CurrentInstances);
        var scaleDownCount = scalingDecisions.Count(d => d.Decision.TargetInstances < context.CurrentInstances);

        if (scaleUpCount > scaleDownCount)
        {
            var avgTarget = (int)scalingDecisions
                .Where(d => d.Decision.TargetInstances > context.CurrentInstances)
                .Average(d => d.Decision.TargetInstances);
                
            return ScalingDecision.ScaleUp(avgTarget, $"Consensus to scale up ({scaleUpCount} policies)");
        }
        else if (scaleDownCount > scaleUpCount)
        {
            var avgTarget = (int)scalingDecisions
                .Where(d => d.Decision.TargetInstances < context.CurrentInstances)
                .Average(d => d.Decision.TargetInstances);
                
            return ScalingDecision.ScaleDown(avgTarget, $"Consensus to scale down ({scaleDownCount} policies)");
        }

        return ScalingDecision.NoAction("No clear consensus on scaling direction");
    }

    private ScalingDecision EvaluateWeightedAverage(List<PolicyDecision> decisions, ResourceScalingContext context)
    {
        var scalingDecisions = decisions.Where(d => d.Decision.ShouldScale).ToList();
        
        if (!scalingDecisions.Any())
        {
            return ScalingDecision.NoAction("No policies recommend scaling");
        }

        var weightedSum = scalingDecisions.Sum(d => d.Decision.TargetInstances * d.Priority);
        var totalWeight = scalingDecisions.Sum(d => d.Priority);
        var weightedAverage = (int)Math.Round((double)weightedSum / totalWeight);

        if (weightedAverage > context.CurrentInstances)
        {
            return ScalingDecision.ScaleUp(weightedAverage, $"Weighted average recommends {weightedAverage} instances");
        }
        else if (weightedAverage < context.CurrentInstances)
        {
            return ScalingDecision.ScaleDown(weightedAverage, $"Weighted average recommends {weightedAverage} instances");
        }

        return ScalingDecision.NoAction("Weighted average matches current instances");
    }
}
```

#### Resource Scaling Models

```csharp
public class ResourceMetrics
{
    public string ServiceName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public int InstanceCount { get; set; }
    
    // CPU Metrics
    public double AverageCpuUsage { get; set; }
    public double MaxCpuUsage { get; set; }
    
    // Memory Metrics
    public double AverageMemoryUsage { get; set; }
    public double MaxMemoryUsage { get; set; }
    
    // Performance Metrics
    public double AverageResponseTime { get; set; }
    public double MaxResponseTime { get; set; }
    public double TotalRequestsPerSecond { get; set; }
    public int TotalActiveConnections { get; set; }
    public int TotalQueueLength { get; set; }
    public double ErrorRate { get; set; }
    
    // Instance-level details
    public List<ServiceInstanceMetrics> InstanceMetrics { get; set; } = new();
}

public class ServiceInstanceMetrics
{
    public string EndpointId { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    
    public double CpuUsagePercent { get; set; }
    public double MemoryUsageMB { get; set; }
    public double MemoryUsagePercent { get; set; }
    public double RequestsPerSecond { get; set; }
    public double ResponseTimeMs { get; set; }
    public double ErrorRate { get; set; }
    public int ActiveConnections { get; set; }
    public int QueueLength { get; set; }
    public double DiskUsagePercent { get; set; }
    public double NetworkBytesPerSecond { get; set; }
    
    public Dictionary<string, double> CustomMetrics { get; set; } = new();
}

public class ResourceScalingContext
{
    public string ServiceName { get; set; } = string.Empty;
    public int CurrentInstances { get; set; }
    public int MinInstances { get; set; } = 1;
    public int MaxInstances { get; set; } = 10;
    public ResourceMetrics ResourceMetrics { get; set; } = default!;
    public DateTime LastScalingAction { get; set; }
    public TimeSpan CooldownPeriod { get; set; } = TimeSpan.FromMinutes(5);
}

public class ScalingDecision
{
    public bool ShouldScale { get; private set; }
    public int TargetInstances { get; private set; }
    public ScalingDirection Direction { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public DateTime Timestamp { get; private set; } = DateTime.UtcNow;

    private ScalingDecision() { }

    public static ScalingDecision ScaleUp(int targetInstances, string reason) =>
        new()
        {
            ShouldScale = true,
            TargetInstances = targetInstances,
            Direction = ScalingDirection.Up,
            Reason = reason
        };

    public static ScalingDecision ScaleDown(int targetInstances, string reason) =>
        new()
        {
            ShouldScale = true,
            TargetInstances = targetInstances,
            Direction = ScalingDirection.Down,
            Reason = reason
        };

    public static ScalingDecision NoAction(string reason) =>
        new()
        {
            ShouldScale = false,
            Reason = reason
        };
}

public enum ScalingDirection
{
    None,
    Up,
    Down
}

public class PolicyDecision
{
    public string PolicyName { get; set; } = string.Empty;
    public int Priority { get; set; }
    public ScalingDecision Decision { get; set; } = default!;
}

// Scaling options classes
public class CpuScalingOptions
{
    public double ScaleUpThreshold { get; set; } = 70.0;
    public double ScaleUpMaxThreshold { get; set; } = 85.0;
    public double ScaleDownThreshold { get; set; } = 30.0;
    public double ScaleDownMaxThreshold { get; set; } = 50.0;
    public double ScaleUpFactor { get; set; } = 1.2;
    public double ScaleDownFactor { get; set; } = 0.8;
    public TimeSpan CooldownPeriod { get; set; } = TimeSpan.FromMinutes(5);
}

public class MemoryScalingOptions
{
    public double ScaleUpThreshold { get; set; } = 80.0;
    public double ScaleUpMaxThreshold { get; set; } = 90.0;
    public double ScaleDownThreshold { get; set; } = 40.0;
    public double ScaleDownMaxThreshold { get; set; } = 60.0;
    public int ScaleUpStep { get; set; } = 2;
    public int ScaleDownStep { get; set; } = 1;
    public TimeSpan CooldownPeriod { get; set; } = TimeSpan.FromMinutes(3);
}

public class RequestRateScalingOptions
{
    public double TargetRequestsPerInstance { get; set; } = 100.0;
    public double MaxRequestsPerInstance { get; set; } = 150.0;
    public double MinRequestsPerInstance { get; set; } = 20.0;
    public TimeSpan CooldownPeriod { get; set; } = TimeSpan.FromMinutes(2);
}

public class CompositeScalingOptions
{
    public CompositeDecisionStrategy DecisionStrategy { get; set; } = CompositeDecisionStrategy.HighestPriority;
    public int MinimumConsensus { get; set; } = 2;
}

public enum CompositeDecisionStrategy
{
    HighestPriority,
    MostAggressive,
    Consensus,
    WeightedAverage
}
```

### Resource Scaling Configuration

```csharp
public class ResourceScalingConfiguration
{
    public const string SectionName = "AspireResourceScaling";
    
    public TimeSpan MetricsCollectionInterval { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan EvaluationInterval { get; set; } = TimeSpan.FromMinutes(1);
    public Dictionary<string, ServiceResourceScalingConfig> Services { get; set; } = new();
    public CpuScalingOptions DefaultCpuOptions { get; set; } = new();
    public MemoryScalingOptions DefaultMemoryOptions { get; set; } = new();
    public RequestRateScalingOptions DefaultRequestRateOptions { get; set; } = new();
    public CompositeScalingOptions CompositeOptions { get; set; } = new();
}

public class ServiceResourceScalingConfig
{
    public bool EnableResourceScaling { get; set; } = true;
    public List<string> EnabledPolicies { get; set; } = new() { "cpu", "memory", "request-rate" };
    public int MinInstances { get; set; } = 1;
    public int MaxInstances { get; set; } = 10;
    public CpuScalingOptions? CpuOptions { get; set; }
    public MemoryScalingOptions? MemoryOptions { get; set; }
    public RequestRateScalingOptions? RequestRateOptions { get; set; }
    public Dictionary<string, object> CustomPolicyOptions { get; set; } = new();
}
```

### Resource Scaling Usage Examples

```csharp
// Configuration example
{
  "AspireResourceScaling": {
    "MetricsCollectionInterval": "00:00:30",
    "EvaluationInterval": "00:01:00",
    "Services": {
      "document-processing": {
        "EnableResourceScaling": true,
        "EnabledPolicies": ["cpu", "memory", "request-rate"],
        "MinInstances": 2,
        "MaxInstances": 20,
        "CpuOptions": {
          "ScaleUpThreshold": 65.0,
          "ScaleDownThreshold": 25.0,
          "CooldownPeriod": "00:03:00"
        },
        "MemoryOptions": {
          "ScaleUpThreshold": 75.0,
          "ScaleDownThreshold": 35.0,
          "ScaleUpStep": 3
        },
        "RequestRateOptions": {
          "TargetRequestsPerInstance": 80.0,
          "MaxRequestsPerInstance": 120.0
        }
      },
      "ml-inference": {
        "EnableResourceScaling": true,
        "EnabledPolicies": ["cpu", "memory"],
        "MinInstances": 1,
        "MaxInstances": 8,
        "CpuOptions": {
          "ScaleUpThreshold": 80.0,
          "ScaleDownThreshold": 30.0,
          "CooldownPeriod": "00:05:00"
        }
      }
    }
  }
}

// Service registration
public static IServiceCollection AddResourceScaling(
    this IServiceCollection services, 
    IConfiguration configuration)
{
    services.Configure<ResourceScalingConfiguration>(
        configuration.GetSection(ResourceScalingConfiguration.SectionName));
        
    services.AddSingleton<IResourceMetricsCollector, AspireResourceMetricsCollector>();
    services.AddSingleton<IResourceScalingPolicy, CpuBasedScalingPolicy>();
    services.AddSingleton<IResourceScalingPolicy, MemoryBasedScalingPolicy>();
    services.AddSingleton<IResourceScalingPolicy, RequestRateScalingPolicy>();
    services.AddSingleton<IResourceScalingPolicy, CompositeScalingPolicy>();
    
    return services;
}
```

## Orleans Cluster Scaling Patterns

### Grain-Aware Scaling

Orleans clusters require specialized scaling strategies that consider grain distribution, silo health, and cluster membership dynamics.

#### Orleans Cluster Scaling Service

```csharp
public interface IOrleansClusterScalingService
{
    Task<OrleansScalingResult> ScaleClusterAsync(string clusterName, int targetSilos, CancellationToken cancellationToken = default);
    Task<ClusterMetrics> GetClusterMetricsAsync(string clusterName, CancellationToken cancellationToken = default);
    Task<IEnumerable<SiloInfo>> GetActiveSilosAsync(string clusterName, CancellationToken cancellationToken = default);
    Task RegisterSiloAsync(SiloInfo silo);
    Task DecommissionSiloAsync(string siloId, TimeSpan gracePeriod);
    event EventHandler<SiloStatusChangedEventArgs> SiloStatusChanged;
}

public class AspireOrleansClusterScalingService : IOrleansClusterScalingService, IDisposable
{
    private readonly IClusterClient clusterClient;
    private readonly IOrleansHostingService hostingService;
    private readonly ILogger<AspireOrleansClusterScalingService> logger;
    private readonly IMetricsCollector metrics;
    private readonly OrleansScalingConfiguration config;
    private readonly ConcurrentDictionary<string, SiloInfo> activeSilos = new();
    private readonly SemaphoreSlim scalingLock = new(1, 1);
    private Timer? monitoringTimer;

    public event EventHandler<SiloStatusChangedEventArgs>? SiloStatusChanged;

    public AspireOrleansClusterScalingService(
        IClusterClient clusterClient,
        IOrleansHostingService hostingService,
        IOptions<OrleansScalingConfiguration> config,
        ILogger<AspireOrleansClusterScalingService> logger,
        IMetricsCollector metrics)
    {
clusterClient = clusterClient;
hostingService = hostingService;
config = config.Value;
logger = logger;
metrics = metrics;
        
        InitializeMonitoring();
    }

    public async Task<OrleansScalingResult> ScaleClusterAsync(
        string clusterName, 
        int targetSilos, 
        CancellationToken cancellationToken = default)
    {
        using var activity = Activity.Current?.Source.StartActivity("Orleans.ScaleCluster");
        activity?.SetTag("cluster.name", clusterName);
        activity?.SetTag("target.silos", targetSilos);

        var startTime = DateTime.UtcNow;

        try
        {
            await scalingLock.WaitAsync(cancellationToken);
            
            var currentSilos = await GetActiveSilosAsync(clusterName, cancellationToken);
            var currentCount = currentSilos.Count();

            if (currentCount == targetSilos)
            {
logger.LogInformation("Orleans cluster {ClusterName} already at target size: {SiloCount}", 
                    clusterName, targetSilos);
                
                return new OrleansScalingResult
                {
                    Success = true,
                    ClusterName = clusterName,
                    PreviousSiloCount = currentCount,
                    CurrentSiloCount = targetSilos,
                    ScalingDuration = TimeSpan.Zero
                };
            }

            OrleansScalingResult result;
            
            if (targetSilos > currentCount)
            {
                // Scale up - add silos
                result = await ScaleUpClusterAsync(clusterName, targetSilos - currentCount, currentSilos, cancellationToken);
            }
            else
            {
                // Scale down - remove silos gracefully
                result = await ScaleDownClusterAsync(clusterName, currentCount - targetSilos, currentSilos, cancellationToken);
            }

            var duration = DateTime.UtcNow - startTime;
            result.ScalingDuration = duration;

            await metrics.RecordOrleansScalingEventAsync(clusterName, currentCount, targetSilos, duration);

            return result;
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
logger.LogError(ex, "Failed to scale Orleans cluster {ClusterName} to {TargetSilos}", clusterName, targetSilos);
            
            return new OrleansScalingResult
            {
                Success = false,
                ClusterName = clusterName,
                ScalingDuration = duration,
                Error = ex.Message
            };
        }
        finally
        {
scalingLock.Release();
        }
    }

    private async Task<OrleansScalingResult> ScaleUpClusterAsync(
        string clusterName, 
        int silosToAdd, 
        IEnumerable<SiloInfo> currentSilos,
        CancellationToken cancellationToken)
    {
logger.LogInformation("Scaling up Orleans cluster {ClusterName} by {SiloCount} silos", clusterName, silosToAdd);

        var addedSilos = new List<SiloInfo>();

        for (int i = 0; i < silosToAdd; i++)
        {
            try
            {
                var newSilo = await CreateNewSiloAsync(clusterName, cancellationToken);
                await StartSiloAsync(newSilo, cancellationToken);
                
                // Wait for silo to join cluster and become active
                await WaitForSiloActive(newSilo.Id, config.SiloStartupTimeout, cancellationToken);
                
                addedSilos.Add(newSilo);
logger.LogInformation("Successfully added silo {SiloId} to cluster {ClusterName}", newSilo.Id, clusterName);
            }
            catch (Exception ex)
            {
logger.LogError(ex, "Failed to add silo {Index} to cluster {ClusterName}", i + 1, clusterName);
                
                // If we fail to add a silo, continue with partial success
                break;
            }
        }

        // Wait for cluster stabilization
        await WaitForClusterStabilization(clusterName, config.ClusterStabilizationTimeout, cancellationToken);

        var finalSilos = await GetActiveSilosAsync(clusterName, cancellationToken);

        return new OrleansScalingResult
        {
            Success = addedSilos.Count > 0,
            ClusterName = clusterName,
            PreviousSiloCount = currentSilos.Count(),
            CurrentSiloCount = finalSilos.Count(),
            AddedSilos = addedSilos,
            Message = $"Added {addedSilos.Count} of {silosToAdd} requested silos"
        };
    }

    private async Task<OrleansScalingResult> ScaleDownClusterAsync(
        string clusterName,
        int silosToRemove,
        IEnumerable<SiloInfo> currentSilos,
        CancellationToken cancellationToken)
    {
logger.LogInformation("Scaling down Orleans cluster {ClusterName} by {SiloCount} silos", clusterName, silosToRemove);

        var removedSilos = new List<SiloInfo>();
        var silosToDecommission = SelectSilosForDecommission(currentSilos, silosToRemove);

        foreach (var silo in silosToDecommission)
        {
            try
            {
                // Graceful decommission with grain migration
                await DecommissionSiloGracefullyAsync(silo, config.GracefulShutdownTimeout, cancellationToken);
                
                removedSilos.Add(silo);
logger.LogInformation("Successfully removed silo {SiloId} from cluster {ClusterName}", silo.Id, clusterName);
            }
            catch (Exception ex)
            {
logger.LogError(ex, "Failed to remove silo {SiloId} from cluster {ClusterName}", silo.Id, clusterName);
            }
        }

        // Wait for cluster stabilization after removals
        await WaitForClusterStabilization(clusterName, config.ClusterStabilizationTimeout, cancellationToken);

        var finalSilos = await GetActiveSilosAsync(clusterName, cancellationToken);

        return new OrleansScalingResult
        {
            Success = removedSilos.Count > 0,
            ClusterName = clusterName,
            PreviousSiloCount = currentSilos.Count(),
            CurrentSiloCount = finalSilos.Count(),
            RemovedSilos = removedSilos,
            Message = $"Removed {removedSilos.Count} of {silosToRemove} requested silos"
        };
    }

    public async Task<ClusterMetrics> GetClusterMetricsAsync(string clusterName, CancellationToken cancellationToken = default)
    {
        try
        {
            var silos = await GetActiveSilosAsync(clusterName, cancellationToken);
            var siloMetrics = new List<SiloMetrics>();

            foreach (var silo in silos)
            {
                try
                {
                    var metrics = await CollectSiloMetricsAsync(silo, cancellationToken);
                    siloMetrics.Add(metrics);
                }
                catch (Exception ex)
                {
logger.LogWarning(ex, "Failed to collect metrics from silo {SiloId}", silo.Id);
                }
            }

            return new ClusterMetrics
            {
                ClusterName = clusterName,
                Timestamp = DateTime.UtcNow,
                ActiveSiloCount = silos.Count(),
                SiloMetrics = siloMetrics,
                
                // Aggregated metrics
                TotalGrainActivations = siloMetrics.Sum(m => m.ActiveGrains),
                AverageCpuUsage = siloMetrics.Any() ? siloMetrics.Average(m => m.CpuUsage) : 0,
                AverageMemoryUsage = siloMetrics.Any() ? siloMetrics.Average(m => m.MemoryUsage) : 0,
                TotalRequestsPerSecond = siloMetrics.Sum(m => m.RequestsPerSecond),
                AverageGrainCallsPerSecond = siloMetrics.Any() ? siloMetrics.Average(m => m.GrainCallsPerSecond) : 0,
                ClusterHealthScore = CalculateClusterHealthScore(siloMetrics)
            };
        }
        catch (Exception ex)
        {
logger.LogError(ex, "Failed to get cluster metrics for {ClusterName}", clusterName);
            throw;
        }
    }

    public async Task<IEnumerable<SiloInfo>> GetActiveSilosAsync(string clusterName, CancellationToken cancellationToken = default)
    {
        try
        {
            var managementGrain =clusterClient.GetGrain<IManagementGrain>(0);
            var hosts = await managementGrain.GetHosts();
            
            return hosts
                .Where(h => h.Status == SiloStatus.Active)
                .Select(h => new SiloInfo
                {
                    Id = h.SiloAddress.ToString(),
                    ClusterName = clusterName,
                    Address = h.SiloAddress.Endpoint.Address.ToString(),
                    Port = h.SiloAddress.Endpoint.Port,
                    Status = MapSiloStatus(h.Status),
                    StartTime = h.StartTime,
                    LastHeartbeat = DateTime.UtcNow // Orleans doesn't expose this directly
                });
        }
        catch (Exception ex)
        {
logger.LogError(ex, "Failed to get active silos for cluster {ClusterName}", clusterName);
            return Enumerable.Empty<SiloInfo>();
        }
    }

    private async Task<SiloInfo> CreateNewSiloAsync(string clusterName, CancellationToken cancellationToken)
    {
        var siloId = $"silo-{Guid.NewGuid():N}";
        var siloConfig = new OrleansHostConfiguration
        {
            ClusterName = clusterName,
            SiloName = siloId,
            // Configuration would be loaded from config
        };

        var silo = new SiloInfo
        {
            Id = siloId,
            ClusterName = clusterName,
            Status = SiloStatus.Created,
            StartTime = DateTime.UtcNow
        };

        return silo;
    }

    private async Task StartSiloAsync(SiloInfo silo, CancellationToken cancellationToken)
    {
logger.LogInformation("Starting Orleans silo {SiloId}", silo.Id);
        
        // This would integrate with your hosting infrastructure (Kubernetes, Docker, etc.)
        await hostingService.StartOrleansHostAsync(silo.Id, silo.ClusterName, cancellationToken);
        
        silo.Status = SiloStatus.Starting;
activeSilos.TryAdd(silo.Id, silo);
        
        SiloStatusChanged?.Invoke(this, new SiloStatusChangedEventArgs(silo, SiloStatus.Created, SiloStatus.Starting));
    }

    private async Task WaitForSiloActive(string siloId, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow.Add(timeout);
        
        while (DateTime.UtcNow < deadline && !cancellationToken.IsCancellationRequested)
        {
            var silos = await GetActiveSilosAsync(_activeSilos[siloId].ClusterName, cancellationToken);
            var targetSilo = silos.FirstOrDefault(s => s.Id == siloId);
            
            if (targetSilo?.Status == SiloStatus.Active)
            {
                _activeSilos[siloId].Status = SiloStatus.Active;
                SiloStatusChanged?.Invoke(this, new SiloStatusChangedEventArgs(targetSilo, SiloStatus.Starting, SiloStatus.Active));
                return;
            }
            
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        }
        
        throw new TimeoutException($"Silo {siloId} did not become active within {timeout}");
    }

    private async Task DecommissionSiloGracefullyAsync(SiloInfo silo, TimeSpan gracePeriod, CancellationToken cancellationToken)
    {
logger.LogInformation("Gracefully decommissioning silo {SiloId}", silo.Id);
        
        try
        {
            // Request graceful shutdown through management grain
            var managementGrain =clusterClient.GetGrain<IManagementGrain>(0);
            
            // This would trigger grain migration and graceful shutdown
            await hostingService.StopOrleansHostAsync(silo.Id, gracePeriod, cancellationToken);
            
            silo.Status = SiloStatus.Stopping;
            SiloStatusChanged?.Invoke(this, new SiloStatusChangedEventArgs(silo, SiloStatus.Active, SiloStatus.Stopping));
            
            // Wait for silo to be removed from cluster
            await WaitForSiloRemoval(silo.Id, gracePeriod, cancellationToken);
activeSilos.TryRemove(silo.Id, out _);
            silo.Status = SiloStatus.Dead;
            SiloStatusChanged?.Invoke(this, new SiloStatusChangedEventArgs(silo, SiloStatus.Stopping, SiloStatus.Dead));
        }
        catch (Exception ex)
        {
logger.LogError(ex, "Failed to gracefully decommission silo {SiloId}", silo.Id);
            throw;
        }
    }

    private IEnumerable<SiloInfo> SelectSilosForDecommission(IEnumerable<SiloInfo> silos, int count)
    {
        // Select silos for removal based on various criteria:
        // 1. Prefer silos with lower grain activation counts
        // 2. Prefer newer silos (to preserve long-running state)
        // 3. Consider resource utilization
        
        return silos
            .OrderByDescending(s => s.StartTime) // Newer first
            .Take(count);
    }

    private async Task<SiloMetrics> CollectSiloMetricsAsync(SiloInfo silo, CancellationToken cancellationToken)
    {
        try
        {
            // This would collect metrics from the silo's management interface
            return new SiloMetrics
            {
                SiloId = silo.Id,
                Timestamp = DateTime.UtcNow,
                CpuUsage = await GetSiloCpuUsageAsync(silo),
                MemoryUsage = await GetSiloMemoryUsageAsync(silo),
                ActiveGrains = await GetActiveGrainCountAsync(silo),
                GrainCallsPerSecond = await GetGrainCallRateAsync(silo),
                RequestsPerSecond = await GetRequestRateAsync(silo),
                MessageQueueLength = await GetMessageQueueLengthAsync(silo)
            };
        }
        catch (Exception ex)
        {
logger.LogWarning(ex, "Failed to collect metrics from silo {SiloId}", silo.Id);
            return new SiloMetrics { SiloId = silo.Id, Timestamp = DateTime.UtcNow };
        }
    }

    private double CalculateClusterHealthScore(List<SiloMetrics> siloMetrics)
    {
        if (!siloMetrics.Any()) return 0.0;
        
        var avgCpu = siloMetrics.Average(m => m.CpuUsage);
        var avgMemory = siloMetrics.Average(m => m.MemoryUsage);
        var maxQueueLength = siloMetrics.Max(m => m.MessageQueueLength);
        
        // Health score based on resource utilization and queue backlog
        var cpuScore = Math.Max(0, 1.0 - (avgCpu / 100.0));
        var memoryScore = Math.Max(0, 1.0 - (avgMemory / 100.0));
        var queueScore = Math.Max(0, 1.0 - (maxQueueLength / 1000.0));
        
        return (cpuScore + memoryScore + queueScore) / 3.0;
    }

    // Placeholder methods for metrics collection - would integrate with Orleans telemetry
    private async Task<double> GetSiloCpuUsageAsync(SiloInfo silo) => await Task.FromResult(45.0);
    private async Task<double> GetSiloMemoryUsageAsync(SiloInfo silo) => await Task.FromResult(60.0);
    private async Task<int> GetActiveGrainCountAsync(SiloInfo silo) => await Task.FromResult(150);
    private async Task<double> GetGrainCallRateAsync(SiloInfo silo) => await Task.FromResult(75.0);
    private async Task<double> GetRequestRateAsync(SiloInfo silo) => await Task.FromResult(25.0);
    private async Task<int> GetMessageQueueLengthAsync(SiloInfo silo) => await Task.FromResult(5);

    public void Dispose()
    {
        monitoringTimer?.Dispose();
        scalingLock?.Dispose();
    }
}
```

#### Orleans Scaling Policies

```csharp
public class OrleansGrainActivationScalingPolicy : IResourceScalingPolicy
{
    private readonly OrleansGrainScalingOptions options;
    private readonly IOrleansClusterScalingService orlenasScaling;
    private readonly ILogger<OrleansGrainActivationScalingPolicy> logger;

    public string PolicyName => "Orleans Grain Activation Scaling";
    public int Priority => 120;

    public OrleansGrainActivationScalingPolicy(
        OrleansGrainScalingOptions options,
        IOrleansClusterScalingService orleansScaling,
        ILogger<OrleansGrainActivationScalingPolicy> logger)
    {
options = options;
orlenasScaling = orleansScaling;
logger = logger;
    }

    public async Task<ScalingDecision> EvaluateAsync(ResourceScalingContext context)
    {
        var clusterMetrics = await orlenasScaling.GetClusterMetricsAsync(context.ServiceName);
        
        if (DateTime.UtcNow - context.LastScalingAction < options.CooldownPeriod)
        {
            return ScalingDecision.NoAction("Orleans grain scaling in cooldown period");
        }

        var avgGrainsPerSilo = clusterMetrics.ActiveSiloCount > 0 
            ? clusterMetrics.TotalGrainActivations / (double)clusterMetrics.ActiveSiloCount 
            : 0;

        var avgCallsPerSilo = clusterMetrics.ActiveSiloCount > 0
            ? clusterMetrics.AverageGrainCallsPerSecond * clusterMetrics.ActiveSiloCount
            : 0;

        // Scale up conditions
        if (avgGrainsPerSilo > options.MaxGrainsPerSilo || avgCallsPerSilo > options.MaxCallsPerSilo)
        {
            var targetSilos = CalculateTargetSilos(
                clusterMetrics.TotalGrainActivations, 
                avgCallsPerSilo, options.TargetGrainsPerSilo,
options.TargetCallsPerSilo);
                
            targetSilos = Math.Min(targetSilos, context.MaxInstances);

            if (targetSilos > clusterMetrics.ActiveSiloCount)
            {
logger.LogInformation("Orleans scale up: {GrainsPerSilo} grains/silo, {CallsPerSilo} calls/silo, target={Target} silos",
                    avgGrainsPerSilo, avgCallsPerSilo, targetSilos);

                return ScalingDecision.ScaleUp(targetSilos,
                    $"Grain load: {avgGrainsPerSilo:F0} grains/silo, {avgCallsPerSilo:F0} calls/silo exceeds capacity");
            }
        }

        // Scale down conditions
        if (avgGrainsPerSilo < options.MinGrainsPerSilo && avgCallsPerSilo < options.MinCallsPerSilo)
        {
            var targetSilos = Math.Max(
                CalculateTargetSilos(
                    clusterMetrics.TotalGrainActivations,
                    avgCallsPerSilo,
options.TargetGrainsPerSilo,
options.TargetCallsPerSilo),
                context.MinInstances);

            if (targetSilos < clusterMetrics.ActiveSiloCount)
            {
logger.LogInformation("Orleans scale down: {GrainsPerSilo} grains/silo, {CallsPerSilo} calls/silo, target={Target} silos",
                    avgGrainsPerSilo, avgCallsPerSilo, targetSilos);

                return ScalingDecision.ScaleDown(targetSilos,
                    $"Grain load: {avgGrainsPerSilo:F0} grains/silo, {avgCallsPerSilo:F0} calls/silo below minimum");
            }
        }

        return ScalingDecision.NoAction($"Grain load: {avgGrainsPerSilo:F0} grains/silo, {avgCallsPerSilo:F0} calls/silo within acceptable range");
    }

    private int CalculateTargetSilos(long totalGrains, double totalCalls, int targetGrainsPerSilo, double targetCallsPerSilo)
    {
        var silosForGrains = (int)Math.Ceiling((double)totalGrains / targetGrainsPerSilo);
        var silosForCalls = (int)Math.Ceiling(totalCalls / targetCallsPerSilo);
        
        // Use the higher requirement
        return Math.Max(silosForGrains, silosForCalls);
    }
}

public class OrleansClusterHealthScalingPolicy : IResourceScalingPolicy
{
    private readonly OrleansHealthScalingOptions options;
    private readonly IOrleansClusterScalingService orleansScaling;
    private readonly ILogger<OrleansClusterHealthScalingPolicy> logger;

    public string PolicyName => "Orleans Cluster Health Scaling";
    public int Priority => 110;

    public OrleansClusterHealthScalingPolicy(
        OrleansHealthScalingOptions options,
        IOrleansClusterScalingService orleansScaling,
        ILogger<OrleansClusterHealthScalingPolicy> logger)
    {
options = options;
orleansScaling = orleansScaling;
logger = logger;
    }

    public async Task<ScalingDecision> EvaluateAsync(ResourceScalingContext context)
    {
        var clusterMetrics = await orleansScaling.GetClusterMetricsAsync(context.ServiceName);
        
        if (DateTime.UtcNow - context.LastScalingAction < options.CooldownPeriod)
        {
            return ScalingDecision.NoAction("Orleans health scaling in cooldown period");
        }

        var healthScore = clusterMetrics.ClusterHealthScore;
        var unhealthySilos = clusterMetrics.SiloMetrics.Count(m => 
            m.CpuUsage > options.UnhealthyCpuThreshold || 
            m.MemoryUsage > options.UnhealthyMemoryThreshold ||
            m.MessageQueueLength > options.UnhealthyQueueLength);

        // Scale up if cluster health is poor
        if (healthScore < options.MinHealthScore || unhealthySilos > options.MaxUnhealthySilos)
        {
            var targetSilos = Math.Min(
                clusterMetrics.ActiveSiloCount + options.HealthScaleUpStep,
                context.MaxInstances);

            if (targetSilos > clusterMetrics.ActiveSiloCount)
            {
logger.LogInformation("Orleans health scale up: health={Health}, unhealthy silos={UnhealthySilos}, target={Target}",
                    healthScore, unhealthySilos, targetSilos);

                return ScalingDecision.ScaleUp(targetSilos,
                    $"Cluster health: {healthScore:F2}, unhealthy silos: {unhealthySilos}");
            }
        }

        // Scale down if cluster is over-provisioned and healthy
        if (healthScore > options.OptimalHealthScore && unhealthySilos == 0 && clusterMetrics.ActiveSiloCount > context.MinInstances)
        {
            var targetSilos = Math.Max(
                clusterMetrics.ActiveSiloCount - options.HealthScaleDownStep,
                context.MinInstances);
logger.LogInformation("Orleans health scale down: health={Health}, target={Target}",
                healthScore, targetSilos);

            return ScalingDecision.ScaleDown(targetSilos,
                $"Cluster over-provisioned: health={healthScore:F2}");
        }

        return ScalingDecision.NoAction($"Cluster health: {healthScore:F2}, unhealthy silos: {unhealthySilos}");
    }
}
```

#### Orleans Models and Configuration

```csharp
public class OrleansScalingResult
{
    public bool Success { get; set; }
    public string ClusterName { get; set; } = string.Empty;
    public int PreviousSiloCount { get; set; }
    public int CurrentSiloCount { get; set; }
    public TimeSpan ScalingDuration { get; set; }
    public string? Error { get; set; }
    public string? Message { get; set; }
    public List<SiloInfo> AddedSilos { get; set; } = new();
    public List<SiloInfo> RemovedSilos { get; set; } = new();
}

public class SiloInfo
{
    public string Id { get; set; } = string.Empty;
    public string ClusterName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public int Port { get; set; }
    public SiloStatus Status { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime LastHeartbeat { get; set; }
}

public class ClusterMetrics
{
    public string ClusterName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public int ActiveSiloCount { get; set; }
    public long TotalGrainActivations { get; set; }
    public double AverageCpuUsage { get; set; }
    public double AverageMemoryUsage { get; set; }
    public double TotalRequestsPerSecond { get; set; }
    public double AverageGrainCallsPerSecond { get; set; }
    public double ClusterHealthScore { get; set; }
    public List<SiloMetrics> SiloMetrics { get; set; } = new();
}

public class SiloMetrics
{
    public string SiloId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public double CpuUsage { get; set; }
    public double MemoryUsage { get; set; }
    public int ActiveGrains { get; set; }
    public double GrainCallsPerSecond { get; set; }
    public double RequestsPerSecond { get; set; }
    public int MessageQueueLength { get; set; }
}

public enum SiloStatus
{
    Created,
    Starting,
    Active,
    Stopping,
    Dead
}

public class SiloStatusChangedEventArgs : EventArgs
{
    public SiloInfo Silo { get; }
    public SiloStatus PreviousStatus { get; }
    public SiloStatus CurrentStatus { get; }

    public SiloStatusChangedEventArgs(SiloInfo silo, SiloStatus previousStatus, SiloStatus currentStatus)
    {
        Silo = silo;
        PreviousStatus = previousStatus;
        CurrentStatus = currentStatus;
    }
}

public class OrleansScalingConfiguration
{
    public const string SectionName = "AspireOrleansScaling";
    
    public TimeSpan SiloStartupTimeout { get; set; } = TimeSpan.FromMinutes(2);
    public TimeSpan GracefulShutdownTimeout { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan ClusterStabilizationTimeout { get; set; } = TimeSpan.FromMinutes(3);
    public TimeSpan MonitoringInterval { get; set; } = TimeSpan.FromSeconds(30);
    public Dictionary<string, OrleansClusterConfig> Clusters { get; set; } = new();
}

public class OrleansClusterConfig
{
    public bool EnableAutoScaling { get; set; } = true;
    public int MinSilos { get; set; } = 1;
    public int MaxSilos { get; set; } = 10;
    public OrleansGrainScalingOptions GrainScaling { get; set; } = new();
    public OrleansHealthScalingOptions HealthScaling { get; set; } = new();
}

public class OrleansGrainScalingOptions
{
    public int TargetGrainsPerSilo { get; set; } = 1000;
    public int MinGrainsPerSilo { get; set; } = 100;
    public int MaxGrainsPerSilo { get; set; } = 2000;
    public double TargetCallsPerSilo { get; set; } = 50.0;
    public double MinCallsPerSilo { get; set; } = 10.0;
    public double MaxCallsPerSilo { get; set; } = 100.0;
    public TimeSpan CooldownPeriod { get; set; } = TimeSpan.FromMinutes(5);
}

public class OrleansHealthScalingOptions
{
    public double MinHealthScore { get; set; } = 0.6;
    public double OptimalHealthScore { get; set; } = 0.8;
    public double UnhealthyCpuThreshold { get; set; } = 85.0;
    public double UnhealthyMemoryThreshold { get; set; } = 90.0;
    public int UnhealthyQueueLength { get; set; } = 100;
    public int MaxUnhealthySilos { get; set; } = 1;
    public int HealthScaleUpStep { get; set; } = 2;
    public int HealthScaleDownStep { get; set; } = 1;
    public TimeSpan CooldownPeriod { get; set; } = TimeSpan.FromMinutes(3);
}
```

### Orleans Integration with Aspire

```csharp
public static class OrleansScalingExtensions
{
    public static IServiceCollection AddOrleansClusterScaling(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<OrleansScalingConfiguration>(
            configuration.GetSection(OrleansScalingConfiguration.SectionName));

        // Register Orleans cluster scaling services
        services.AddSingleton<IOrleansClusterScalingService, AspireOrleansClusterScalingService>();
        services.AddSingleton<IOrleansHostingService, AspireOrleansHostingService>();

        // Register Orleans-specific scaling policies
        services.AddSingleton<IResourceScalingPolicy, OrleansGrainActivationScalingPolicy>();
        services.AddSingleton<IResourceScalingPolicy, OrleansClusterHealthScalingPolicy>();

        return services;
    }

    public static IServiceCollection AddOrleansIntegration(
        this IServiceCollection services,
        Action<OrleansClientOptions> configureClient)
    {
        services.AddOrleansClient(configureClient);
        return services;
    }
}

// Example Aspire service integration
public class OrleansAspireService : BackgroundService
{
    private readonly IOrleansClusterScalingService clusterScaling;
    private readonly IResourceScalingService resourceScaling;
    private readonly ILogger<OrleansAspireService> logger;

    public OrleansAspireService(
        IOrleansClusterScalingService clusterScaling,
        IResourceScalingService resourceScaling,
        ILogger<OrleansAspireService> logger)
    {
clusterScaling = clusterScaling;
resourceScaling = resourceScaling;
logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Integrate Orleans cluster scaling with resource scaling
                await EvaluateOrleansScalingAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
            catch (Exception ex)
            {
logger.LogError(ex, "Error in Orleans scaling evaluation");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }

    private async Task EvaluateOrleansScalingAsync(CancellationToken cancellationToken)
    {
        // This integrates Orleans-specific scaling with general resource scaling
        var orleansServices = new[] { "grain-host", "orleans-cluster" };
        
        foreach (var serviceName in orleansServices)
        {
            var decision = await resourceScaling.EvaluateScalingAsync(serviceName, cancellationToken);
            
            if (decision.ShouldScale)
            {
                var result = await clusterScaling.ScaleClusterAsync(
                    serviceName, 
                    decision.TargetInstances, 
                    cancellationToken);
logger.LogInformation("Orleans scaling result: {Result}", result.Message);
            }
        }
    }
}
```

### Orleans Scaling Configuration Example

```csharp
// appsettings.json
{
  "AspireOrleansScaling": {
    "SiloStartupTimeout": "00:02:00",
    "GracefulShutdownTimeout": "00:05:00",
    "ClusterStabilizationTimeout": "00:03:00",
    "MonitoringInterval": "00:00:30",
    "Clusters": {
      "document-processing": {
        "EnableAutoScaling": true,
        "MinSilos": 2,
        "MaxSilos": 15,
        "GrainScaling": {
          "TargetGrainsPerSilo": 800,
          "MaxGrainsPerSilo": 1500,
          "TargetCallsPerSilo": 40.0,
          "MaxCallsPerSilo": 80.0,
          "CooldownPeriod": "00:03:00"
        },
        "HealthScaling": {
          "MinHealthScore": 0.7,
          "OptimalHealthScore": 0.85,
          "UnhealthyCpuThreshold": 80.0,
          "UnhealthyMemoryThreshold": 85.0,
          "MaxUnhealthySilos": 1,
          "HealthScaleUpStep": 2
        }
      },
      "ml-inference": {
        "EnableAutoScaling": true,
        "MinSilos": 1,
        "MaxSilos": 8,
        "GrainScaling": {
          "TargetGrainsPerSilo": 500,
          "MaxGrainsPerSilo": 1000,
          "CooldownPeriod": "00:05:00"
        }
      }
    }
  }
}
```

## Monitoring and Alerting Integration

### Prometheus Metrics Integration

Comprehensive metrics collection and integration with Prometheus for scaling event monitoring and alerting.

#### Scaling Metrics Collector

```csharp
public interface IScalingMetricsCollector
{
    Task RecordScalingEventAsync(ScalingEventMetrics metrics);
    Task RecordLoadBalancingDecisionAsync(string serviceName, string endpointId, string strategy);
    Task RecordOrleansScalingEventAsync(string clusterName, int previousSilos, int currentSilos, TimeSpan duration);
    Task RecordResourceUtilizationAsync(string serviceName, ResourceMetrics metrics);
    Task RecordScalingPolicyEvaluationAsync(string serviceName, string policyName, ScalingDecision decision);
    void IncrementScalingFailures(string serviceName, string reason);
    void RecordScalingDuration(string serviceName, TimeSpan duration);
    void SetActiveInstances(string serviceName, int instances);
}

public class PrometheusScalingMetricsCollector : IScalingMetricsCollector
{
    private readonly IMetricFactory metricFactory;
    private readonly ILogger<PrometheusScalingMetricsCollector> logger;

    // Prometheus metrics
    private readonly ICounter scalingEventsCounter;
    private readonly ICounter scalingFailuresCounter;
    private readonly IHistogram scalingDurationHistogram;
    private readonly IGauge activeInstancesGauge;
    private readonly IGauge resourceUtilizationGauge;
    private readonly ICounter loadBalancingDecisionsCounter;
    private readonly ICounter policyEvaluationsCounter;
    private readonly IGauge orleansClusterSizeGauge;
    private readonly IHistogram orleansScalingDurationHistogram;

    public PrometheusScalingMetricsCollector(IMetricFactory metricFactory, ILogger<PrometheusScalingMetricsCollector> logger)
    {
metricFactory = metricFactory;
logger = logger;

        // Initialize Prometheus metrics
scalingEventsCounter =metricFactory.CreateCounter(
            "aspire_scaling_events_total",
            "Total number of scaling events",
            new[] { "serviceName", "direction", "orchestrator", "success" });
scalingFailuresCounter =metricFactory.CreateCounter(
            "aspire_scaling_failures_total",
            "Total number of scaling failures",
            new[] { "serviceName", "reason", "orchestrator" });
scalingDurationHistogram =metricFactory.CreateHistogram(
            "aspire_scaling_duration_seconds",
            "Duration of scaling operations in seconds",
            new[] { "serviceName", "direction", "orchestrator" },
            new[] { 0.1, 0.5, 1.0, 2.0, 5.0, 10.0, 30.0, 60.0, 120.0, 300.0 });
activeInstancesGauge =metricFactory.CreateGauge(
            "aspire_service_instances_active",
            "Number of active service instances",
            new[] { "serviceName", "orchestrator" });
resourceUtilizationGauge =metricFactory.CreateGauge(
            "aspire_resource_utilization_percent",
            "Resource utilization percentage",
            new[] { "serviceName", "resource_type", "metric_type" });
loadBalancingDecisionsCounter =metricFactory.CreateCounter(
            "aspire_load_balancing_decisions_total",
            "Total number of load balancing decisions",
            new[] { "serviceName", "endpoint_id", "strategy" });
policyEvaluationsCounter =metricFactory.CreateCounter(
            "aspire_scaling_policy_evaluations_total",
            "Total number of scaling policy evaluations",
            new[] { "serviceName", "policy_name", "decision" });
orleansClusterSizeGauge =metricFactory.CreateGauge(
            "aspire_orleans_cluster_silos",
            "Number of active Orleans silos in cluster",
            new[] { "cluster_name" });
orleansScalingDurationHistogram =metricFactory.CreateHistogram(
            "aspire_orleans_scaling_duration_seconds",
            "Duration of Orleans cluster scaling operations",
            new[] { "cluster_name", "direction" },
            new[] { 0.5, 1.0, 2.0, 5.0, 10.0, 30.0, 60.0, 120.0, 300.0, 600.0 });
    }

    public async Task RecordScalingEventAsync(ScalingEventMetrics metrics)
    {
        try
        {
scalingEventsCounter
                .WithLabels(metrics.ServiceName, metrics.Direction.ToString(), metrics.Orchestrator, metrics.Success.ToString())
                .Inc();
scalingDurationHistogram
                .WithLabels(metrics.ServiceName, metrics.Direction.ToString(), metrics.Orchestrator)
                .Observe(metrics.Duration.TotalSeconds);
activeInstancesGauge
                .WithLabels(metrics.ServiceName, metrics.Orchestrator)
                .Set(metrics.CurrentInstances);

            if (!metrics.Success)
            {
scalingFailuresCounter
                    .WithLabels(metrics.ServiceName, metrics.ErrorReason ?? "unknown", metrics.Orchestrator)
                    .Inc();
            }
logger.LogDebug("Recorded scaling event for {ServiceName}: {Direction} to {Instances} instances in {Duration}ms",
                metrics.ServiceName, metrics.Direction, metrics.CurrentInstances, metrics.Duration.TotalMilliseconds);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
logger.LogError(ex, "Failed to record scaling event metrics for {ServiceName}", metrics.ServiceName);
        }
    }

    public async Task RecordLoadBalancingDecisionAsync(string serviceName, string endpointId, string strategy)
    {
        try
        {
loadBalancingDecisionsCounter
                .WithLabels(serviceName, endpointId, strategy)
                .Inc();

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
logger.LogError(ex, "Failed to record load balancing decision for {ServiceName}", serviceName);
        }
    }

    public async Task RecordOrleansScalingEventAsync(string clusterName, int previousSilos, int currentSilos, TimeSpan duration)
    {
        try
        {
            var direction = currentSilos > previousSilos ? "up" : "down";
orleansScalingDurationHistogram
                .WithLabels(clusterName, direction)
                .Observe(duration.TotalSeconds);
orleansClusterSizeGauge
                .WithLabels(clusterName)
                .Set(currentSilos);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
logger.LogError(ex, "Failed to record Orleans scaling event for cluster {ClusterName}", clusterName);
        }
    }

    public async Task RecordResourceUtilizationAsync(string serviceName, ResourceMetrics metrics)
    {
        try
        {
resourceUtilizationGauge
                .WithLabels(serviceName, "cpu", "average")
                .Set(metrics.AverageCpuUsage);
resourceUtilizationGauge
                .WithLabels(serviceName, "cpu", "max")
                .Set(metrics.MaxCpuUsage);
resourceUtilizationGauge
                .WithLabels(serviceName, "memory", "average")
                .Set(metrics.AverageMemoryUsage);
resourceUtilizationGauge
                .WithLabels(serviceName, "memory", "max")
                .Set(metrics.MaxMemoryUsage);
resourceUtilizationGauge
                .WithLabels(serviceName, "requests", "total_per_second")
                .Set(metrics.TotalRequestsPerSecond);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
logger.LogError(ex, "Failed to record resource utilization for {ServiceName}", serviceName);
        }
    }

    public async Task RecordScalingPolicyEvaluationAsync(string serviceName, string policyName, ScalingDecision decision)
    {
        try
        {
policyEvaluationsCounter
                .WithLabels(serviceName, policyName, decision.Direction.ToString())
                .Inc();

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
logger.LogError(ex, "Failed to record policy evaluation for {ServiceName}", serviceName);
        }
    }

    public void IncrementScalingFailures(string serviceName, string reason)
    {
        try
        {
scalingFailuresCounter
                .WithLabels(serviceName, reason, "unknown")
                .Inc();
        }
        catch (Exception ex)
        {
logger.LogError(ex, "Failed to increment scaling failures for {ServiceName}", serviceName);
        }
    }

    public void RecordScalingDuration(string serviceName, TimeSpan duration)
    {
        try
        {
scalingDurationHistogram
                .WithLabels(serviceName, "unknown", "unknown")
                .Observe(duration.TotalSeconds);
        }
        catch (Exception ex)
        {
logger.LogError(ex, "Failed to record scaling duration for {ServiceName}", serviceName);
        }
    }

    public void SetActiveInstances(string serviceName, int instances)
    {
        try
        {
activeInstancesGauge
                .WithLabels(serviceName, "unknown")
                .Set(instances);
        }
        catch (Exception ex)
        {
logger.LogError(ex, "Failed to set active instances for {ServiceName}", serviceName);
        }
    }
}
```

#### Grafana Dashboard Integration

```csharp
public interface IGrafanaDashboardService
{
    Task<string> CreateScalingDashboardAsync(string organizationId, ScalingDashboardConfig config);
    Task UpdateDashboardAsync(string dashboardId, ScalingDashboardConfig config);
    Task<DashboardData> GetDashboardDataAsync(string dashboardId, TimeRange timeRange);
    Task CreateScalingAlertsAsync(string serviceName, ScalingAlertConfig alertConfig);
}

public class AspireGrafanaDashboardService : IGrafanaDashboardService
{
    private readonly HttpClient httpClient;
    private readonly GrafanaConfiguration config;
    private readonly ILogger<AspireGrafanaDashboardService> logger;

    public AspireGrafanaDashboardService(
        HttpClient httpClient,
        IOptions<GrafanaConfiguration> config,
        ILogger<AspireGrafanaDashboardService> logger)
    {
httpClient = httpClient;
config = config.Value;
logger = logger;
        
        ConfigureHttpClient();
    }

    public async Task<string> CreateScalingDashboardAsync(string organizationId, ScalingDashboardConfig config)
    {
        try
        {
            var dashboardJson = GenerateScalingDashboard(config);
            
            var request = new
            {
                dashboard = dashboardJson,
                folderId = config.FolderId,
                overwrite = true,
                message = "Created by Aspire Scaling Service"
            };

            var response = await httpClient.PostAsJsonAsync("/api/dashboards/db", request);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<GrafanaDashboardResponse>();
logger.LogInformation("Created Grafana scaling dashboard: {DashboardId} for organization {OrgId}",
                result?.Id, organizationId);

            return result?.Uid ?? string.Empty;
        }
        catch (Exception ex)
        {
logger.LogError(ex, "Failed to create Grafana scaling dashboard for organization {OrgId}", organizationId);
            throw;
        }
    }

    private object GenerateScalingDashboard(ScalingDashboardConfig config)
    {
        return new
        {
            id = (int?)null,
            uid = config.Uid,
            title = config.Title,
            tags = new[] { "aspire", "scaling", "monitoring" },
            timezone = "browser",
            panels = GenerateDashboardPanels(config),
            time = new
            {
                from = "now-1h",
                to = "now"
            },
            timepicker = new { },
            templating = new
            {
                list = new[]
                {
                    new
                    {
                        name = "service",
                        type = "query",
                        query = "label_values(aspire_service_instancesactive, servicename)",
                        refresh = 1,
                        includeAll = true,
                        multi = true
                    }
                }
            },
            refresh = "30s"
        };
    }

    private object[] GenerateDashboardPanels(ScalingDashboardConfig config)
    {
        return new object[]
        {
            // Service instances panel
            new
            {
                id = 1,
                title = "Active Service Instances",
                type = "stat",
                targets = new[]
                {
                    new
                    {
                        expr = "aspire_service_instances_active{servicename=~\"$service\"}",
                        refId = "A"
                    }
                },
                gridPos = new { h = 8, w = 12, x = 0, y = 0 },
                fieldConfig = new
                {
                    defaults = new
                    {
                        color = new { mode = "thresholds" },
                        thresholds = new
                        {
                            steps = new[]
                            {
                                new { color = "green", value = (int?)null },
                                new { color = "red", value = 80 }
                            }
                        }
                    }
                }
            },
            
            // Scaling events panel
            new
            {
                id = 2,
                title = "Scaling Events Rate",
                type = "graph",
                targets = new[]
                {
                    new
                    {
                        expr = "rate(aspire_scaling_events_total{servicename=~\"$service\"}[5m])",
                        refId = "A",
                        legendFormat = "{{serviceName}} - {{direction}}"
                    }
                },
                gridPos = new { h = 8, w = 12, x = 12, y = 0 }
            },
            
            // Resource utilization panel
            new
            {
                id = 3,
                title = "Resource Utilization",
                type = "graph",
                targets = new[]
                {
                    new
                    {
                        expr = "aspire_resource_utilization_percent{servicename=~\"$service\", resourcetype=\"cpu\", metrictype=\"average\"}",
                        refId = "A",
                        legendFormat = "{{serviceName}} - CPU Avg"
                    },
                    new
                    {
                        expr = "aspire_resource_utilization_percent{servicename=~\"$service\", resourcetype=\"memory\", metrictype=\"average\"}",
                        refId = "B",
                        legendFormat = "{{serviceName}} - Memory Avg"
                    }
                },
                gridPos = new { h = 8, w = 24, x = 0, y = 8 },
                yAxes = new[]
                {
                    new { max = 100, min = 0, unit = "percent" },
                    new { }
                }
            },
            
            // Scaling duration panel
            new
            {
                id = 4,
                title = "Scaling Duration",
                type = "graph",
                targets = new[]
                {
                    new
                    {
                        expr = "histogram_quantile(0.95, rate(aspire_scaling_duration_seconds_bucket{servicename=~\"$service\"}[5m]))",
                        refId = "A",
                        legendFormat = "{{serviceName}} - 95th percentile"
                    }
                },
                gridPos = new { h = 8, w = 12, x = 0, y = 16 }
            },
            
            // Orleans cluster size panel (if Orleans is enabled)
            new
            {
                id = 5,
                title = "Orleans Cluster Size",
                type = "stat",
                targets = new[]
                {
                    new
                    {
                        expr = "aspire_orleans_cluster_silos",
                        refId = "A"
                    }
                },
                gridPos = new { h = 8, w = 12, x = 12, y = 16 }
            }
        };
    }

    private void ConfigureHttpClient()
    {
httpClient.BaseAddress = new Uri(config.BaseUrl);
httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", config.ApiKey);
    }
}
```

#### Alert Manager Integration

```csharp
public interface IScalingAlertService
{
    Task CreateScalingAlertsAsync(string serviceName, ScalingAlertConfiguration alertConfig);
    Task UpdateAlertAsync(string alertId, ScalingAlertConfiguration alertConfig);
    Task<IEnumerable<ActiveAlert>> GetActiveAlertsAsync(string serviceName = "");
    Task AcknowledgeAlertAsync(string alertId, string acknowledgmentMessage);
    event EventHandler<ScalingAlertTriggeredEventArgs> AlertTriggered;
}

public class PrometheusAlertService : IScalingAlertService
{
    private readonly HttpClient httpClient;
    private readonly AlertManagerConfiguration config;
    private readonly ILogger<PrometheusAlertService> logger;

    public event EventHandler<ScalingAlertTriggeredEventArgs>? AlertTriggered;

    public PrometheusAlertService(
        HttpClient httpClient,
        IOptions<AlertManagerConfiguration> config,
        ILogger<PrometheusAlertService> logger)
    {
httpClient = httpClient;
config = config.Value;
logger = logger;
    }

    public async Task CreateScalingAlertsAsync(string serviceName, ScalingAlertConfiguration alertConfig)
    {
        try
        {
            var alertRules = GenerateScalingAlertRules(serviceName, alertConfig);
            
            foreach (var rule in alertRules)
            {
                await CreateAlertRuleAsync(rule);
            }
logger.LogInformation("Created {Count} scaling alerts for service {ServiceName}", 
                alertRules.Count(), serviceName);
        }
        catch (Exception ex)
        {
logger.LogError(ex, "Failed to create scaling alerts for service {ServiceName}", serviceName);
            throw;
        }
    }

    private IEnumerable<AlertRule> GenerateScalingAlertRules(string serviceName, ScalingAlertConfiguration config)
    {
        var rules = new List<AlertRule>();

        // High resource utilization alert
        if (config.EnableResourceAlerts)
        {
            rules.Add(new AlertRule
            {
                Alert = $"HighResourceUtilization_{serviceName}",
                Expr = $"aspire_resource_utilization_percent{{servicename=\"{serviceName}\", metrictype=\"average\"}} > {config.HighResourceThreshold}",
                For = config.AlertDuration,
                Labels = new Dictionary<string, string>
                {
                    ["severity"] = "warning",
                    ["service"] = serviceName,
                    ["alert_type"] = "resource_utilization"
                },
                Annotations = new Dictionary<string, string>
                {
                    ["summary"] = $"High resource utilization for {serviceName}",
                    ["description"] = $"Service {serviceName} has resource utilization above {config.HighResourceThreshold}% for more than {config.AlertDuration}"
                }
            });
        }

        // Scaling failure alert
        if (config.EnableScalingFailureAlerts)
        {
            rules.Add(new AlertRule
            {
                Alert = $"ScalingFailure_{serviceName}",
                Expr = $"increase(aspire_scaling_failures_total{{servicename=\"{serviceName}\"}}[5m]) > {config.ScalingFailureThreshold}",
                For = "1m",
                Labels = new Dictionary<string, string>
                {
                    ["severity"] = "critical",
                    ["service"] = serviceName,
                    ["alert_type"] = "scaling_failure"
                },
                Annotations = new Dictionary<string, string>
                {
                    ["summary"] = $"Scaling failures detected for {serviceName}",
                    ["description"] = $"Service {serviceName} has experienced {config.ScalingFailureThreshold}+ scaling failures in the last 5 minutes"
                }
            });
        }

        // Long scaling duration alert
        if (config.EnablePerformanceAlerts)
        {
            rules.Add(new AlertRule
            {
                Alert = $"LongScalingDuration_{serviceName}",
                Expr = $"histogram_quantile(0.95, rate(aspire_scaling_duration_seconds_bucket{{servicename=\"{serviceName}\"}}[5m])) > {config.MaxScalingDuration.TotalSeconds}",
                For = config.AlertDuration,
                Labels = new Dictionary<string, string>
                {
                    ["severity"] = "warning",
                    ["service"] = serviceName,
                    ["alert_type"] = "performance"
                },
                Annotations = new Dictionary<string, string>
                {
                    ["summary"] = $"Long scaling duration for {serviceName}",
                    ["description"] = $"95th percentile scaling duration for {serviceName} exceeds {config.MaxScalingDuration.TotalSeconds} seconds"
                }
            });
        }

        // Orleans cluster health alert
        if (config.EnableOrleansAlerts)
        {
            rules.Add(new AlertRule
            {
                Alert = $"OrleansClusterUnhealthy_{serviceName}",
                Expr = $"aspire_orleans_cluster_silos{{clustername=\"{serviceName}\"}} < {config.MinOrleansClusterSize}",
                For = "2m",
                Labels = new Dictionary<string, string>
                {
                    ["severity"] = "critical",
                    ["service"] = serviceName,
                    ["alert_type"] = "orleans_cluster"
                },
                Annotations = new Dictionary<string, string>
                {
                    ["summary"] = $"Orleans cluster {serviceName} below minimum size",
                    ["description"] = $"Orleans cluster {serviceName} has fewer than {config.MinOrleansClusterSize} active silos"
                }
            });
        }

        return rules;
    }

    public async Task<IEnumerable<ActiveAlert>> GetActiveAlertsAsync(string serviceName = "")
    {
        try
        {
            var filter = string.IsNullOrEmpty(serviceName) ? "" : $"?filter=service=\"{serviceName}\"";
            var response = await httpClient.GetAsync($"/api/v1/alerts{filter}");
            response.EnsureSuccessStatusCode();

            var alertResponse = await response.Content.ReadFromJsonAsync<AlertManagerResponse>();
            
            return alertResponse?.Data?.Select(alert => new ActiveAlert
            {
                Id = alert.Fingerprint,
                ServiceName = alert.Labels.GetValueOrDefault("service", ""),
                AlertName = alert.Labels.GetValueOrDefault("alertname", ""),
                Severity = alert.Labels.GetValueOrDefault("severity", ""),
                Status = alert.Status.State,
                StartsAt = alert.StartsAt,
                EndsAt = alert.EndsAt,
                Summary = alert.Annotations.GetValueOrDefault("summary", ""),
                Description = alert.Annotations.GetValueOrDefault("description", "")
            }) ?? Enumerable.Empty<ActiveAlert>();
        }
        catch (Exception ex)
        {
logger.LogError(ex, "Failed to get active alerts for service {ServiceName}", serviceName);
            return Enumerable.Empty<ActiveAlert>();
        }
    }
}
```

#### Monitoring Models and Configuration

```csharp
public class ScalingEventMetrics
{
    public string ServiceName { get; set; } = string.Empty;
    public string Orchestrator { get; set; } = string.Empty;
    public ScalingDirection Direction { get; set; }
    public int PreviousInstances { get; set; }
    public int CurrentInstances { get; set; }
    public TimeSpan Duration { get; set; }
    public bool Success { get; set; }
    public string? ErrorReason { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class ScalingDashboardConfig
{
    public string Uid { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int? FolderId { get; set; }
    public List<string> Services { get; set; } = new();
    public bool IncludeOrleansMetrics { get; set; } = true;
    public TimeSpan RefreshInterval { get; set; } = TimeSpan.FromSeconds(30);
}

public class ScalingAlertConfiguration
{
    public bool EnableResourceAlerts { get; set; } = true;
    public bool EnableScalingFailureAlerts { get; set; } = true;
    public bool EnablePerformanceAlerts { get; set; } = true;
    public bool EnableOrleansAlerts { get; set; } = false;
    
    public double HighResourceThreshold { get; set; } = 85.0;
    public int ScalingFailureThreshold { get; set; } = 3;
    public TimeSpan MaxScalingDuration { get; set; } = TimeSpan.FromMinutes(5);
    public int MinOrleansClusterSize { get; set; } = 1;
    public string AlertDuration { get; set; } = "5m";
    
    public Dictionary<string, string> NotificationChannels { get; set; } = new();
}

public class ActiveAlert
{
    public string Id { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string AlertName { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class MonitoringConfiguration
{
    public const string SectionName = "AspireMonitoring";
    
    public PrometheusConfig Prometheus { get; set; } = new();
    public GrafanaConfiguration Grafana { get; set; } = new();
    public AlertManagerConfiguration AlertManager { get; set; } = new();
    public bool EnableMetricsCollection { get; set; } = true;
    public TimeSpan MetricsRetention { get; set; } = TimeSpan.FromDays(30);
}

public class PrometheusConfig
{
    public string BaseUrl { get; set; } = "http://localhost:9090";
    public string MetricsPath { get; set; } = "/metrics";
    public TimeSpan ScrapeInterval { get; set; } = TimeSpan.FromSeconds(15);
}

public class GrafanaConfiguration
{
    public string BaseUrl { get; set; } = "http://localhost:3000";
    public string ApiKey { get; set; } = string.Empty;
    public string OrganizationId { get; set; } = "1";
}

public class AlertManagerConfiguration
{
    public string BaseUrl { get; set; } = "http://localhost:9093";
    public string WebhookUrl { get; set; } = string.Empty;
    public Dictionary<string, NotificationChannel> NotificationChannels { get; set; } = new();
}

public class NotificationChannel
{
    public string Type { get; set; } = string.Empty; // slack, email, webhook
    public string Url { get; set; } = string.Empty;
    public Dictionary<string, string> Settings { get; set; } = new();
}
```

### Monitoring Integration Extensions

```csharp
public static class MonitoringServiceExtensions
{
    public static IServiceCollection AddScalingMonitoring(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<MonitoringConfiguration>(
            configuration.GetSection(MonitoringConfiguration.SectionName));

        // Register Prometheus metrics
        services.AddSingleton<IMetricFactory, MetricFactory>();
        services.AddSingleton<IScalingMetricsCollector, PrometheusScalingMetricsCollector>();

        // Register Grafana integration
        services.AddHttpClient<IGrafanaDashboardService, AspireGrafanaDashboardService>();
        
        // Register AlertManager integration
        services.AddHttpClient<IScalingAlertService, PrometheusAlertService>();

        // Register monitoring background service
        services.AddHostedService<ScalingMonitoringService>();

        return services;
    }
}

public class ScalingMonitoringService : BackgroundService
{
    private readonly IScalingMetricsCollector metricsCollector;
    private readonly IGrafanaDashboardService grafanaService;
    private readonly IScalingAlertService alertService;
    private readonly ILogger<ScalingMonitoringService> logger;
    private readonly MonitoringConfiguration config;

    public ScalingMonitoringService(
        IScalingMetricsCollector metricsCollector,
        IGrafanaDashboardService grafanaService,
        IScalingAlertService alertService,
        IOptions<MonitoringConfiguration> config,
        ILogger<ScalingMonitoringService> logger)
    {
metricsCollector = metricsCollector;
grafanaService = grafanaService;
alertService = alertService;
config = config.Value;
logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!config.EnableMetricsCollection)
        {
logger.LogInformation("Metrics collection is disabled");
            return;
        }
logger.LogInformation("Starting scaling monitoring service");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await MonitorActiveAlertsAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
            catch (Exception ex)
            {
logger.LogError(ex, "Error in scaling monitoring service");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }

    private async Task MonitorActiveAlertsAsync(CancellationToken cancellationToken)
    {
        var activeAlerts = await alertService.GetActiveAlertsAsync();
        
        foreach (var alert in activeAlerts.Where(a => a.Status == "firing"))
        {
logger.LogWarning("Active scaling alert: {AlertName} for service {ServiceName} - {Summary}",
                alert.AlertName, alert.ServiceName, alert.Summary);
        }
    }
}
```

### Monitoring Configuration Example

```csharp
// appsettings.json
{
  "AspireMonitoring": {
    "EnableMetricsCollection": true,
    "MetricsRetention": "30.00:00:00",
    "Prometheus": {
      "BaseUrl": "http://localhost:9090",
      "MetricsPath": "/metrics",
      "ScrapeInterval": "00:00:15"
    },
    "Grafana": {
      "BaseUrl": "http://localhost:3000",
      "ApiKey": "your-grafana-api-key",
      "OrganizationId": "1"
    },
    "AlertManager": {
      "BaseUrl": "http://localhost:9093",
      "WebhookUrl": "http://localhost:8080/webhooks/alertmanager",
      "NotificationChannels": {
        "slack": {
          "Type": "slack",
          "Url": "https://hooks.slack.com/services/...",
          "Settings": {
            "channel": "#alerts",
            "username": "AspireScaling"
          }
        },
        "email": {
          "Type": "email",
          "Settings": {
            "to": "ops-team@company.com",
            "subject": "Aspire Scaling Alert"
          }
        }
      }
    }
  }
}
```

## Implementation Summary

This document provides comprehensive auto-scaling and load balancing patterns for .NET Aspire applications:

### Completed Implementations

- **✅ Container Scaling**: Full Docker Compose and Kubernetes scaling services with:
  - Production-ready scaling orchestration
  - Container health monitoring and rollout verification
  - Policy-driven scaling decisions with metrics integration
  - Horizontal Pod Autoscaler (HPA) support for Kubernetes
  - Process execution and container state management

- **✅ Load Balancing Strategies**: Comprehensive traffic distribution with:
  - Multiple algorithms (Round Robin, Least Connections, Health-Based, Response Time)
  - Circuit breaker integration for fault tolerance
  - Service mesh compatibility (Istio, Linkerd, Consul)
  - Real-time health monitoring and endpoint management
  - Weighted routing and consistent hashing support

- **✅ Resource-Based Scaling Policies**: Intelligent metric-driven scaling with:
  - CPU, memory, and request rate-based policies
  - Real-time Prometheus metrics collection and aggregation
  - Composite policy evaluation with consensus and weighted strategies
  - Configurable thresholds, cooldown periods, and scaling factors
  - Multi-instance resource monitoring and health scoring

- **✅ Orleans Cluster Scaling Patterns**: Grain-aware distributed scaling with:
  - Silo lifecycle management with graceful decommissioning
  - Grain activation and call rate-based scaling policies
  - Cluster health monitoring and automatic remediation
  - Integration with Orleans management grain and telemetry
  - Safe cluster membership changes with stabilization periods

### Future Sections

- **Monitoring and Alerting Integration**: Prometheus/Grafana integration for scaling events

## Related Patterns

- [Health Monitoring](health-monitoring.md) - Health-based scaling decisions
- [Resource Dependencies](resource-dependencies.md) - Dependency-aware scaling
- [Service Orchestration](service-orchestration.md) - Service coordination during scaling
- [Configuration Management](configuration-management.md) - Scaling configuration patterns
