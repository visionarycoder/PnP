# .NET Aspire Deployment Strategies

**Description**: Core deployment strategy patterns for .NET Aspire applications, covering deployment methodologies, rollout strategies, environment management, and strategic deployment decision frameworks.

**Language/Technology**: C#, .NET Aspire, .NET 9.0, DevOps, Cloud Architecture

**Code**:

## Table of Contents

1. [Deployment Strategy Overview](#deployment-strategy-overview)
2. [Environment Strategy Patterns](#environment-strategy-patterns)
3. [Rollout Strategy Patterns](#rollout-strategy-patterns)
4. [Multi-Environment Management](#multi-environment-management)
5. [Deployment Decision Framework](#deployment-decision-framework)
6. [Strategy Implementation](#strategy-implementation)

## Deployment Strategy Overview

### Strategy Selection Framework

```csharp
namespace DocumentProcessor.Aspire.Strategy;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;

public interface IDeploymentStrategy
{
    string StrategyName { get; }
    DeploymentComplexity Complexity { get; }
    TimeSpan TypicalDeploymentTime { get; }
    RiskLevel RiskProfile { get; }
    Task<DeploymentPlan> CreateDeploymentPlanAsync(DeploymentContext context);
    Task<bool> ValidatePrerequisitesAsync(DeploymentContext context);
    Task<DeploymentResult> ExecuteAsync(DeploymentPlan plan, CancellationToken cancellationToken = default);
}

public enum DeploymentComplexity { Simple, Moderate, Complex, Advanced }
public enum RiskLevel { Low, Medium, High, Critical }

public record DeploymentContext
{
    public string Environment { get; init; } = string.Empty;
    public string ApplicationVersion { get; init; } = string.Empty;
    public Dictionary<string, string> Configuration { get; init; } = new();
    public List<ServiceComponent> Components { get; init; } = new();
    public InfrastructureRequirements Infrastructure { get; init; } = new();
    public ComplianceRequirements Compliance { get; init; } = new();
}

public record ServiceComponent
{
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty; // API, Worker, Database, Cache
    public string CurrentVersion { get; init; } = string.Empty;
    public string TargetVersion { get; init; } = string.Empty;
    public List<string> Dependencies { get; init; } = new();
    public ResourceRequirements Resources { get; init; } = new();
    public bool SupportsZeroDowntime { get; init; }
}

public record InfrastructureRequirements
{
    public CloudProvider Provider { get; init; }
    public string Region { get; init; } = string.Empty;
    public bool RequiresMultiRegion { get; init; }
    public NetworkRequirements Network { get; init; } = new();
    public SecurityRequirements Security { get; init; } = new();
    public List<string> ExternalDependencies { get; init; } = new();
}

public record ComplianceRequirements
{
    public List<string> Standards { get; init; } = new(); // SOC2, HIPAA, GDPR, etc.
    public bool RequiresApprovalGates { get; init; }
    public bool RequiresChangeControl { get; init; }
    public TimeSpan MinimumReviewPeriod { get; init; }
}
```

### Strategy Registry

```csharp
namespace DocumentProcessor.Aspire.Strategy.Registry;

public class DeploymentStrategyRegistry
{
    private readonly Dictionary<string, IDeploymentStrategy> strategies = new();
    private readonly ILogger<DeploymentStrategyRegistry> logger;

    public DeploymentStrategyRegistry(
        IEnumerable<IDeploymentStrategy> availableStrategies,
        ILogger<DeploymentStrategyRegistry> logger)
    {
        this.logger = logger;
        
        foreach (var strategy in availableStrategies)
        {
            strategies[strategy.StrategyName] = strategy;
        }
    }

    public IDeploymentStrategy GetStrategy(string name)
    {
        if (strategies.TryGetValue(name, out var strategy))
        {
            return strategy;
        }
        
        throw new InvalidOperationException($"Deployment strategy '{name}' not found");
    }

    public IDeploymentStrategy RecommendStrategy(DeploymentContext context)
    {
        var candidates = strategies.Values
            .Where(s => s.ValidatePrerequisitesAsync(context).Result)
            .ToList();

        if (!candidates.Any())
        {
            throw new InvalidOperationException("No suitable deployment strategy found for the given context");
        }

        // Strategy selection logic based on context
        return context switch
        {
            { Environment: "Production" } when context.Compliance.RequiresApprovalGates => 
                GetStrategy("BlueGreenWithApproval"),
            { Environment: "Production" } when context.Components.Any(c => !c.SupportsZeroDowntime) => 
                GetStrategy("MaintenanceWindow"),
            { Environment: "Production" } => 
                GetStrategy("RollingDeployment"),
            { Environment: "Staging" } => 
                GetStrategy("BlueGreen"),
            { Environment: "Development" } => 
                GetStrategy("DirectReplacement"),
            _ => candidates.OrderBy(s => s.RiskProfile).First()
        };
    }

    public List<IDeploymentStrategy> GetAvailableStrategies() => 
        strategies.Values.ToList();
}
```

## Environment Strategy Patterns

### Multi-Environment Configuration

```csharp
namespace DocumentProcessor.Aspire.Strategy.Environments;

public class EnvironmentManager
{
    private readonly Dictionary<string, EnvironmentConfiguration> environments;
    private readonly ILogger<EnvironmentManager> logger;

    public EnvironmentManager(IConfiguration configuration, ILogger<EnvironmentManager> logger)
    {
        this.logger = logger;
        this.environments = LoadEnvironmentConfigurations(configuration);
    }

    private Dictionary<string, EnvironmentConfiguration> LoadEnvironmentConfigurations(IConfiguration config)
    {
        return new Dictionary<string, EnvironmentConfiguration>
        {
            ["Development"] = new()
            {
                Name = "Development",
                Purpose = "Feature development and initial testing",
                ApprovalRequired = false,
                AutomatedTesting = TestingLevel.Unit,
                DeploymentFrequency = DeploymentFrequency.OnDemand,
                RollbackStrategy = RollbackStrategy.Immediate,
                InfrastructureType = InfrastructureType.Shared,
                DataStrategy = DataStrategy.Synthetic,
                SecurityLevel = SecurityLevel.Development
            },
            
            ["Testing"] = new()
            {
                Name = "Testing",
                Purpose = "Integration and system testing",
                ApprovalRequired = false,
                AutomatedTesting = TestingLevel.Integration,
                DeploymentFrequency = DeploymentFrequency.Continuous,
                RollbackStrategy = RollbackStrategy.Immediate,
                InfrastructureType = InfrastructureType.Shared,
                DataStrategy = DataStrategy.Anonymized,
                SecurityLevel = SecurityLevel.Testing
            },
            
            ["Staging"] = new()
            {
                Name = "Staging",
                Purpose = "Production-like testing and validation",
                ApprovalRequired = true,
                AutomatedTesting = TestingLevel.EndToEnd,
                DeploymentFrequency = DeploymentFrequency.Scheduled,
                RollbackStrategy = RollbackStrategy.Planned,
                InfrastructureType = InfrastructureType.Production,
                DataStrategy = DataStrategy.ProductionSubset,
                SecurityLevel = SecurityLevel.Production
            },
            
            ["Production"] = new()
            {
                Name = "Production",
                Purpose = "Live customer-facing environment",
                ApprovalRequired = true,
                AutomatedTesting = TestingLevel.Smoke,
                DeploymentFrequency = DeploymentFrequency.Controlled,
                RollbackStrategy = RollbackStrategy.ChangeControl,
                InfrastructureType = InfrastructureType.Production,
                DataStrategy = DataStrategy.Production,
                SecurityLevel = SecurityLevel.Production
            }
        };
    }

    public EnvironmentConfiguration GetEnvironment(string name)
    {
        if (environments.TryGetValue(name, out var config))
        {
            return config;
        }
        
        throw new ArgumentException($"Environment '{name}' not configured", nameof(name));
    }

    public async Task<bool> ValidateEnvironmentReadinessAsync(string environmentName, DeploymentContext context)
    {
        var environment = GetEnvironment(environmentName);
        
        // Validate environment prerequisites
        var checks = new List<Task<bool>>
        {
            ValidateInfrastructureAsync(environment, context),
            ValidateSecurityAsync(environment, context),
            ValidateDataAsync(environment, context),
            ValidateNetworkAsync(environment, context)
        };

        var results = await Task.WhenAll(checks);
        return results.All(result => result);
    }

    private async Task<bool> ValidateInfrastructureAsync(EnvironmentConfiguration env, DeploymentContext context)
    {
        // Infrastructure validation logic
        logger.LogInformation("Validating infrastructure for environment {Environment}", env.Name);
        
        // Example validations:
        // - Resource group exists
        // - Required services are running
        // - Network connectivity is available
        // - Storage accounts are accessible
        
        await Task.Delay(100); // Simulate async validation
        return true;
    }

    private async Task<bool> ValidateSecurityAsync(EnvironmentConfiguration env, DeploymentContext context)
    {
        logger.LogInformation("Validating security configuration for environment {Environment}", env.Name);
        
        // Security validation logic:
        // - Certificates are valid and not expired
        // - Key Vault is accessible
        // - Managed identities are configured
        // - Network security groups are properly configured
        
        await Task.Delay(100);
        return true;
    }

    private async Task<bool> ValidateDataAsync(EnvironmentConfiguration env, DeploymentContext context)
    {
        logger.LogInformation("Validating data configuration for environment {Environment}", env.Name);
        
        // Data validation logic:
        // - Database connections are working
        // - Required data is available
        // - Backup systems are functioning
        // - Data migration scripts are ready
        
        await Task.Delay(100);
        return true;
    }

    private async Task<bool> ValidateNetworkAsync(EnvironmentConfiguration env, DeploymentContext context)
    {
        logger.LogInformation("Validating network configuration for environment {Environment}", env.Name);
        
        // Network validation logic:
        // - Load balancers are configured
        // - DNS records are correct
        // - Firewall rules are in place
        // - Service endpoints are reachable
        
        await Task.Delay(100);
        return true;
    }
}

public record EnvironmentConfiguration
{
    public string Name { get; init; } = string.Empty;
    public string Purpose { get; init; } = string.Empty;
    public bool ApprovalRequired { get; init; }
    public TestingLevel AutomatedTesting { get; init; }
    public DeploymentFrequency DeploymentFrequency { get; init; }
    public RollbackStrategy RollbackStrategy { get; init; }
    public InfrastructureType InfrastructureType { get; init; }
    public DataStrategy DataStrategy { get; init; }
    public SecurityLevel SecurityLevel { get; init; }
}

public enum TestingLevel { None, Unit, Integration, EndToEnd, Smoke }
public enum DeploymentFrequency { OnDemand, Continuous, Scheduled, Controlled }
public enum RollbackStrategy { Immediate, Planned, ChangeControl }
public enum InfrastructureType { Shared, Dedicated, Production }
public enum DataStrategy { Synthetic, Anonymized, ProductionSubset, Production }
public enum SecurityLevel { Development, Testing, Production }
```

## Rollout Strategy Patterns

### Blue-Green Deployment Strategy

```csharp
namespace DocumentProcessor.Aspire.Strategy.Rollout;

public class BlueGreenDeploymentStrategy : IDeploymentStrategy
{
    public string StrategyName => "BlueGreen";
    public DeploymentComplexity Complexity => DeploymentComplexity.Moderate;
    public TimeSpan TypicalDeploymentTime => TimeSpan.FromMinutes(15);
    public RiskLevel RiskProfile => RiskLevel.Low;

    private readonly ILogger<BlueGreenDeploymentStrategy> logger;
    private readonly IInfrastructureOrchestrator orchestrator;
    private readonly ITrafficManager trafficManager;

    public BlueGreenDeploymentStrategy(
        ILogger<BlueGreenDeploymentStrategy> logger,
        IInfrastructureOrchestrator orchestrator,
        ITrafficManager trafficManager)
    {
        this.logger = logger;
        this.orchestrator = orchestrator;
        this.trafficManager = trafficManager;
    }

    public async Task<DeploymentPlan> CreateDeploymentPlanAsync(DeploymentContext context)
    {
        var plan = new DeploymentPlan
        {
            StrategyName = StrategyName,
            EstimatedDuration = TypicalDeploymentTime,
            RiskLevel = RiskProfile,
            Steps = new List<DeploymentStep>
            {
                new() {
                    Name = "Prepare Green Environment",
                    Description = "Provision and configure green environment",
                    EstimatedDuration = TimeSpan.FromMinutes(5),
                    Type = DeploymentStepType.Infrastructure
                },
                new() {
                    Name = "Deploy to Green",
                    Description = "Deploy application to green environment",
                    EstimatedDuration = TimeSpan.FromMinutes(3),
                    Type = DeploymentStepType.Application
                },
                new() {
                    Name = "Validate Green",
                    Description = "Run health checks and validation tests",
                    EstimatedDuration = TimeSpan.FromMinutes(5),
                    Type = DeploymentStepType.Validation
                },
                new() {
                    Name = "Switch Traffic",
                    Description = "Route traffic from blue to green",
                    EstimatedDuration = TimeSpan.FromMinutes(1),
                    Type = DeploymentStepType.Traffic
                },
                new() {
                    Name = "Monitor and Validate",
                    Description = "Monitor green environment post-switch",
                    EstimatedDuration = TimeSpan.FromMinutes(1),
                    Type = DeploymentStepType.Monitoring
                }
            }
        };

        // Add rollback step
        plan.RollbackPlan = new DeploymentStep
        {
            Name = "Rollback to Blue",
            Description = "Switch traffic back to blue environment",
            EstimatedDuration = TimeSpan.FromMinutes(1),
            Type = DeploymentStepType.Rollback
        };

        return plan;
    }

    public async Task<bool> ValidatePrerequisitesAsync(DeploymentContext context)
    {
        var validations = new List<Task<bool>>
        {
            ValidateLoadBalancerCapabilityAsync(context),
            ValidateInfrastructureCapacityAsync(context),
            ValidateNetworkConfigurationAsync(context)
        };

        var results = await Task.WhenAll(validations);
        return results.All(result => result);
    }

    public async Task<DeploymentResult> ExecuteAsync(DeploymentPlan plan, CancellationToken cancellationToken = default)
    {
        var result = new DeploymentResult { StrategyUsed = StrategyName };
        
        try
        {
            logger.LogInformation("Starting Blue-Green deployment");

            // Step 1: Prepare Green Environment
            await ExecuteStepWithLogging("Preparing Green Environment", async () =>
            {
                await orchestrator.ProvisionGreenEnvironmentAsync(cancellationToken);
            });

            // Step 2: Deploy to Green
            await ExecuteStepWithLogging("Deploying to Green Environment", async () =>
            {
                await orchestrator.DeployToGreenAsync(plan.Context!, cancellationToken);
            });

            // Step 3: Validate Green
            await ExecuteStepWithLogging("Validating Green Environment", async () =>
            {
                var healthCheck = await orchestrator.ValidateGreenHealthAsync(cancellationToken);
                if (!healthCheck.IsHealthy)
                {
                    throw new DeploymentException($"Green environment health check failed: {healthCheck.Details}");
                }
            });

            // Step 4: Switch Traffic
            await ExecuteStepWithLogging("Switching Traffic to Green", async () =>
            {
                await trafficManager.SwitchToGreenAsync(cancellationToken);
            });

            // Step 5: Monitor
            await ExecuteStepWithLogging("Monitoring Green Environment", async () =>
            {
                await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
                var postSwitchHealth = await orchestrator.ValidateGreenHealthAsync(cancellationToken);
                if (!postSwitchHealth.IsHealthy)
                {
                    throw new DeploymentException($"Post-switch health check failed: {postSwitchHealth.Details}");
                }
            });

            result.Success = true;
            result.CompletedAt = DateTimeOffset.UtcNow;
            logger.LogInformation("Blue-Green deployment completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Blue-Green deployment failed");
            result.Success = false;
            result.ErrorMessage = ex.Message;
            
            // Attempt rollback
            try
            {
                await trafficManager.SwitchToBlueAsync(cancellationToken);
                logger.LogInformation("Rollback to blue environment completed");
            }
            catch (Exception rollbackEx)
            {
                logger.LogError(rollbackEx, "Rollback to blue environment failed");
                result.ErrorMessage += $" | Rollback failed: {rollbackEx.Message}";
            }
        }

        return result;
    }

    private async Task ExecuteStepWithLogging(string stepName, Func<Task> stepAction)
    {
        logger.LogInformation("Executing step: {StepName}", stepName);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            await stepAction();
            logger.LogInformation("Step completed: {StepName} in {Duration}ms", stepName, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Step failed: {StepName} after {Duration}ms", stepName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    private async Task<bool> ValidateLoadBalancerCapabilityAsync(DeploymentContext context)
    {
        // Validate that load balancer supports blue-green switching
        await Task.Delay(100); // Simulate validation
        return true;
    }

    private async Task<bool> ValidateInfrastructureCapacityAsync(DeploymentContext context)
    {
        // Validate that we have capacity for running both blue and green environments
        await Task.Delay(100);
        return true;
    }

    private async Task<bool> ValidateNetworkConfigurationAsync(DeploymentContext context)
    {
        // Validate network configuration supports blue-green deployment
        await Task.Delay(100);
        return true;
    }
}
```

### Rolling Deployment Strategy

```csharp
namespace DocumentProcessor.Aspire.Strategy.Rollout;

public class RollingDeploymentStrategy : IDeploymentStrategy
{
    public string StrategyName => "RollingDeployment";
    public DeploymentComplexity Complexity => DeploymentComplexity.Simple;
    public TimeSpan TypicalDeploymentTime => TimeSpan.FromMinutes(10);
    public RiskLevel RiskProfile => RiskLevel.Medium;

    private readonly ILogger<RollingDeploymentStrategy> logger;
    private readonly IContainerOrchestrator orchestrator;
    private readonly IHealthCheckService healthCheck;

    public RollingDeploymentStrategy(
        ILogger<RollingDeploymentStrategy> logger,
        IContainerOrchestrator orchestrator,
        IHealthCheckService healthCheck)
    {
        this.logger = logger;
        this.orchestrator = orchestrator;
        this.healthCheck = healthCheck;
    }

    public async Task<DeploymentPlan> CreateDeploymentPlanAsync(DeploymentContext context)
    {
        var totalInstances = context.Components.Sum(c => c.Resources.DesiredReplicas);
        var batchSize = CalculateOptimalBatchSize(totalInstances);

        return new DeploymentPlan
        {
            StrategyName = StrategyName,
            EstimatedDuration = TypicalDeploymentTime,
            RiskLevel = RiskProfile,
            Parameters = new Dictionary<string, object>
            {
                ["BatchSize"] = batchSize,
                ["MaxUnavailable"] = Math.Max(1, totalInstances / 4), // 25% max unavailable
                ["HealthCheckTimeout"] = TimeSpan.FromSeconds(30)
            },
            Steps = CreateRollingSteps(context.Components, batchSize)
        };
    }

    public async Task<bool> ValidatePrerequisitesAsync(DeploymentContext context)
    {
        return await Task.FromResult(
            context.Components.All(c => c.Resources.DesiredReplicas > 1) && // Must have multiple instances
            context.Components.All(c => c.SupportsZeroDowntime) // Must support graceful shutdown
        );
    }

    public async Task<DeploymentResult> ExecuteAsync(DeploymentPlan plan, CancellationToken cancellationToken = default)
    {
        var result = new DeploymentResult { StrategyUsed = StrategyName };
        var batchSize = (int)plan.Parameters["BatchSize"];
        var maxUnavailable = (int)plan.Parameters["MaxUnavailable"];

        try
        {
            logger.LogInformation("Starting Rolling deployment with batch size {BatchSize}", batchSize);

            foreach (var component in plan.Context!.Components)
            {
                await DeployComponentRollingAsync(component, batchSize, maxUnavailable, cancellationToken);
            }

            result.Success = true;
            result.CompletedAt = DateTimeOffset.UtcNow;
            logger.LogInformation("Rolling deployment completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Rolling deployment failed");
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    private async Task DeployComponentRollingAsync(
        ServiceComponent component, 
        int batchSize, 
        int maxUnavailable, 
        CancellationToken cancellationToken)
    {
        var instances = await orchestrator.GetInstancesAsync(component.Name, cancellationToken);
        var batches = instances.Chunk(batchSize);

        foreach (var batch in batches)
        {
            logger.LogInformation("Deploying batch of {Count} instances for {Component}", 
                batch.Length, component.Name);

            // Update instances in batch
            var updateTasks = batch.Select(instance => 
                orchestrator.UpdateInstanceAsync(instance, component.TargetVersion, cancellationToken));
            
            await Task.WhenAll(updateTasks);

            // Wait for instances to become healthy
            foreach (var instance in batch)
            {
                await WaitForInstanceHealthyAsync(instance, component.Name, cancellationToken);
            }

            logger.LogInformation("Batch completed successfully for {Component}", component.Name);
        }
    }

    private async Task WaitForInstanceHealthyAsync(string instanceId, string componentName, CancellationToken cancellationToken)
    {
        var timeout = TimeSpan.FromMinutes(5);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        while (stopwatch.Elapsed < timeout)
        {
            var isHealthy = await healthCheck.CheckInstanceHealthAsync(instanceId, cancellationToken);
            if (isHealthy)
            {
                logger.LogInformation("Instance {InstanceId} of {Component} is healthy", instanceId, componentName);
                return;
            }

            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
        }

        throw new DeploymentException($"Instance {instanceId} of {componentName} failed to become healthy within timeout");
    }

    private int CalculateOptimalBatchSize(int totalInstances)
    {
        return totalInstances switch
        {
            <= 3 => 1,
            <= 10 => 2,
            <= 20 => 3,
            _ => Math.Max(1, totalInstances / 10) // 10% at a time for large deployments
        };
    }

    private List<DeploymentStep> CreateRollingSteps(List<ServiceComponent> components, int batchSize)
    {
        var steps = new List<DeploymentStep>();

        foreach (var component in components)
        {
            var batches = Math.Ceiling((double)component.Resources.DesiredReplicas / batchSize);
            
            for (int i = 0; i < batches; i++)
            {
                steps.Add(new DeploymentStep
                {
                    Name = $"Deploy {component.Name} Batch {i + 1}",
                    Description = $"Update batch {i + 1} of {component.Name} instances",
                    EstimatedDuration = TimeSpan.FromMinutes(2),
                    Type = DeploymentStepType.Application
                });
            }
        }

        return steps;
    }
}
```

## Multi-Environment Management

### Environment Promotion Pipeline

```csharp
namespace DocumentProcessor.Aspire.Strategy.Promotion;

public class EnvironmentPromotionManager
{
    private readonly EnvironmentManager environmentManager;
    private readonly DeploymentStrategyRegistry strategyRegistry;
    private readonly ILogger<EnvironmentPromotionManager> logger;
    private readonly IApprovalService approvalService;

    public EnvironmentPromotionManager(
        EnvironmentManager environmentManager,
        DeploymentStrategyRegistry strategyRegistry,
        ILogger<EnvironmentPromotionManager> logger,
        IApprovalService approvalService)
    {
        this.environmentManager = environmentManager;
        this.strategyRegistry = strategyRegistry;
        this.logger = logger;
        this.approvalService = approvalService;
    }

    public async Task<PromotionResult> PromoteAsync(PromotionRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting promotion from {Source} to {Target}", 
            request.SourceEnvironment, request.TargetEnvironment);

        var result = new PromotionResult 
        { 
            PromotionId = Guid.NewGuid(),
            SourceEnvironment = request.SourceEnvironment,
            TargetEnvironment = request.TargetEnvironment,
            StartedAt = DateTimeOffset.UtcNow
        };

        try
        {
            // Validate promotion path
            await ValidatePromotionPathAsync(request);

            // Check prerequisites
            await ValidatePrerequisitesAsync(request);

            // Handle approvals if required
            if (await RequiresApprovalAsync(request))
            {
                var approvalResult = await approvalService.RequestApprovalAsync(request, cancellationToken);
                if (!approvalResult.Approved)
                {
                    result.Status = PromotionStatus.Rejected;
                    result.Message = approvalResult.Reason;
                    return result;
                }
            }

            // Execute pre-deployment hooks
            await ExecutePreDeploymentHooksAsync(request);

            // Deploy to target environment
            var deploymentContext = await CreateDeploymentContextAsync(request);
            var strategy = strategyRegistry.RecommendStrategy(deploymentContext);
            var deploymentPlan = await strategy.CreateDeploymentPlanAsync(deploymentContext);
            var deploymentResult = await strategy.ExecuteAsync(deploymentPlan, cancellationToken);

            if (!deploymentResult.Success)
            {
                throw new PromotionException($"Deployment failed: {deploymentResult.ErrorMessage}");
            }

            // Execute post-deployment validation
            await ExecutePostDeploymentValidationAsync(request);

            // Execute post-deployment hooks
            await ExecutePostDeploymentHooksAsync(request);

            result.Status = PromotionStatus.Completed;
            result.DeploymentResult = deploymentResult;
            result.CompletedAt = DateTimeOffset.UtcNow;

            logger.LogInformation("Promotion completed successfully from {Source} to {Target}", 
                request.SourceEnvironment, request.TargetEnvironment);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Promotion failed from {Source} to {Target}", 
                request.SourceEnvironment, request.TargetEnvironment);
                
            result.Status = PromotionStatus.Failed;
            result.Message = ex.Message;
            result.CompletedAt = DateTimeOffset.UtcNow;

            // Execute failure hooks
            await ExecuteFailureHooksAsync(request, ex);
        }

        return result;
    }

    private async Task ValidatePromotionPathAsync(PromotionRequest request)
    {
        var validPaths = new Dictionary<string, List<string>>
        {
            ["Development"] = new() { "Testing" },
            ["Testing"] = new() { "Staging" },
            ["Staging"] = new() { "Production" },
            ["Production"] = new() { } // No promotion from production
        };

        if (!validPaths.TryGetValue(request.SourceEnvironment, out var allowedTargets) ||
            !allowedTargets.Contains(request.TargetEnvironment))
        {
            throw new InvalidOperationException(
                $"Invalid promotion path from {request.SourceEnvironment} to {request.TargetEnvironment}");
        }

        await Task.CompletedTask;
    }

    private async Task<bool> RequiresApprovalAsync(PromotionRequest request)
    {
        var targetEnvironment = environmentManager.GetEnvironment(request.TargetEnvironment);
        return await Task.FromResult(targetEnvironment.ApprovalRequired);
    }

    private async Task<DeploymentContext> CreateDeploymentContextAsync(PromotionRequest request)
    {
        // Create deployment context based on promotion request
        // This would typically involve extracting configuration from the source environment
        // and adapting it for the target environment
        
        return await Task.FromResult(new DeploymentContext
        {
            Environment = request.TargetEnvironment,
            ApplicationVersion = request.Version,
            Configuration = request.Configuration,
            Components = request.Components
        });
    }

    private async Task ValidatePrerequisitesAsync(PromotionRequest request)
    {
        // Validate that target environment is ready for deployment
        var isReady = await environmentManager.ValidateEnvironmentReadinessAsync(
            request.TargetEnvironment, 
            await CreateDeploymentContextAsync(request));

        if (!isReady)
        {
            throw new PromotionException($"Target environment {request.TargetEnvironment} is not ready for deployment");
        }
    }

    private async Task ExecutePreDeploymentHooksAsync(PromotionRequest request)
    {
        logger.LogInformation("Executing pre-deployment hooks");
        
        // Example hooks:
        // - Database migrations
        // - Configuration updates
        // - Cache warming
        // - External service notifications
        
        await Task.Delay(100); // Simulate hook execution
    }

    private async Task ExecutePostDeploymentValidationAsync(PromotionRequest request)
    {
        logger.LogInformation("Executing post-deployment validation");
        
        // Example validations:
        // - Smoke tests
        // - Health checks
        // - Performance validation
        // - Integration tests
        
        await Task.Delay(100);
    }

    private async Task ExecutePostDeploymentHooksAsync(PromotionRequest request)
    {
        logger.LogInformation("Executing post-deployment hooks");
        
        // Example hooks:
        // - Monitoring setup
        // - Alert configuration
        // - Documentation updates
        // - Team notifications
        
        await Task.Delay(100);
    }

    private async Task ExecuteFailureHooksAsync(PromotionRequest request, Exception exception)
    {
        logger.LogInformation("Executing failure hooks");
        
        // Example failure hooks:
        // - Incident creation
        // - Team notifications
        // - Rollback initiation
        // - Post-mortem scheduling
        
        await Task.Delay(100);
    }
}

public record PromotionRequest
{
    public string SourceEnvironment { get; init; } = string.Empty;
    public string TargetEnvironment { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public Dictionary<string, string> Configuration { get; init; } = new();
    public List<ServiceComponent> Components { get; init; } = new();
    public string RequestedBy { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
}

public record PromotionResult
{
    public Guid PromotionId { get; init; }
    public string SourceEnvironment { get; init; } = string.Empty;
    public string TargetEnvironment { get; init; } = string.Empty;
    public PromotionStatus Status { get; init; }
    public string Message { get; init; } = string.Empty;
    public DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public DeploymentResult? DeploymentResult { get; init; }
}

public enum PromotionStatus { Pending, InProgress, Completed, Failed, Rejected }
```

## Deployment Decision Framework

### Decision Engine

```csharp
namespace DocumentProcessor.Aspire.Strategy.Decision;

public class DeploymentDecisionEngine
{
    private readonly List<IDecisionCriterion> criteria;
    private readonly ILogger<DeploymentDecisionEngine> logger;

    public DeploymentDecisionEngine(
        IEnumerable<IDecisionCriterion> criteria,
        ILogger<DeploymentDecisionEngine> logger)
    {
        this.criteria = criteria.ToList();
        this.logger = logger;
    }

    public async Task<DeploymentRecommendation> RecommendAsync(DeploymentScenario scenario)
    {
        logger.LogInformation("Analyzing deployment scenario for recommendations");

        var evaluations = new List<CriterionEvaluation>();

        foreach (var criterion in criteria)
        {
            try
            {
                var evaluation = await criterion.EvaluateAsync(scenario);
                evaluations.Add(evaluation);
                
                logger.LogDebug("Criterion {Criterion} evaluation: {Score} - {Reasoning}", 
                    criterion.Name, evaluation.Score, evaluation.Reasoning);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to evaluate criterion {Criterion}", criterion.Name);
            }
        }

        return GenerateRecommendation(scenario, evaluations);
    }

    private DeploymentRecommendation GenerateRecommendation(
        DeploymentScenario scenario, 
        List<CriterionEvaluation> evaluations)
    {
        var weightedScore = evaluations
            .Sum(e => e.Score * e.Weight) / evaluations.Sum(e => e.Weight);

        var recommendation = new DeploymentRecommendation
        {
            ScenarioId = scenario.Id,
            OverallScore = weightedScore,
            Evaluations = evaluations,
            GeneratedAt = DateTimeOffset.UtcNow
        };

        // Determine recommended strategy based on evaluations
        recommendation.RecommendedStrategy = DetermineOptimalStrategy(scenario, evaluations);
        recommendation.AlternativeStrategies = DetermineAlternativeStrategies(scenario, evaluations);
        recommendation.RiskFactors = IdentifyRiskFactors(evaluations);
        recommendation.Mitigations = SuggestMitigations(scenario, recommendation.RiskFactors);

        return recommendation;
    }

    private string DetermineOptimalStrategy(DeploymentScenario scenario, List<CriterionEvaluation> evaluations)
    {
        // Strategy selection logic based on evaluations
        var riskTolerance = evaluations.FirstOrDefault(e => e.CriterionName == "RiskTolerance")?.Score ?? 0.5;
        var timeConstraint = evaluations.FirstOrDefault(e => e.CriterionName == "TimeConstraint")?.Score ?? 0.5;
        var complexityScore = evaluations.FirstOrDefault(e => e.CriterionName == "Complexity")?.Score ?? 0.5;

        return (riskTolerance, timeConstraint, complexityScore) switch
        {
            (> 0.8, _, _) => "BlueGreen", // High risk tolerance
            (_, > 0.8, _) => "RollingDeployment", // Time constrained
            (< 0.3, _, > 0.7) => "MaintenanceWindow", // Low risk tolerance, high complexity
            (< 0.3, _, _) => "CanaryDeployment", // Low risk tolerance
            _ => "RollingDeployment" // Default
        };
    }

    private List<string> DetermineAlternativeStrategies(DeploymentScenario scenario, List<CriterionEvaluation> evaluations)
    {
        var alternatives = new List<string>();
        
        // Add alternatives based on scenario characteristics
        if (scenario.SupportsBlueGreen)
            alternatives.Add("BlueGreen");
        if (scenario.SupportsRolling)
            alternatives.Add("RollingDeployment");
        if (scenario.SupportsCanary)
            alternatives.Add("CanaryDeployment");
            
        return alternatives.Distinct().ToList();
    }

    private List<string> IdentifyRiskFactors(List<CriterionEvaluation> evaluations)
    {
        var risks = new List<string>();

        foreach (var evaluation in evaluations.Where(e => e.Score < 0.3))
        {
            risks.AddRange(evaluation.RiskFactors);
        }

        return risks.Distinct().ToList();
    }

    private List<string> SuggestMitigations(DeploymentScenario scenario, List<string> riskFactors)
    {
        var mitigations = new List<string>();

        foreach (var risk in riskFactors)
        {
            mitigations.AddRange(risk switch
            {
                "HighComplexity" => new[] { "Implement comprehensive testing", "Use phased rollout", "Prepare detailed rollback plan" },
                "TightTimeline" => new[] { "Automate deployment process", "Prepare emergency procedures", "Increase monitoring" },
                "CriticalSystem" => new[] { "Use blue-green deployment", "Implement circuit breakers", "Schedule maintenance window" },
                _ => new[] { "Increase monitoring and alerting" }
            });
        }

        return mitigations.Distinct().ToList();
    }
}

public interface IDecisionCriterion
{
    string Name { get; }
    Task<CriterionEvaluation> EvaluateAsync(DeploymentScenario scenario);
}

public record DeploymentScenario
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string ApplicationName { get; init; } = string.Empty;
    public string TargetEnvironment { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public TimeSpan AvailableWindow { get; init; }
    public BusinessCriticality Criticality { get; init; }
    public List<ServiceComponent> Components { get; init; } = new();
    public bool SupportsBlueGreen { get; init; }
    public bool SupportsRolling { get; init; }
    public bool SupportsCanary { get; init; }
    public ComplianceRequirements Compliance { get; init; } = new();
}

public record CriterionEvaluation
{
    public string CriterionName { get; init; } = string.Empty;
    public double Score { get; init; } // 0.0 to 1.0
    public double Weight { get; init; } // Importance weight
    public string Reasoning { get; init; } = string.Empty;
    public List<string> RiskFactors { get; init; } = new();
}

public record DeploymentRecommendation
{
    public Guid ScenarioId { get; init; }
    public string RecommendedStrategy { get; init; } = string.Empty;
    public List<string> AlternativeStrategies { get; init; } = new();
    public double OverallScore { get; init; }
    public List<CriterionEvaluation> Evaluations { get; init; } = new();
    public List<string> RiskFactors { get; init; } = new();
    public List<string> Mitigations { get; init; } = new();
    public DateTimeOffset GeneratedAt { get; init; }
}

public enum BusinessCriticality { Low, Medium, High, Critical }
```

## Strategy Implementation

### Strategy Configuration

```csharp
namespace DocumentProcessor.Aspire.Strategy.Configuration;

public static class DeploymentStrategyServiceExtensions
{
    public static IServiceCollection AddDeploymentStrategies(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Register core services
        services.AddSingleton<DeploymentStrategyRegistry>();
        services.AddSingleton<EnvironmentManager>();
        services.AddSingleton<EnvironmentPromotionManager>();
        services.AddSingleton<DeploymentDecisionEngine>();

        // Register deployment strategies
        services.AddTransient<IDeploymentStrategy, BlueGreenDeploymentStrategy>();
        services.AddTransient<IDeploymentStrategy, RollingDeploymentStrategy>();
        services.AddTransient<IDeploymentStrategy, CanaryDeploymentStrategy>();
        services.AddTransient<IDeploymentStrategy, MaintenanceWindowDeploymentStrategy>();

        // Register decision criteria
        services.AddTransient<IDecisionCriterion, RiskToleranceCriterion>();
        services.AddTransient<IDecisionCriterion, TimeConstraintCriterion>();
        services.AddTransient<IDecisionCriterion, ComplexityCriterion>();
        services.AddTransient<IDecisionCriterion, BusinessCriticalityCriterion>();

        // Register supporting services
        services.AddTransient<IInfrastructureOrchestrator, AzureInfrastructureOrchestrator>();
        services.AddTransient<IContainerOrchestrator, KubernetesOrchestrator>();
        services.AddTransient<ITrafficManager, AzureTrafficManager>();
        services.AddTransient<IHealthCheckService, AspireHealthCheckService>();
        services.AddTransient<IApprovalService, ApprovalService>();

        // Configure options
        services.Configure<DeploymentStrategyOptions>(configuration.GetSection("DeploymentStrategy"));
        
        return services;
    }
}

public class DeploymentStrategyOptions
{
    public string DefaultStrategy { get; set; } = "RollingDeployment";
    public Dictionary<string, EnvironmentStrategyOptions> Environments { get; set; } = new();
    public int DefaultTimeoutMinutes { get; set; } = 30;
    public bool EnableApprovalWorkflow { get; set; } = true;
    public string ApprovalServiceUrl { get; set; } = string.Empty;
}

public class EnvironmentStrategyOptions
{
    public string PreferredStrategy { get; set; } = string.Empty;
    public bool RequireApproval { get; set; }
    public List<string> AllowedStrategies { get; set; } = new();
    public Dictionary<string, object> StrategyParameters { get; set; } = new();
}
```

**Usage**:

1. **Strategy Selection**: Use the framework to select optimal deployment strategies based on context
2. **Environment Management**: Manage multi-environment deployments with proper validation and promotion
3. **Risk Assessment**: Leverage the decision engine to assess and mitigate deployment risks
4. **Rollout Patterns**: Implement proven rollout patterns like blue-green, rolling, and canary deployments
5. **Automation**: Automate deployment decisions and execution through the strategic framework
6. **Compliance**: Ensure deployments meet organizational compliance and approval requirements
7. **Monitoring**: Track deployment success and performance across different strategies

**Notes**:

- **Strategy Flexibility**: Framework supports multiple deployment strategies with automatic selection
- **Environment Promotion**: Structured approach to promoting applications through environment tiers
- **Risk Management**: Built-in risk assessment and mitigation recommendations
- **Approval Workflows**: Configurable approval processes for sensitive environments
- **Extensibility**: Pluggable architecture for adding custom strategies and decision criteria
- **Observability**: Comprehensive logging and monitoring throughout the deployment process
- **Rollback Capability**: Automatic rollback mechanisms for failed deployments

**Related Snippets**:

- [Production Deployment](production-deployment.md) - Implementing specific deployment technologies
- [Scaling Strategies](scaling-strategies.md) - Auto-scaling production workloads
- [Service Orchestration](service-orchestration.md) - Coordinating services during deployment
- [Health Monitoring](health-monitoring.md) - Monitoring deployment success and application health
