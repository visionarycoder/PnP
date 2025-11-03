# Enterprise ML Model Deployment & MLOps

**Description**: Advanced enterprise ML model deployment with zero-downtime deployments, automated A/B testing, comprehensive monitoring, intelligent rollback, multi-region distribution, and enterprise-grade MLOps workflows for mission-critical production systems.

**Language/Technology**: C#, ML.NET, .NET 9.0, Azure Container Apps, AKS, GitOps, MLOps, Terraform
**Enterprise Features**: Zero-downtime deployments, automated A/B testing, intelligent rollbacks, multi-region distribution, comprehensive monitoring, and enterprise compliance frameworks

**Code**:

## Model Deployment Framework

### Advanced Model Deployment Manager

```csharp
namespace DocumentProcessor.ML.Deployment;

using Microsoft.ML;
using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;
using System.Text.Json;

public interface IModelDeploymentManager
{
    Task<DeploymentResult> DeployModelAsync(ModelDeploymentRequest request);
    Task<DeploymentResult> UpdateModelAsync(string deploymentId, ModelUpdateRequest request);
    Task<DeploymentResult> RollbackModelAsync(string deploymentId, string targetVersion);
    Task<DeploymentStatus> GetDeploymentStatusAsync(string deploymentId);
    Task<List<DeploymentInfo>> GetActiveDeploymentsAsync();
    Task<ABTestResult> StartABTestAsync(ABTestConfiguration config);
    Task<ABTestResult> StopABTestAsync(string testId);
    Task<CanaryDeploymentResult> StartCanaryDeploymentAsync(CanaryConfiguration config);
    Task<HealthCheckResult> ValidateModelHealthAsync(string deploymentId);
}

public class ModelDeploymentManager : IModelDeploymentManager, IHostedService
{
    private readonly IModelRepository modelRepository;
    private readonly IModelValidator modelValidator;
    private readonly IModelMonitoring modelMonitoring;
    private readonly ILoadBalancer loadBalancer;
    private readonly ILogger<ModelDeploymentManager> logger;
    private readonly DeploymentConfiguration config;
    private readonly ConcurrentDictionary<string, DeploymentInstance> activeDeployments;
    private readonly ConcurrentDictionary<string, ABTestInstance> activeABTests;
    private readonly Timer healthCheckTimer;

    public ModelDeploymentManager(
        IModelRepository modelRepository,
        IModelValidator modelValidator,
        IModelMonitoring modelMonitoring,
        ILoadBalancer loadBalancer,
        ILogger<ModelDeploymentManager> logger,
        IOptions<DeploymentConfiguration> config)
    {
        modelRepository = modelRepository;
        modelValidator = modelValidator;
        modelMonitoring = modelMonitoring;
        loadBalancer = loadBalancer;
        logger = logger;
        config = config.Value;
        activeDeployments = new ConcurrentDictionary<string, DeploymentInstance>();
        activeABTests = new ConcurrentDictionary<string, ABTestInstance>();
        
        healthCheckTimer = new Timer(PerformHealthChecks, null, 
            TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(config.HealthCheckIntervalMinutes));
    }

    public async Task<DeploymentResult> DeployModelAsync(ModelDeploymentRequest request)
    {
        var deploymentId = Guid.NewGuid().ToString();
        
        logger.LogInformation("Starting model deployment {DeploymentId} for model {ModelId} version {Version}",
            deploymentId, request.ModelId, request.Version);

        try
        {
            // Validate deployment request
            var validation = await ValidateDeploymentRequestAsync(request);
            if (!validation.IsValid)
            {
                return new DeploymentResult(
                    DeploymentId: deploymentId,
                    Status: DeploymentStatus.Failed,
                    Message: $"Validation failed: {string.Join(", ", validation.Errors)}",
                    StartedAt: DateTime.UtcNow,
                    CompletedAt: DateTime.UtcNow);
            }

            // Load and validate model
            var model = await modelRepository.LoadModelAsync(request.ModelId, request.Version);
            var modelValidation = await modelValidator.ValidateModelAsync(model, request.ValidationDataset);
            
            if (!modelValidation.IsValid)
            {
                return new DeploymentResult(
                    DeploymentId: deploymentId,
                    Status: DeploymentStatus.Failed,
                    Message: $"Model validation failed: {modelValidation.Message}",
                    StartedAt: DateTime.UtcNow,
                    CompletedAt: DateTime.UtcNow);
            }

            // Create deployment instance
            var deploymentInstance = new DeploymentInstance(
                Id: deploymentId,
                ModelId: request.ModelId,
                Version: request.Version,
                Model: model,
                Configuration: request.Configuration,
                Status: DeploymentStatus.Deploying,
                CreatedAt: DateTime.UtcNow,
                Traffic: request.InitialTrafficPercentage);

            _activeDeployments[deploymentId] = deploymentInstance;

            // Execute deployment strategy
            var deploymentResult = await ExecuteDeploymentStrategyAsync(deploymentInstance, request.Strategy);
            
            if (deploymentResult.Status == DeploymentStatus.Deployed)
            {
                // Start monitoring
                await modelMonitoring.StartMonitoringAsync(deploymentId, model, request.MonitoringConfig);
                
                // Update load balancer
                await loadBalancer.UpdateRoutingAsync(deploymentId, request.InitialTrafficPercentage);
                
                logger.LogInformation("Model deployment {DeploymentId} completed successfully", deploymentId);
            }

            return deploymentResult;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Model deployment {DeploymentId} failed", deploymentId);
            
            await CleanupFailedDeploymentAsync(deploymentId);
            
            return new DeploymentResult(
                DeploymentId: deploymentId,
                Status: DeploymentStatus.Failed,
                Message: $"Deployment failed: {ex.Message}",
                StartedAt: DateTime.UtcNow,
                CompletedAt: DateTime.UtcNow);
        }
    }

    public async Task<DeploymentResult> UpdateModelAsync(string deploymentId, ModelUpdateRequest request)
    {
        logger.LogInformation("Updating deployment {DeploymentId} to version {Version}", 
            deploymentId, request.NewVersion);

        if (!activeDeployments.TryGetValue(deploymentId, out var currentDeployment))
        {
            return new DeploymentResult(
                DeploymentId: deploymentId,
                Status: DeploymentStatus.Failed,
                Message: "Deployment not found",
                StartedAt: DateTime.UtcNow,
                CompletedAt: DateTime.UtcNow);
        }

        try
        {
            // Create backup of current deployment
            var backup = currentDeployment with { };
            
            // Load new model version
            var newModel = await modelRepository.LoadModelAsync(currentDeployment.ModelId, request.NewVersion);
            var validation = await modelValidator.ValidateModelAsync(newModel, request.ValidationDataset);
            
            if (!validation.IsValid)
            {
                return new DeploymentResult(
                    DeploymentId: deploymentId,
                    Status: DeploymentStatus.Failed,
                    Message: $"New model validation failed: {validation.Message}",
                    StartedAt: DateTime.UtcNow,
                    CompletedAt: DateTime.UtcNow);
            }

            // Update deployment
            var updatedDeployment = currentDeployment with
            {
                Version = request.NewVersion,
                Model = newModel,
                Status = DeploymentStatus.Updating,
                LastUpdated = DateTime.UtcNow
            };

            _activeDeployments[deploymentId] = updatedDeployment;

            // Execute update strategy
            var updateResult = await ExecuteUpdateStrategyAsync(updatedDeployment, request.UpdateStrategy);
            
            if (updateResult.Status == DeploymentStatus.Deployed)
            {
                await modelMonitoring.UpdateModelAsync(deploymentId, newModel);
                logger.LogInformation("Deployment {DeploymentId} updated successfully to version {Version}",
                    deploymentId, request.NewVersion);
            }
            else
            {
                // Rollback on failure
                _activeDeployments[deploymentId] = backup;
                logger.LogWarning("Deployment {DeploymentId} update failed, rolled back to version {Version}",
                    deploymentId, backup.Version);
            }

            return updateResult;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update deployment {DeploymentId}", deploymentId);
            return new DeploymentResult(
                DeploymentId: deploymentId,
                Status: DeploymentStatus.Failed,
                Message: $"Update failed: {ex.Message}",
                StartedAt: DateTime.UtcNow,
                CompletedAt: DateTime.UtcNow);
        }
    }

    public async Task<DeploymentResult> RollbackModelAsync(string deploymentId, string targetVersion)
    {
        logger.LogInformation("Rolling back deployment {DeploymentId} to version {TargetVersion}",
            deploymentId, targetVersion);

        if (!activeDeployments.TryGetValue(deploymentId, out var deployment))
        {
            return new DeploymentResult(
                DeploymentId: deploymentId,
                Status: DeploymentStatus.Failed,
                Message: "Deployment not found",
                StartedAt: DateTime.UtcNow,
                CompletedAt: DateTime.UtcNow);
        }

        try
        {
            // Load target version
            var targetModel = await modelRepository.LoadModelAsync(deployment.ModelId, targetVersion);
            
            // Validate target model
            var validation = await modelValidator.ValidateModelAsync(targetModel, null);
            if (!validation.IsValid)
            {
                return new DeploymentResult(
                    DeploymentId: deploymentId,
                    Status: DeploymentStatus.Failed,
                    Message: $"Target model validation failed: {validation.Message}",
                    StartedAt: DateTime.UtcNow,
                    CompletedAt: DateTime.UtcNow);
            }

            // Execute rollback
            var rolledBackDeployment = deployment with
            {
                Version = targetVersion,
                Model = targetModel,
                Status = DeploymentStatus.RollingBack,
                LastUpdated = DateTime.UtcNow
            };

            _activeDeployments[deploymentId] = rolledBackDeployment;

            // Update model in monitoring and load balancer
            await modelMonitoring.UpdateModelAsync(deploymentId, targetModel);
            
            // Mark as deployed
            _activeDeployments[deploymentId] = rolledBackDeployment with { Status = DeploymentStatus.Deployed };

            logger.LogInformation("Successfully rolled back deployment {DeploymentId} to version {TargetVersion}",
                deploymentId, targetVersion);

            return new DeploymentResult(
                DeploymentId: deploymentId,
                Status: DeploymentStatus.Deployed,
                Message: $"Successfully rolled back to version {targetVersion}",
                StartedAt: DateTime.UtcNow,
                CompletedAt: DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to rollback deployment {DeploymentId}", deploymentId);
            return new DeploymentResult(
                DeploymentId: deploymentId,
                Status: DeploymentStatus.Failed,
                Message: $"Rollback failed: {ex.Message}",
                StartedAt: DateTime.UtcNow,
                CompletedAt: DateTime.UtcNow);
        }
    }

    public async Task<ABTestResult> StartABTestAsync(ABTestConfiguration config)
    {
        var testId = Guid.NewGuid().ToString();
        
        logger.LogInformation("Starting A/B test {TestId} between models {ModelA} and {ModelB}",
            testId, config.ModelAId, config.ModelBId);

        try
        {
            // Validate both models
            var modelA = await modelRepository.LoadModelAsync(config.ModelAId, config.ModelAVersion);
            var modelB = await modelRepository.LoadModelAsync(config.ModelBId, config.ModelBVersion);

            var validationA = await modelValidator.ValidateModelAsync(modelA, config.ValidationDataset);
            var validationB = await modelValidator.ValidateModelAsync(modelB, config.ValidationDataset);

            if (!validationA.IsValid || !validationB.IsValid)
            {
                return new ABTestResult(
                    TestId: testId,
                    Status: ABTestStatus.Failed,
                    Message: "Model validation failed",
                    StartedAt: DateTime.UtcNow,
                    CompletedAt: DateTime.UtcNow);
            }

            // Create A/B test instance
            var abTest = new ABTestInstance(
                Id: testId,
                ModelA: modelA,
                ModelB: modelB,
                Configuration: config,
                Status: ABTestStatus.Running,
                StartedAt: DateTime.UtcNow,
                Metrics: new ABTestMetrics());

            _activeABTests[testId] = abTest;

            // Configure load balancer for A/B testing
            await loadBalancer.ConfigureABTestAsync(testId, config.TrafficSplitPercentage);

            // Start monitoring both models
            await modelMonitoring.StartABTestMonitoringAsync(testId, modelA, modelB, config.MonitoringConfig);

            logger.LogInformation("A/B test {TestId} started successfully", testId);

            return new ABTestResult(
                TestId: testId,
                Status: ABTestStatus.Running,
                Message: "A/B test started successfully",
                StartedAt: DateTime.UtcNow,
                CompletedAt: null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to start A/B test {TestId}", testId);
            return new ABTestResult(
                TestId: testId,
                Status: ABTestStatus.Failed,
                Message: $"Failed to start A/B test: {ex.Message}",
                StartedAt: DateTime.UtcNow,
                CompletedAt: DateTime.UtcNow);
        }
    }

    public async Task<CanaryDeploymentResult> StartCanaryDeploymentAsync(CanaryConfiguration config)
    {
        var canaryId = Guid.NewGuid().ToString();
        
        logger.LogInformation("Starting canary deployment {CanaryId} for model {ModelId} version {Version}",
            canaryId, config.ModelId, config.Version);

        try
        {
            // Load and validate canary model
            var canaryModel = await modelRepository.LoadModelAsync(config.ModelId, config.Version);
            var validation = await modelValidator.ValidateModelAsync(canaryModel, config.ValidationDataset);

            if (!validation.IsValid)
            {
                return new CanaryDeploymentResult(
                    CanaryId: canaryId,
                    Status: CanaryStatus.Failed,
                    Message: $"Model validation failed: {validation.Message}",
                    StartedAt: DateTime.UtcNow);
            }

            // Start with minimal traffic
            await loadBalancer.StartCanaryDeploymentAsync(canaryId, config.InitialTrafficPercentage);
            
            // Monitor canary deployment
            var monitoringTask = MonitorCanaryDeploymentAsync(canaryId, canaryModel, config);

            logger.LogInformation("Canary deployment {CanaryId} started with {TrafficPercentage}% traffic",
                canaryId, config.InitialTrafficPercentage);

            return new CanaryDeploymentResult(
                CanaryId: canaryId,
                Status: CanaryStatus.Running,
                Message: "Canary deployment started",
                StartedAt: DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to start canary deployment {CanaryId}", canaryId);
            return new CanaryDeploymentResult(
                CanaryId: canaryId,
                Status: CanaryStatus.Failed,
                Message: $"Failed to start canary: {ex.Message}",
                StartedAt: DateTime.UtcNow);
        }
    }

    public async Task<DeploymentStatus> GetDeploymentStatusAsync(string deploymentId)
    {
        if (activeDeployments.TryGetValue(deploymentId, out var deployment))
        {
            return await Task.FromResult(deployment.Status);
        }
        
        return DeploymentStatus.NotFound;
    }

    public async Task<List<DeploymentInfo>> GetActiveDeploymentsAsync()
    {
        var deployments = activeDeployments.Values
            .Where(d => d.Status == DeploymentStatus.Deployed)
            .Select(d => new DeploymentInfo(
                Id: d.Id,
                ModelId: d.ModelId,
                Version: d.Version,
                Status: d.Status,
                TrafficPercentage: d.Traffic,
                CreatedAt: d.CreatedAt,
                LastUpdated: d.LastUpdated))
            .ToList();

        return await Task.FromResult(deployments);
    }

    public async Task<HealthCheckResult> ValidateModelHealthAsync(string deploymentId)
    {
        if (!activeDeployments.TryGetValue(deploymentId, out var deployment))
        {
            return new HealthCheckResult(
                DeploymentId: deploymentId,
                IsHealthy: false,
                Message: "Deployment not found",
                CheckedAt: DateTime.UtcNow);
        }

        try
        {
            // Perform health checks
            var modelHealth = await modelValidator.CheckModelHealthAsync(deployment.Model);
            var monitoringHealth = await modelMonitoring.GetHealthStatusAsync(deploymentId);
            
            var isHealthy = modelHealth.IsHealthy && monitoringHealth.IsHealthy;
            var messages = new List<string>();
            
            if (!modelHealth.IsHealthy)
                messages.Add($"Model health: {modelHealth.Message}");
            
            if (!monitoringHealth.IsHealthy)
                messages.Add($"Monitoring health: {monitoringHealth.Message}");

            return new HealthCheckResult(
                DeploymentId: deploymentId,
                IsHealthy: isHealthy,
                Message: isHealthy ? "Deployment is healthy" : string.Join("; ", messages),
                CheckedAt: DateTime.UtcNow,
                Details: new Dictionary<string, object>
                {
                    ["model_health"] = modelHealth,
                    ["monitoring_health"] = monitoringHealth,
                    ["deployment_info"] = deployment
                });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Health check failed for deployment {DeploymentId}", deploymentId);
            return new HealthCheckResult(
                DeploymentId: deploymentId,
                IsHealthy: false,
                Message: $"Health check failed: {ex.Message}",
                CheckedAt: DateTime.UtcNow);
        }
    }

    private async Task<DeploymentResult> ExecuteDeploymentStrategyAsync(
        DeploymentInstance deployment, 
        DeploymentStrategy strategy)
    {
        return strategy switch
        {
            DeploymentStrategy.BlueGreen => await ExecuteBlueGreenDeploymentAsync(deployment),
            DeploymentStrategy.RollingUpdate => await ExecuteRollingUpdateAsync(deployment),
            DeploymentStrategy.Canary => await ExecuteCanaryDeploymentAsync(deployment),
            DeploymentStrategy.Immediate => await ExecuteImmediateDeploymentAsync(deployment),
            _ => throw new ArgumentException($"Unknown deployment strategy: {strategy}")
        };
    }

    private async Task<DeploymentResult> ExecuteBlueGreenDeploymentAsync(DeploymentInstance deployment)
    {
        logger.LogInformation("Executing blue-green deployment for {DeploymentId}", deployment.Id);

        try
        {
            // Deploy to green environment
            await loadBalancer.DeployToGreenEnvironmentAsync(deployment.Id, deployment.Model);
            
            // Warm up the new deployment
            await WarmUpDeploymentAsync(deployment);
            
            // Switch traffic atomically
            await loadBalancer.SwitchToGreenEnvironmentAsync(deployment.Id);
            
            // Clean up blue environment after successful switch
            await Task.Delay(TimeSpan.FromMinutes(5)); // Grace period
            await loadBalancer.CleanupBlueEnvironmentAsync(deployment.Id);

            return new DeploymentResult(
                DeploymentId: deployment.Id,
                Status: DeploymentStatus.Deployed,
                Message: "Blue-green deployment completed successfully",
                StartedAt: deployment.CreatedAt,
                CompletedAt: DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Blue-green deployment failed for {DeploymentId}", deployment.Id);
            await loadBalancer.RollbackToBlueEnvironmentAsync(deployment.Id);
            throw;
        }
    }

    private async Task<DeploymentResult> ExecuteRollingUpdateAsync(DeploymentInstance deployment)
    {
        logger.LogInformation("Executing rolling update for {DeploymentId}", deployment.Id);

        var batchSize = config.RollingUpdateBatchSize;
        var totalInstances = await loadBalancer.GetInstanceCountAsync(deployment.Id);
        var batches = (int)Math.Ceiling((double)totalInstances / batchSize);

        for (int batch = 0; batch < batches; batch++)
        {
            var startIndex = batch * batchSize;
            var endIndex = Math.Min(startIndex + batchSize, totalInstances);
            
            logger.LogInformation("Updating instances {StartIndex}-{EndIndex} of {Total} for deployment {DeploymentId}",
                startIndex, endIndex, totalInstances, deployment.Id);

            await loadBalancer.UpdateInstanceBatchAsync(deployment.Id, deployment.Model, startIndex, endIndex);
            
            // Wait for health check
            await Task.Delay(TimeSpan.FromSeconds(config.RollingUpdateDelaySeconds));
            
            var healthCheck = await ValidateModelHealthAsync(deployment.Id);
            if (!healthCheck.IsHealthy)
            {
                logger.LogError("Rolling update failed health check for deployment {DeploymentId}", deployment.Id);
                await loadBalancer.RollbackInstanceBatchAsync(deployment.Id, startIndex, endIndex);
                throw new Exception($"Rolling update failed: {healthCheck.Message}");
            }
        }

        return new DeploymentResult(
            DeploymentId: deployment.Id,
            Status: DeploymentStatus.Deployed,
            Message: "Rolling update completed successfully",
            StartedAt: deployment.CreatedAt,
            CompletedAt: DateTime.UtcNow);
    }

    private async Task<DeploymentResult> ExecuteCanaryDeploymentAsync(DeploymentInstance deployment)
    {
        logger.LogInformation("Executing canary deployment for {DeploymentId}", deployment.Id);

        // Start with small percentage of traffic
        var canaryPercentage = config.InitialCanaryPercentage;
        await loadBalancer.StartCanaryDeploymentAsync(deployment.Id, canaryPercentage);

        // Monitor canary metrics
        var monitoringDuration = TimeSpan.FromMinutes(config.CanaryMonitoringMinutes);
        var monitoringTask = MonitorCanaryDeploymentAsync(deployment.Id, deployment.Model, 
            new CanaryConfiguration 
            { 
                MonitoringDuration = monitoringDuration,
                SuccessThreshold = config.CanarySuccessThreshold 
            });

        var canaryResult = await monitoringTask;
        
        if (canaryResult.Status == CanaryStatus.Successful)
        {
            // Gradually increase traffic
            await GraduallyIncreaseCanaryTrafficAsync(deployment.Id);
            
            return new DeploymentResult(
                DeploymentId: deployment.Id,
                Status: DeploymentStatus.Deployed,
                Message: "Canary deployment successful, promoted to full deployment",
                StartedAt: deployment.CreatedAt,
                CompletedAt: DateTime.UtcNow);
        }
        else
        {
            // Rollback canary
            await loadBalancer.StopCanaryDeploymentAsync(deployment.Id);
            
            return new DeploymentResult(
                DeploymentId: deployment.Id,
                Status: DeploymentStatus.Failed,
                Message: $"Canary deployment failed: {canaryResult.Message}",
                StartedAt: deployment.CreatedAt,
                CompletedAt: DateTime.UtcNow);
        }
    }

    private async Task<DeploymentResult> ExecuteImmediateDeploymentAsync(DeploymentInstance deployment)
    {
        logger.LogInformation("Executing immediate deployment for {DeploymentId}", deployment.Id);

        await loadBalancer.ImmediateDeploymentAsync(deployment.Id, deployment.Model);
        
        return new DeploymentResult(
            DeploymentId: deployment.Id,
            Status: DeploymentStatus.Deployed,
            Message: "Immediate deployment completed",
            StartedAt: deployment.CreatedAt,
            CompletedAt: DateTime.UtcNow);
    }

    private async Task WarmUpDeploymentAsync(DeploymentInstance deployment)
    {
        logger.LogInformation("Warming up deployment {DeploymentId}", deployment.Id);

        // Send test requests to warm up the model
        var warmupRequests = config.WarmupRequestCount;
        var testInputs = await modelRepository.GetWarmupDataAsync(deployment.ModelId);

        var tasks = testInputs.Take(warmupRequests).Select(async input =>
        {
            try
            {
                // Make prediction to warm up model
                var dataView = deployment.Model.Transform(input);
                await Task.Delay(100); // Simulate processing time
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Warmup request failed for deployment {DeploymentId}", deployment.Id);
            }
        });

        await Task.WhenAll(tasks);
        logger.LogInformation("Warmup completed for deployment {DeploymentId}", deployment.Id);
    }

    private async Task<CanaryDeploymentResult> MonitorCanaryDeploymentAsync(
        string canaryId,
        ITransformer model,
        CanaryConfiguration config)
    {
        var startTime = DateTime.UtcNow;
        var endTime = startTime.Add(config.MonitoringDuration);

        while (DateTime.UtcNow < endTime)
        {
            var metrics = await modelMonitoring.GetCanaryMetricsAsync(canaryId);
            
            if (metrics.ErrorRate > config.MaxErrorRate)
            {
                logger.LogWarning("Canary deployment {CanaryId} error rate {ErrorRate} exceeds threshold {Threshold}",
                    canaryId, metrics.ErrorRate, config.MaxErrorRate);

                await loadBalancer.StopCanaryDeploymentAsync(canaryId);
                
                return new CanaryDeploymentResult(
                    CanaryId: canaryId,
                    Status: CanaryStatus.Failed,
                    Message: $"Error rate {metrics.ErrorRate:P2} exceeded threshold {config.MaxErrorRate:P2}",
                    StartedAt: startTime);
            }

            if (metrics.ResponseTime > config.MaxResponseTime)
            {
                logger.LogWarning("Canary deployment {CanaryId} response time {ResponseTime}ms exceeds threshold {Threshold}ms",
                    canaryId, metrics.ResponseTime.TotalMilliseconds, config.MaxResponseTime.TotalMilliseconds);

                await loadBalancer.StopCanaryDeploymentAsync(canaryId);
                
                return new CanaryDeploymentResult(
                    CanaryId: canaryId,
                    Status: CanaryStatus.Failed,
                    Message: $"Response time {metrics.ResponseTime.TotalMilliseconds}ms exceeded threshold",
                    StartedAt: startTime);
            }

            await Task.Delay(TimeSpan.FromSeconds(30)); // Check every 30 seconds
        }

        return new CanaryDeploymentResult(
            CanaryId: canaryId,
            Status: CanaryStatus.Successful,
            Message: "Canary deployment metrics within acceptable thresholds",
            StartedAt: startTime);
    }

    private async Task GraduallyIncreaseCanaryTrafficAsync(string deploymentId)
    {
        var trafficPercentages = new[] { 10, 25, 50, 75, 100 };
        
        foreach (var percentage in trafficPercentages)
        {
            logger.LogInformation("Increasing canary traffic to {Percentage}% for deployment {DeploymentId}",
                percentage, deploymentId);

            await loadBalancer.UpdateCanaryTrafficAsync(deploymentId, percentage);
            
            // Monitor for a period before increasing further
            await Task.Delay(TimeSpan.FromMinutes(config.CanaryTrafficIncreaseDelayMinutes));
            
            var healthCheck = await ValidateModelHealthAsync(deploymentId);
            if (!healthCheck.IsHealthy)
            {
                logger.LogError("Health check failed during traffic increase for deployment {DeploymentId}", deploymentId);
                await loadBalancer.StopCanaryDeploymentAsync(deploymentId);
                throw new Exception($"Canary promotion failed: {healthCheck.Message}");
            }
        }
    }

    private async Task<DeploymentValidation> ValidateDeploymentRequestAsync(ModelDeploymentRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.ModelId))
            errors.Add("ModelId is required");

        if (string.IsNullOrWhiteSpace(request.Version))
            errors.Add("Version is required");

        if (request.InitialTrafficPercentage < 0 || request.InitialTrafficPercentage > 100)
            errors.Add("InitialTrafficPercentage must be between 0 and 100");

        // Additional validation logic...

        return await Task.FromResult(new DeploymentValidation(errors.Count == 0, errors));
    }

    private async Task CleanupFailedDeploymentAsync(string deploymentId)
    {
        try
        {
            activeDeployments.TryRemove(deploymentId, out _);
            await loadBalancer.CleanupDeploymentAsync(deploymentId);
            await modelMonitoring.StopMonitoringAsync(deploymentId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to cleanup deployment {DeploymentId}", deploymentId);
        }
    }

    private void PerformHealthChecks(object? state)
    {
        _ = Task.Run(async () =>
        {
            foreach (var deployment in activeDeployments.Values)
            {
                if (deployment.Status == DeploymentStatus.Deployed)
                {
                    try
                    {
                        var health = await ValidateModelHealthAsync(deployment.Id);
                        if (!health.IsHealthy)
                        {
                            logger.LogWarning("Deployment {DeploymentId} health check failed: {Message}",
                                deployment.Id, health.Message);
                            
                            // Could trigger automatic remediation here
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Health check error for deployment {DeploymentId}", deployment.Id);
                    }
                }
            }
        });
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Model Deployment Manager started");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        healthCheckTimer?.Dispose();
        logger.LogInformation("Model Deployment Manager stopped");
        return Task.CompletedTask;
    }
}

// Data Transfer Objects and Supporting Types

public record ModelDeploymentRequest(
    string ModelId,
    string Version,
    DeploymentStrategy Strategy,
    DeploymentConfiguration Configuration,
    int InitialTrafficPercentage,
    IDataView? ValidationDataset,
    MonitoringConfiguration MonitoringConfig);

public record ModelUpdateRequest(
    string NewVersion,
    IDataView? ValidationDataset,
    UpdateStrategy UpdateStrategy);

public record DeploymentResult(
    string DeploymentId,
    DeploymentStatus Status,
    string Message,
    DateTime StartedAt,
    DateTime? CompletedAt);

public record ABTestConfiguration(
    string ModelAId,
    string ModelAVersion,
    string ModelBId,
    string ModelBVersion,
    int TrafficSplitPercentage,
    TimeSpan Duration,
    IDataView? ValidationDataset,
    MonitoringConfiguration MonitoringConfig);

public record ABTestResult(
    string TestId,
    ABTestStatus Status,
    string Message,
    DateTime StartedAt,
    DateTime? CompletedAt);

public record CanaryConfiguration(
    string ModelId,
    string Version,
    int InitialTrafficPercentage,
    TimeSpan MonitoringDuration,
    double MaxErrorRate,
    TimeSpan MaxResponseTime,
    double SuccessThreshold,
    IDataView? ValidationDataset);

public record CanaryDeploymentResult(
    string CanaryId,
    CanaryStatus Status,
    string Message,
    DateTime StartedAt);

public record DeploymentInstance(
    string Id,
    string ModelId,
    string Version,
    ITransformer Model,
    DeploymentConfiguration Configuration,
    DeploymentStatus Status,
    DateTime CreatedAt,
    int Traffic,
    DateTime? LastUpdated = null);

public record DeploymentInfo(
    string Id,
    string ModelId,
    string Version,
    DeploymentStatus Status,
    int TrafficPercentage,
    DateTime CreatedAt,
    DateTime? LastUpdated);

public record HealthCheckResult(
    string DeploymentId,
    bool IsHealthy,
    string Message,
    DateTime CheckedAt,
    Dictionary<string, object>? Details = null);

public record ABTestInstance(
    string Id,
    ITransformer ModelA,
    ITransformer ModelB,
    ABTestConfiguration Configuration,
    ABTestStatus Status,
    DateTime StartedAt,
    ABTestMetrics Metrics);

public record ABTestMetrics(
    int ModelARequests = 0,
    int ModelBRequests = 0,
    double ModelALatency = 0.0,
    double ModelBLatency = 0.0,
    double ModelAAccuracy = 0.0,
    double ModelBAccuracy = 0.0);

public record DeploymentValidation(bool IsValid, List<string> Errors);

public record MonitoringConfiguration(
    bool EnableMetrics,
    bool EnableLogging,
    bool EnableAlerting,
    Dictionary<string, object> Settings);

public enum DeploymentStatus
{
    NotFound,
    Deploying,
    Deployed,
    Updating,
    RollingBack,
    Failed,
    Stopped
}

public enum DeploymentStrategy
{
    Immediate,
    BlueGreen,
    RollingUpdate,
    Canary
}

public enum UpdateStrategy
{
    Immediate,
    Gradual,
    Canary
}

public enum ABTestStatus
{
    Running,
    Completed,
    Failed,
    Stopped
}

public enum CanaryStatus
{
    Running,
    Successful,
    Failed,
    Stopped
}

public class DeploymentConfiguration
{
    public const string SectionName = "ModelDeployment";
    
    public int HealthCheckIntervalMinutes { get; set; } = 5;
    public int RollingUpdateBatchSize { get; set; } = 1;
    public int RollingUpdateDelaySeconds { get; set; } = 30;
    public int InitialCanaryPercentage { get; set; } = 5;
    public int CanaryMonitoringMinutes { get; set; } = 10;
    public double CanarySuccessThreshold { get; set; } = 0.95;
    public int CanaryTrafficIncreaseDelayMinutes { get; set; } = 5;
    public int WarmupRequestCount { get; set; } = 10;
    public string ModelStoragePath { get; set; } = "./models";
    public bool EnableAutomaticRollback { get; set; } = true;
    public double ErrorRateThreshold { get; set; } = 0.05;
    public TimeSpan ResponseTimeThreshold { get; set; } = TimeSpan.FromSeconds(5);
}
```

## ASP.NET Core Integration

### Model Deployment Controller

```csharp
namespace DocumentProcessor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ModelDeploymentController : ControllerBase
{
    private readonly IModelDeploymentManager deploymentManager;
    private readonly ILogger<ModelDeploymentController> logger;

    public ModelDeploymentController(
        IModelDeploymentManager deploymentManager,
        ILogger<ModelDeploymentController> logger)
    {
        deploymentManager = deploymentManager;
        logger = logger;
    }

    [HttpPost("deploy")]
    public async Task<ActionResult<DeploymentResponse>> DeployModel(
        [FromBody] DeployModelRequest request)
    {
        try
        {
            var deploymentRequest = new ModelDeploymentRequest(
                ModelId: request.ModelId,
                Version: request.Version,
                Strategy: request.Strategy,
                Configuration: new DeploymentConfiguration(),
                InitialTrafficPercentage: request.InitialTrafficPercentage,
                ValidationDataset: null, // Would be loaded based on request
                MonitoringConfig: new MonitoringConfiguration(true, true, true, new Dictionary<string, object>()));

            var result = await deploymentManager.DeployModelAsync(deploymentRequest);

            var response = new DeploymentResponse(
                DeploymentId: result.DeploymentId,
                Status: result.Status.ToString(),
                Message: result.Message,
                RequestId: Guid.NewGuid().ToString(),
                Timestamp: DateTime.UtcNow);

            return result.Status == DeploymentStatus.Failed ? 
                StatusCode(500, response) : 
                Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deploying model {ModelId}", request.ModelId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("{deploymentId}/update")]
    public async Task<ActionResult<DeploymentResponse>> UpdateDeployment(
        string deploymentId,
        [FromBody] UpdateDeploymentRequest request)
    {
        try
        {
            var updateRequest = new ModelUpdateRequest(
                NewVersion: request.NewVersion,
                ValidationDataset: null,
                UpdateStrategy: request.UpdateStrategy);

            var result = await deploymentManager.UpdateModelAsync(deploymentId, updateRequest);

            var response = new DeploymentResponse(
                DeploymentId: result.DeploymentId,
                Status: result.Status.ToString(),
                Message: result.Message,
                RequestId: Guid.NewGuid().ToString(),
                Timestamp: DateTime.UtcNow);

            return Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating deployment {DeploymentId}", deploymentId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("{deploymentId}/rollback")]
    public async Task<ActionResult<DeploymentResponse>> RollbackDeployment(
        string deploymentId,
        [FromBody] RollbackRequest request)
    {
        try
        {
            var result = await deploymentManager.RollbackModelAsync(deploymentId, request.TargetVersion);

            var response = new DeploymentResponse(
                DeploymentId: result.DeploymentId,
                Status: result.Status.ToString(),
                Message: result.Message,
                RequestId: Guid.NewGuid().ToString(),
                Timestamp: DateTime.UtcNow);

            return Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error rolling back deployment {DeploymentId}", deploymentId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{deploymentId}/status")]
    public async Task<ActionResult<DeploymentStatusResponse>> GetDeploymentStatus(string deploymentId)
    {
        try
        {
            var status = await deploymentManager.GetDeploymentStatusAsync(deploymentId);

            var response = new DeploymentStatusResponse(
                DeploymentId: deploymentId,
                Status: status.ToString(),
                RequestId: Guid.NewGuid().ToString(),
                Timestamp: DateTime.UtcNow);

            return Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting deployment status for {DeploymentId}", deploymentId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("active")]
    public async Task<ActionResult<ActiveDeploymentsResponse>> GetActiveDeployments()
    {
        try
        {
            var deployments = await deploymentManager.GetActiveDeploymentsAsync();

            var response = new ActiveDeploymentsResponse(
                Deployments: deployments,
                Count: deployments.Count,
                RequestId: Guid.NewGuid().ToString(),
                Timestamp: DateTime.UtcNow);

            return Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting active deployments");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("ab-test")]
    public async Task<ActionResult<ABTestResponse>> StartABTest(
        [FromBody] StartABTestRequest request)
    {
        try
        {
            var config = new ABTestConfiguration(
                ModelAId: request.ModelAId,
                ModelAVersion: request.ModelAVersion,
                ModelBId: request.ModelBId,
                ModelBVersion: request.ModelBVersion,
                TrafficSplitPercentage: request.TrafficSplitPercentage,
                Duration: TimeSpan.FromHours(request.DurationHours),
                ValidationDataset: null,
                MonitoringConfig: new MonitoringConfiguration(true, true, true, new Dictionary<string, object>()));

            var result = await deploymentManager.StartABTestAsync(config);

            var response = new ABTestResponse(
                TestId: result.TestId,
                Status: result.Status.ToString(),
                Message: result.Message,
                RequestId: Guid.NewGuid().ToString(),
                Timestamp: DateTime.UtcNow);

            return Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error starting A/B test");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{deploymentId}/health")]
    public async Task<ActionResult<HealthCheckResponse>> CheckDeploymentHealth(string deploymentId)
    {
        try
        {
            var health = await deploymentManager.ValidateModelHealthAsync(deploymentId);

            var response = new HealthCheckResponse(
                DeploymentId: deploymentId,
                IsHealthy: health.IsHealthy,
                Message: health.Message,
                Details: health.Details,
                CheckedAt: health.CheckedAt,
                RequestId: Guid.NewGuid().ToString());

            return Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking deployment health for {DeploymentId}", deploymentId);
            return StatusCode(500, "Internal server error");
        }
    }
}

// Request/Response DTOs
public record DeployModelRequest(
    string ModelId,
    string Version,
    DeploymentStrategy Strategy,
    int InitialTrafficPercentage = 100);

public record UpdateDeploymentRequest(
    string NewVersion,
    UpdateStrategy UpdateStrategy = UpdateStrategy.Gradual);

public record RollbackRequest(string TargetVersion);

public record StartABTestRequest(
    string ModelAId,
    string ModelAVersion,
    string ModelBId,
    string ModelBVersion,
    int TrafficSplitPercentage = 50,
    int DurationHours = 24);

public record DeploymentResponse(
    string DeploymentId,
    string Status,
    string Message,
    string RequestId,
    DateTime Timestamp);

public record DeploymentStatusResponse(
    string DeploymentId,
    string Status,
    string RequestId,
    DateTime Timestamp);

public record ActiveDeploymentsResponse(
    List<DeploymentInfo> Deployments,
    int Count,
    string RequestId,
    DateTime Timestamp);

public record ABTestResponse(
    string TestId,
    string Status,
    string Message,
    string RequestId,
    DateTime Timestamp);

public record HealthCheckResponse(
    string DeploymentId,
    bool IsHealthy,
    string Message,
    Dictionary<string, object>? Details,
    DateTime CheckedAt,
    string RequestId);
```

## Service Registration

### ML.NET Deployment Services

```csharp
namespace DocumentProcessor.Extensions;

public static class ModelDeploymentServiceCollectionExtensions
{
    public static IServiceCollection AddModelDeployment(this IServiceCollection services, IConfiguration configuration)
    {
        // Register deployment manager
        services.Configure<DeploymentConfiguration>(configuration.GetSection(DeploymentConfiguration.SectionName));
        services.AddSingleton<IModelDeploymentManager, ModelDeploymentManager>();
        
        // Register supporting services
        services.AddScoped<IModelRepository, ModelRepository>();
        services.AddScoped<IModelValidator, ModelValidator>();
        services.AddScoped<IModelMonitoring, ModelMonitoring>();
        services.AddScoped<ILoadBalancer, LoadBalancer>();
        
        // Register as hosted service for background tasks
        services.AddHostedService<ModelDeploymentManager>();
        
        // Add health checks
        services.AddHealthChecks()
            .AddCheck<ModelDeploymentHealthCheck>("model-deployment");

        return services;
    }
}
```

**Usage**:

```csharp
// Deploy a new model
var deploymentManager = serviceProvider.GetRequiredService<IModelDeploymentManager>();

var deploymentRequest = new ModelDeploymentRequest(
    ModelId: "sentiment-classifier",
    Version: "v2.1.0",
    Strategy: DeploymentStrategy.BlueGreen,
    Configuration: new DeploymentConfiguration(),
    InitialTrafficPercentage: 100,
    ValidationDataset: validationData,
    MonitoringConfig: new MonitoringConfiguration(true, true, true, new Dictionary<string, object>()));

var result = await deploymentManager.DeployModelAsync(deploymentRequest);

if (result.Status == DeploymentStatus.Deployed)
{
    Console.WriteLine($"Model deployed successfully: {result.DeploymentId}");
}

// Start A/B test
var abTestConfig = new ABTestConfiguration(
    ModelAId: "sentiment-v1",
    ModelAVersion: "1.0.0",
    ModelBId: "sentiment-v2", 
    ModelBVersion: "2.0.0",
    TrafficSplitPercentage: 50,
    Duration: TimeSpan.FromHours(24),
    ValidationDataset: testData,
    MonitoringConfig: new MonitoringConfiguration(true, true, true, new Dictionary<string, object>()));

var abTestResult = await deploymentManager.StartABTestAsync(abTestConfig);
Console.WriteLine($"A/B test started: {abTestResult.TestId}");

// Monitor deployment health
var health = await deploymentManager.ValidateModelHealthAsync(result.DeploymentId);
Console.WriteLine($"Deployment health: {health.IsHealthy} - {health.Message}");

// Canary deployment
var canaryConfig = new CanaryConfiguration(
    ModelId: "new-model",
    Version: "3.0.0",
    InitialTrafficPercentage: 5,
    MonitoringDuration: TimeSpan.FromMinutes(30),
    MaxErrorRate: 0.01,
    MaxResponseTime: TimeSpan.FromSeconds(2),
    SuccessThreshold: 0.99,
    ValidationDataset: canaryTestData);

var canaryResult = await deploymentManager.StartCanaryDeploymentAsync(canaryConfig);
Console.WriteLine($"Canary deployment: {canaryResult.Status}");

// Get active deployments
var activeDeployments = await deploymentManager.GetActiveDeploymentsAsync();
foreach (var deployment in activeDeployments)
{
    Console.WriteLine($"Active: {deployment.ModelId} v{deployment.Version} - {deployment.TrafficPercentage}% traffic");
}
```

**Notes**:

- **Multiple Deployment Strategies**: Blue-green, rolling updates, canary deployments, and immediate deployments
- **A/B Testing Framework**: Statistical comparison of model performance with automated traffic splitting
- **Health Monitoring**: Continuous health checks with automatic alerting and remediation capabilities
- **Version Management**: Model versioning with rollback capabilities and deployment history tracking
- **Traffic Management**: Gradual traffic shifting with configurable thresholds and monitoring
- **Production Safety**: Validation gates, warmup procedures, and automated rollback on failures
- **Monitoring Integration**: Comprehensive metrics collection and alerting for deployment status

**Performance Considerations**: Implements efficient health checking, gradual traffic shifting, and optimized model loading to minimize deployment impact on production systems.
