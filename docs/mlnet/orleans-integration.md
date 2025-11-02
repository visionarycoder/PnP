# Orleans Integration for ML.NET

**Description**: Integration patterns for ML.NET with Microsoft Orleans framework, enabling distributed machine learning services with grain-based architecture, stateful model management, and scalable inference pipelines for high-throughput scenarios.

**Language/Technology**: C#, ML.NET, Microsoft Orleans, ASP.NET Core

**Code**:

## Orleans ML.NET Integration Framework

### ML Grain Interfaces and Implementations

```csharp
namespace DocumentProcessor.Orleans.ML;

using Microsoft.ML;
using Orleans;
using System.Collections.Concurrent;

// Grain interfaces for ML operations
public interface IMLModelGrain : IGrainWithStringKey
{
    Task<ModelInfo> GetModelInfoAsync();
    Task<PredictionResult> PredictAsync(PredictionRequest request);
    Task<BatchPredictionResult> PredictBatchAsync(BatchPredictionRequest request);
    Task<LoadModelResult> LoadModelAsync(string modelPath, string version);
    Task<UnloadModelResult> UnloadModelAsync();
    Task<ModelMetrics> GetModelMetricsAsync();
    Task<HealthStatus> GetHealthStatusAsync();
    Task WarmupAsync(WarmupRequest request);
}

public interface IMLModelManagerGrain : IGrainWithStringKey
{
    Task<DeploymentResult> DeployModelAsync(ModelDeploymentRequest request);
    Task<List<ModelInfo>> GetActiveModelsAsync();
    Task<ModelInfo> GetModelAsync(string modelId);
    Task<ScalingResult> ScaleModelAsync(string modelId, ScalingRequest request);
    Task<LoadBalancingInfo> GetLoadBalancingInfoAsync(string modelId);
    Task<ModelPerformanceMetrics> GetPerformanceMetricsAsync(string modelId, TimeSpan period);
}

public interface IMLTrainingGrain : IGrainWithStringKey
{
    Task<TrainingResult> StartTrainingAsync(TrainingRequest request);
    Task<TrainingStatus> GetTrainingStatusAsync(string trainingJobId);
    Task<TrainingResult> GetTrainingResultAsync(string trainingJobId);
    Task<CancelTrainingResult> CancelTrainingAsync(string trainingJobId);
    Task<List<TrainingJob>> GetActiveTrainingJobsAsync();
}

public interface IMLPipelineGrain : IGrainWithStringKey
{
    Task<PipelineResult> ExecutePipelineAsync(PipelineRequest request);
    Task<PipelineStatus> GetPipelineStatusAsync();
    Task<List<PipelineStepResult>> GetPipelineStepsAsync();
    Task<PipelineMetrics> GetPipelineMetricsAsync();
}

// ML Model Grain Implementation
public class MLModelGrain : Grain, IMLModelGrain
{
    private readonly ILogger<MLModelGrain> _logger;
    private readonly IMLModelRepository _modelRepository;
    private readonly IMLMetricsCollector _metricsCollector;
    
    private ITransformer? _model;
    private MLContext? _mlContext;
    private ModelInfo? _modelInfo;
    private readonly ConcurrentQueue<PredictionMetrics> _recentPredictions;
    private Timer? _metricsTimer;

    public MLModelGrain(
        ILogger<MLModelGrain> logger,
        IMLModelRepository modelRepository,
        IMLMetricsCollector metricsCollector)
    {
        _logger = logger;
        _modelRepository = modelRepository;
        _metricsCollector = metricsCollector;
        _recentPredictions = new ConcurrentQueue<PredictionMetrics>();
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Activating ML Model Grain {GrainId}", this.GetPrimaryKeyString());
        
        _mlContext = new MLContext(seed: 0);
        
        // Start metrics collection timer
        _metricsTimer = RegisterTimer(CollectMetrics, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        
        await base.OnActivateAsync(cancellationToken);
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deactivating ML Model Grain {GrainId}, Reason: {Reason}", 
            this.GetPrimaryKeyString(), reason);
        
        _metricsTimer?.Dispose();
        _model?.Dispose();
        
        await base.OnDeactivateAsync(reason, cancellationToken);
    }

    public async Task<ModelInfo> GetModelInfoAsync()
    {
        if (_modelInfo == null)
        {
            return new ModelInfo(
                Id: this.GetPrimaryKeyString(),
                Name: "Not Loaded",
                Version: "Unknown",
                Status: ModelStatus.NotLoaded,
                LoadedAt: null,
                LastPredictionAt: null,
                PredictionCount: 0,
                ModelSize: 0);
        }

        return await Task.FromResult(_modelInfo);
    }

    public async Task<PredictionResult> PredictAsync(PredictionRequest request)
    {
        if (_model == null || _mlContext == null)
        {
            return new PredictionResult(
                Success: false,
                Error: "Model not loaded",
                RequestId: request.RequestId,
                ProcessingTime: TimeSpan.Zero);
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            _logger.LogDebug("Processing prediction request {RequestId}", request.RequestId);

            // Create data view from input
            var dataView = _mlContext.Data.LoadFromEnumerable(new[] { request.Input });
            
            // Transform data and get predictions
            var predictions = _model.Transform(dataView);
            
            // Extract prediction results (this would be specific to your model type)
            var predictionResults = _mlContext.Data.CreateEnumerable<PredictionOutput>(predictions, false).ToArray();
            
            stopwatch.Stop();
            
            // Record metrics
            var metrics = new PredictionMetrics(
                RequestId: request.RequestId,
                ProcessingTime: stopwatch.Elapsed,
                Success: true,
                Timestamp: DateTime.UtcNow,
                InputSize: EstimateInputSize(request.Input),
                ModelId: this.GetPrimaryKeyString());

            _recentPredictions.Enqueue(metrics);
            await _metricsCollector.RecordPredictionAsync(metrics);

            // Update model info
            if (_modelInfo != null)
            {
                _modelInfo = _modelInfo with 
                { 
                    LastPredictionAt = DateTime.UtcNow,
                    PredictionCount = _modelInfo.PredictionCount + 1
                };
            }

            return new PredictionResult(
                Success: true,
                Predictions: predictionResults,
                RequestId: request.RequestId,
                ProcessingTime: stopwatch.Elapsed,
                ModelVersion: _modelInfo?.Version);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Prediction failed for request {RequestId}", request.RequestId);
            
            var errorMetrics = new PredictionMetrics(
                RequestId: request.RequestId,
                ProcessingTime: stopwatch.Elapsed,
                Success: false,
                Timestamp: DateTime.UtcNow,
                InputSize: EstimateInputSize(request.Input),
                ModelId: this.GetPrimaryKeyString(),
                Error: ex.Message);

            _recentPredictions.Enqueue(errorMetrics);
            await _metricsCollector.RecordPredictionAsync(errorMetrics);

            return new PredictionResult(
                Success: false,
                Error: ex.Message,
                RequestId: request.RequestId,
                ProcessingTime: stopwatch.Elapsed);
        }
    }

    public async Task<BatchPredictionResult> PredictBatchAsync(BatchPredictionRequest request)
    {
        if (_model == null || _mlContext == null)
        {
            return new BatchPredictionResult(
                Success: false,
                Error: "Model not loaded",
                RequestId: request.RequestId,
                ProcessingTime: TimeSpan.Zero,
                ProcessedCount: 0);
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var results = new List<PredictionOutput>();
        var errors = new List<string>();

        try
        {
            _logger.LogInformation("Processing batch prediction request {RequestId} with {Count} items", 
                request.RequestId, request.Inputs.Count);

            // Process in chunks to manage memory
            var chunkSize = Math.Min(request.BatchSize ?? 1000, request.Inputs.Count);
            var chunks = request.Inputs.Chunk(chunkSize);

            foreach (var chunk in chunks)
            {
                try
                {
                    var dataView = _mlContext.Data.LoadFromEnumerable(chunk);
                    var predictions = _model.Transform(dataView);
                    var chunkResults = _mlContext.Data.CreateEnumerable<PredictionOutput>(predictions, false);
                    
                    results.AddRange(chunkResults);
                }
                catch (Exception chunkEx)
                {
                    _logger.LogWarning(chunkEx, "Failed to process chunk in batch {RequestId}", request.RequestId);
                    errors.Add($"Chunk processing failed: {chunkEx.Message}");
                }
            }

            stopwatch.Stop();

            var batchMetrics = new BatchPredictionMetrics(
                RequestId: request.RequestId,
                ProcessingTime: stopwatch.Elapsed,
                Success: errors.Count == 0,
                Timestamp: DateTime.UtcNow,
                InputCount: request.Inputs.Count,
                ProcessedCount: results.Count,
                ModelId: this.GetPrimaryKeyString(),
                Errors: errors);

            await _metricsCollector.RecordBatchPredictionAsync(batchMetrics);

            // Update model info
            if (_modelInfo != null)
            {
                _modelInfo = _modelInfo with 
                { 
                    LastPredictionAt = DateTime.UtcNow,
                    PredictionCount = _modelInfo.PredictionCount + results.Count
                };
            }

            return new BatchPredictionResult(
                Success: errors.Count == 0,
                Predictions: results,
                RequestId: request.RequestId,
                ProcessingTime: stopwatch.Elapsed,
                ProcessedCount: results.Count,
                Errors: errors,
                ModelVersion: _modelInfo?.Version);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Batch prediction failed for request {RequestId}", request.RequestId);

            return new BatchPredictionResult(
                Success: false,
                Error: ex.Message,
                RequestId: request.RequestId,
                ProcessingTime: stopwatch.Elapsed,
                ProcessedCount: results.Count);
        }
    }

    public async Task<LoadModelResult> LoadModelAsync(string modelPath, string version)
    {
        try
        {
            _logger.LogInformation("Loading model from {ModelPath} version {Version}", modelPath, version);

            if (_mlContext == null)
            {
                _mlContext = new MLContext(seed: 0);
            }

            // Load model from repository
            var modelData = await _modelRepository.LoadModelAsync(modelPath, version);
            
            // Dispose previous model if exists
            _model?.Dispose();
            
            // Load new model
            using var stream = new MemoryStream(modelData.ModelBytes);
            _model = _mlContext.Model.Load(stream, out var modelInputSchema);

            // Update model info
            _modelInfo = new ModelInfo(
                Id: this.GetPrimaryKeyString(),
                Name: modelData.Name,
                Version: version,
                Status: ModelStatus.Loaded,
                LoadedAt: DateTime.UtcNow,
                LastPredictionAt: null,
                PredictionCount: 0,
                ModelSize: modelData.ModelBytes.Length,
                Schema: modelInputSchema.ToString());

            _logger.LogInformation("Successfully loaded model {ModelId} version {Version}", 
                this.GetPrimaryKeyString(), version);

            return new LoadModelResult(
                Success: true,
                ModelId: this.GetPrimaryKeyString(),
                Version: version,
                LoadTime: DateTime.UtcNow,
                ModelSize: modelData.ModelBytes.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load model from {ModelPath}", modelPath);
            
            _modelInfo = _modelInfo?.WithStatus(ModelStatus.LoadFailed) ?? 
                new ModelInfo(
                    Id: this.GetPrimaryKeyString(),
                    Name: "Load Failed",
                    Version: version,
                    Status: ModelStatus.LoadFailed,
                    LoadedAt: null,
                    LastPredictionAt: null,
                    PredictionCount: 0,
                    ModelSize: 0);

            return new LoadModelResult(
                Success: false,
                Error: ex.Message,
                ModelId: this.GetPrimaryKeyString(),
                Version: version,
                LoadTime: DateTime.UtcNow);
        }
    }

    public async Task<UnloadModelResult> UnloadModelAsync()
    {
        try
        {
            _logger.LogInformation("Unloading model {ModelId}", this.GetPrimaryKeyString());

            _model?.Dispose();
            _model = null;

            if (_modelInfo != null)
            {
                _modelInfo = _modelInfo with { Status = ModelStatus.Unloaded };
            }

            return await Task.FromResult(new UnloadModelResult(
                Success: true,
                ModelId: this.GetPrimaryKeyString(),
                UnloadTime: DateTime.UtcNow));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unload model {ModelId}", this.GetPrimaryKeyString());
            
            return new UnloadModelResult(
                Success: false,
                Error: ex.Message,
                ModelId: this.GetPrimaryKeyString(),
                UnloadTime: DateTime.UtcNow);
        }
    }

    public async Task<ModelMetrics> GetModelMetricsAsync()
    {
        var recentMetrics = _recentPredictions.ToArray();
        
        var totalRequests = recentMetrics.Length;
        var successfulRequests = recentMetrics.Count(m => m.Success);
        var averageProcessingTime = recentMetrics.Length > 0 
            ? TimeSpan.FromMilliseconds(recentMetrics.Average(m => m.ProcessingTime.TotalMilliseconds))
            : TimeSpan.Zero;
        
        var p95ProcessingTime = recentMetrics.Length > 0
            ? TimeSpan.FromMilliseconds(recentMetrics.OrderBy(m => m.ProcessingTime)
                .Skip((int)(recentMetrics.Length * 0.95)).First().ProcessingTime.TotalMilliseconds)
            : TimeSpan.Zero;

        return await Task.FromResult(new ModelMetrics(
            ModelId: this.GetPrimaryKeyString(),
            TotalRequests: totalRequests,
            SuccessfulRequests: successfulRequests,
            FailedRequests: totalRequests - successfulRequests,
            AverageProcessingTime: averageProcessingTime,
            P95ProcessingTime: p95ProcessingTime,
            RequestsPerMinute: CalculateRequestsPerMinute(recentMetrics),
            LastUpdateTime: DateTime.UtcNow));
    }

    public async Task<HealthStatus> GetHealthStatusAsync()
    {
        var isHealthy = _model != null && _modelInfo?.Status == ModelStatus.Loaded;
        var recentErrors = _recentPredictions.Where(m => !m.Success && 
            m.Timestamp > DateTime.UtcNow.AddMinutes(-5)).Count();
        
        if (recentErrors > 10)
        {
            isHealthy = false;
        }

        return await Task.FromResult(new HealthStatus(
            IsHealthy: isHealthy,
            ModelId: this.GetPrimaryKeyString(),
            Status: _modelInfo?.Status ?? ModelStatus.NotLoaded,
            RecentErrors: recentErrors,
            LastPredictionAt: _modelInfo?.LastPredictionAt,
            CheckedAt: DateTime.UtcNow));
    }

    public async Task WarmupAsync(WarmupRequest request)
    {
        if (_model == null || _mlContext == null)
        {
            throw new InvalidOperationException("Model not loaded");
        }

        _logger.LogInformation("Warming up model {ModelId} with {Count} requests", 
            this.GetPrimaryKeyString(), request.WarmupInputs.Count);

        var tasks = request.WarmupInputs.Select(async input =>
        {
            try
            {
                var warmupRequest = new PredictionRequest(
                    RequestId: Guid.NewGuid().ToString(),
                    Input: input,
                    Timeout: TimeSpan.FromSeconds(30));

                await PredictAsync(warmupRequest);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Warmup request failed for model {ModelId}", this.GetPrimaryKeyString());
            }
        });

        await Task.WhenAll(tasks);
        _logger.LogInformation("Model warmup completed for {ModelId}", this.GetPrimaryKeyString());
    }

    private async Task CollectMetrics(object _)
    {
        try
        {
            var metrics = await GetModelMetricsAsync();
            await _metricsCollector.RecordModelMetricsAsync(metrics);
            
            // Clean old metrics (keep last hour)
            var cutoffTime = DateTime.UtcNow.AddHours(-1);
            while (_recentPredictions.TryPeek(out var oldMetric) && oldMetric.Timestamp < cutoffTime)
            {
                _recentPredictions.TryDequeue(out _);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to collect metrics for model {ModelId}", this.GetPrimaryKeyString());
        }
    }

    private int EstimateInputSize(object input)
    {
        // Simple estimation - in real scenario, implement proper size calculation
        return input?.ToString()?.Length ?? 0;
    }

    private double CalculateRequestsPerMinute(PredictionMetrics[] metrics)
    {
        if (metrics.Length == 0) return 0;
        
        var oneMinuteAgo = DateTime.UtcNow.AddMinutes(-1);
        var recentCount = metrics.Count(m => m.Timestamp > oneMinuteAgo);
        
        return recentCount;
    }
}

// ML Model Manager Grain Implementation
public class MLModelManagerGrain : Grain, IMLModelManagerGrain
{
    private readonly ILogger<MLModelManagerGrain> _logger;
    private readonly IClusterClient _clusterClient;
    private readonly IMLModelRepository _modelRepository;
    private readonly ConcurrentDictionary<string, ModelDeploymentInfo> _deployedModels;

    public MLModelManagerGrain(
        ILogger<MLModelManagerGrain> logger,
        IClusterClient clusterClient,
        IMLModelRepository modelRepository)
    {
        _logger = logger;
        _clusterClient = clusterClient;
        _modelRepository = modelRepository;
        _deployedModels = new ConcurrentDictionary<string, ModelDeploymentInfo>();
    }

    public async Task<DeploymentResult> DeployModelAsync(ModelDeploymentRequest request)
    {
        _logger.LogInformation("Deploying model {ModelId} version {Version} with {Instances} instances",
            request.ModelId, request.Version, request.InstanceCount);

        try
        {
            var deploymentInfo = new ModelDeploymentInfo(
                ModelId: request.ModelId,
                Version: request.Version,
                InstanceCount: request.InstanceCount,
                DeployedAt: DateTime.UtcNow,
                Status: DeploymentStatus.Deploying);

            _deployedModels[request.ModelId] = deploymentInfo;

            // Deploy to multiple grain instances for load distribution
            var deploymentTasks = Enumerable.Range(0, request.InstanceCount)
                .Select(async i =>
                {
                    var grainKey = $"{request.ModelId}-instance-{i}";
                    var modelGrain = _clusterClient.GetGrain<IMLModelGrain>(grainKey);
                    
                    var loadResult = await modelGrain.LoadModelAsync(request.ModelPath, request.Version);
                    if (!loadResult.Success)
                    {
                        throw new Exception($"Failed to load model on instance {i}: {loadResult.Error}");
                    }

                    // Warmup if requested
                    if (request.WarmupInputs?.Any() == true)
                    {
                        var warmupRequest = new WarmupRequest(request.WarmupInputs);
                        await modelGrain.WarmupAsync(warmupRequest);
                    }

                    return grainKey;
                });

            var instanceIds = await Task.WhenAll(deploymentTasks);

            // Update deployment status
            _deployedModels[request.ModelId] = deploymentInfo with 
            { 
                Status = DeploymentStatus.Deployed,
                InstanceIds = instanceIds.ToList()
            };

            _logger.LogInformation("Successfully deployed model {ModelId} to {Count} instances",
                request.ModelId, instanceIds.Length);

            return new DeploymentResult(
                Success: true,
                ModelId: request.ModelId,
                Version: request.Version,
                InstanceCount: instanceIds.Length,
                InstanceIds: instanceIds.ToList(),
                DeployedAt: DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deploy model {ModelId}", request.ModelId);

            // Update deployment status to failed
            if (_deployedModels.TryGetValue(request.ModelId, out var failedDeployment))
            {
                _deployedModels[request.ModelId] = failedDeployment with { Status = DeploymentStatus.Failed };
            }

            return new DeploymentResult(
                Success: false,
                Error: ex.Message,
                ModelId: request.ModelId,
                Version: request.Version,
                DeployedAt: DateTime.UtcNow);
        }
    }

    public async Task<List<ModelInfo>> GetActiveModelsAsync()
    {
        var modelInfos = new List<ModelInfo>();

        foreach (var deployment in _deployedModels.Values.Where(d => d.Status == DeploymentStatus.Deployed))
        {
            foreach (var instanceId in deployment.InstanceIds ?? new List<string>())
            {
                try
                {
                    var modelGrain = _clusterClient.GetGrain<IMLModelGrain>(instanceId);
                    var modelInfo = await modelGrain.GetModelInfoAsync();
                    modelInfos.Add(modelInfo);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get model info for instance {InstanceId}", instanceId);
                }
            }
        }

        return modelInfos;
    }

    public async Task<ModelInfo> GetModelAsync(string modelId)
    {
        if (!_deployedModels.TryGetValue(modelId, out var deployment))
        {
            throw new ArgumentException($"Model {modelId} not found");
        }

        if (deployment.InstanceIds?.Any() != true)
        {
            throw new InvalidOperationException($"No instances found for model {modelId}");
        }

        // Get info from first available instance
        var firstInstanceId = deployment.InstanceIds.First();
        var modelGrain = _clusterClient.GetGrain<IMLModelGrain>(firstInstanceId);
        
        return await modelGrain.GetModelInfoAsync();
    }

    public async Task<ScalingResult> ScaleModelAsync(string modelId, ScalingRequest request)
    {
        _logger.LogInformation("Scaling model {ModelId} to {TargetInstances} instances", 
            modelId, request.TargetInstanceCount);

        if (!_deployedModels.TryGetValue(modelId, out var deployment))
        {
            return new ScalingResult(
                Success: false,
                Error: $"Model {modelId} not found",
                ModelId: modelId);
        }

        try
        {
            var currentInstanceCount = deployment.InstanceCount;
            var targetInstanceCount = request.TargetInstanceCount;

            if (targetInstanceCount > currentInstanceCount)
            {
                // Scale up - add new instances
                var newInstanceTasks = Enumerable.Range(currentInstanceCount, targetInstanceCount - currentInstanceCount)
                    .Select(async i =>
                    {
                        var grainKey = $"{modelId}-instance-{i}";
                        var modelGrain = _clusterClient.GetGrain<IMLModelGrain>(grainKey);
                        
                        var loadResult = await modelGrain.LoadModelAsync(deployment.ModelPath ?? "", deployment.Version);
                        if (!loadResult.Success)
                        {
                            throw new Exception($"Failed to load model on new instance {i}: {loadResult.Error}");
                        }

                        return grainKey;
                    });

                var newInstanceIds = await Task.WhenAll(newInstanceTasks);
                
                var updatedInstanceIds = deployment.InstanceIds?.ToList() ?? new List<string>();
                updatedInstanceIds.AddRange(newInstanceIds);

                _deployedModels[modelId] = deployment with 
                { 
                    InstanceCount = targetInstanceCount,
                    InstanceIds = updatedInstanceIds
                };
            }
            else if (targetInstanceCount < currentInstanceCount)
            {
                // Scale down - remove instances
                var instancesToRemove = deployment.InstanceIds?.Skip(targetInstanceCount).ToList() ?? new List<string>();
                
                foreach (var instanceId in instancesToRemove)
                {
                    try
                    {
                        var modelGrain = _clusterClient.GetGrain<IMLModelGrain>(instanceId);
                        await modelGrain.UnloadModelAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to unload model instance {InstanceId}", instanceId);
                    }
                }

                var remainingInstanceIds = deployment.InstanceIds?.Take(targetInstanceCount).ToList() ?? new List<string>();

                _deployedModels[modelId] = deployment with 
                { 
                    InstanceCount = targetInstanceCount,
                    InstanceIds = remainingInstanceIds
                };
            }

            _logger.LogInformation("Successfully scaled model {ModelId} from {OldCount} to {NewCount} instances",
                modelId, currentInstanceCount, targetInstanceCount);

            return new ScalingResult(
                Success: true,
                ModelId: modelId,
                PreviousInstanceCount: currentInstanceCount,
                NewInstanceCount: targetInstanceCount,
                ScaledAt: DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scale model {ModelId}", modelId);
            
            return new ScalingResult(
                Success: false,
                Error: ex.Message,
                ModelId: modelId,
                ScaledAt: DateTime.UtcNow);
        }
    }

    public async Task<LoadBalancingInfo> GetLoadBalancingInfoAsync(string modelId)
    {
        if (!_deployedModels.TryGetValue(modelId, out var deployment))
        {
            throw new ArgumentException($"Model {modelId} not found");
        }

        var instanceMetrics = new List<InstanceMetrics>();

        if (deployment.InstanceIds != null)
        {
            foreach (var instanceId in deployment.InstanceIds)
            {
                try
                {
                    var modelGrain = _clusterClient.GetGrain<IMLModelGrain>(instanceId);
                    var metrics = await modelGrain.GetModelMetricsAsync();
                    var health = await modelGrain.GetHealthStatusAsync();

                    instanceMetrics.Add(new InstanceMetrics(
                        InstanceId: instanceId,
                        RequestsPerMinute: metrics.RequestsPerMinute,
                        AverageProcessingTime: metrics.AverageProcessingTime,
                        IsHealthy: health.IsHealthy,
                        LastPredictionAt: metrics.LastUpdateTime));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get metrics for instance {InstanceId}", instanceId);
                }
            }
        }

        return new LoadBalancingInfo(
            ModelId: modelId,
            InstanceCount: deployment.InstanceCount,
            HealthyInstances: instanceMetrics.Count(i => i.IsHealthy),
            TotalRequestsPerMinute: instanceMetrics.Sum(i => i.RequestsPerMinute),
            AverageProcessingTime: instanceMetrics.Any() 
                ? TimeSpan.FromMilliseconds(instanceMetrics.Average(i => i.AverageProcessingTime.TotalMilliseconds))
                : TimeSpan.Zero,
            InstanceMetrics: instanceMetrics);
    }

    public async Task<ModelPerformanceMetrics> GetPerformanceMetricsAsync(string modelId, TimeSpan period)
    {
        if (!_deployedModels.TryGetValue(modelId, out var deployment))
        {
            throw new ArgumentException($"Model {modelId} not found");
        }

        var allMetrics = new List<ModelMetrics>();

        if (deployment.InstanceIds != null)
        {
            foreach (var instanceId in deployment.InstanceIds)
            {
                try
                {
                    var modelGrain = _clusterClient.GetGrain<IMLModelGrain>(instanceId);
                    var metrics = await modelGrain.GetModelMetricsAsync();
                    allMetrics.Add(metrics);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get metrics for instance {InstanceId}", instanceId);
                }
            }
        }

        var totalRequests = allMetrics.Sum(m => m.TotalRequests);
        var totalSuccessful = allMetrics.Sum(m => m.SuccessfulRequests);
        var totalFailed = allMetrics.Sum(m => m.FailedRequests);
        var avgProcessingTime = allMetrics.Any() 
            ? TimeSpan.FromMilliseconds(allMetrics.Average(m => m.AverageProcessingTime.TotalMilliseconds))
            : TimeSpan.Zero;

        return new ModelPerformanceMetrics(
            ModelId: modelId,
            Period: period,
            TotalRequests: totalRequests,
            SuccessfulRequests: totalSuccessful,
            FailedRequests: totalFailed,
            SuccessRate: totalRequests > 0 ? (double)totalSuccessful / totalRequests : 0.0,
            AverageProcessingTime: avgProcessingTime,
            RequestsPerMinute: allMetrics.Sum(m => m.RequestsPerMinute),
            InstanceCount: deployment.InstanceCount,
            GeneratedAt: DateTime.UtcNow);
    }
}
```

## Data Transfer Objects

### Orleans ML Data Types

```csharp
namespace DocumentProcessor.Orleans.ML.Models;

// Request/Response Types
[GenerateSerializer]
public record PredictionRequest(
    [property: Id(0)] string RequestId,
    [property: Id(1)] object Input,
    [property: Id(2)] TimeSpan? Timeout = null,
    [property: Id(3)] Dictionary<string, object>? Metadata = null);

[GenerateSerializer]
public record PredictionResult(
    [property: Id(0)] bool Success,
    [property: Id(1)] IEnumerable<PredictionOutput>? Predictions = null,
    [property: Id(2)] string? Error = null,
    [property: Id(3)] string RequestId = "",
    [property: Id(4)] TimeSpan ProcessingTime = default,
    [property: Id(5)] string? ModelVersion = null);

[GenerateSerializer]
public record BatchPredictionRequest(
    [property: Id(0)] string RequestId,
    [property: Id(1)] List<object> Inputs,
    [property: Id(2)] int? BatchSize = null,
    [property: Id(3)] TimeSpan? Timeout = null);

[GenerateSerializer]
public record BatchPredictionResult(
    [property: Id(0)] bool Success,
    [property: Id(1)] List<PredictionOutput>? Predictions = null,
    [property: Id(2)] string? Error = null,
    [property: Id(3)] string RequestId = "",
    [property: Id(4)] TimeSpan ProcessingTime = default,
    [property: Id(5)] int ProcessedCount = 0,
    [property: Id(6)] List<string>? Errors = null,
    [property: Id(7)] string? ModelVersion = null);

[GenerateSerializer]
public record WarmupRequest(
    [property: Id(0)] List<object> WarmupInputs,
    [property: Id(1)] int? ConcurrentRequests = null);

// Model Management Types
[GenerateSerializer]
public record ModelInfo(
    [property: Id(0)] string Id,
    [property: Id(1)] string Name,
    [property: Id(2)] string Version,
    [property: Id(3)] ModelStatus Status,
    [property: Id(4)] DateTime? LoadedAt,
    [property: Id(5)] DateTime? LastPredictionAt,
    [property: Id(6)] int PredictionCount,
    [property: Id(7)] long ModelSize,
    [property: Id(8)] string? Schema = null)
{
    public ModelInfo WithStatus(ModelStatus newStatus) =>
        this with { Status = newStatus };
}

[GenerateSerializer]
public record LoadModelResult(
    [property: Id(0)] bool Success,
    [property: Id(1)] string? Error = null,
    [property: Id(2)] string ModelId = "",
    [property: Id(3)] string Version = "",
    [property: Id(4)] DateTime LoadTime = default,
    [property: Id(5)] long ModelSize = 0);

[GenerateSerializer]
public record UnloadModelResult(
    [property: Id(0)] bool Success,
    [property: Id(1)] string? Error = null,
    [property: Id(2)] string ModelId = "",
    [property: Id(3)] DateTime UnloadTime = default);

// Deployment Types
[GenerateSerializer]
public record ModelDeploymentRequest(
    [property: Id(0)] string ModelId,
    [property: Id(1)] string Version,
    [property: Id(2)] string ModelPath,
    [property: Id(3)] int InstanceCount,
    [property: Id(4)] List<object>? WarmupInputs = null);

[GenerateSerializer]
public record DeploymentResult(
    [property: Id(0)] bool Success,
    [property: Id(1)] string? Error = null,
    [property: Id(2)] string ModelId = "",
    [property: Id(3)] string Version = "",
    [property: Id(4)] int InstanceCount = 0,
    [property: Id(5)] List<string>? InstanceIds = null,
    [property: Id(6)] DateTime DeployedAt = default);

[GenerateSerializer]
public record ModelDeploymentInfo(
    [property: Id(0)] string ModelId,
    [property: Id(1)] string Version,
    [property: Id(2)] int InstanceCount,
    [property: Id(3)] DateTime DeployedAt,
    [property: Id(4)] DeploymentStatus Status,
    [property: Id(5)] List<string>? InstanceIds = null,
    [property: Id(6)] string? ModelPath = null);

// Scaling Types
[GenerateSerializer]
public record ScalingRequest(
    [property: Id(0)] int TargetInstanceCount,
    [property: Id(1)] ScalingStrategy Strategy = ScalingStrategy.Immediate,
    [property: Id(2)] TimeSpan? ScalingTimeout = null);

[GenerateSerializer]
public record ScalingResult(
    [property: Id(0)] bool Success,
    [property: Id(1)] string? Error = null,
    [property: Id(2)] string ModelId = "",
    [property: Id(3)] int PreviousInstanceCount = 0,
    [property: Id(4)] int NewInstanceCount = 0,
    [property: Id(5)] DateTime ScaledAt = default);

// Metrics Types
[GenerateSerializer]
public record ModelMetrics(
    [property: Id(0)] string ModelId,
    [property: Id(1)] int TotalRequests,
    [property: Id(2)] int SuccessfulRequests,
    [property: Id(3)] int FailedRequests,
    [property: Id(4)] TimeSpan AverageProcessingTime,
    [property: Id(5)] TimeSpan P95ProcessingTime,
    [property: Id(6)] double RequestsPerMinute,
    [property: Id(7)] DateTime LastUpdateTime);

[GenerateSerializer]
public record PredictionMetrics(
    [property: Id(0)] string RequestId,
    [property: Id(1)] TimeSpan ProcessingTime,
    [property: Id(2)] bool Success,
    [property: Id(3)] DateTime Timestamp,
    [property: Id(4)] int InputSize,
    [property: Id(5)] string ModelId,
    [property: Id(6)] string? Error = null);

[GenerateSerializer]
public record BatchPredictionMetrics(
    [property: Id(0)] string RequestId,
    [property: Id(1)] TimeSpan ProcessingTime,
    [property: Id(2)] bool Success,
    [property: Id(3)] DateTime Timestamp,
    [property: Id(4)] int InputCount,
    [property: Id(5)] int ProcessedCount,
    [property: Id(6)] string ModelId,
    [property: Id(7)] List<string>? Errors = null);

[GenerateSerializer]
public record HealthStatus(
    [property: Id(0)] bool IsHealthy,
    [property: Id(1)] string ModelId,
    [property: Id(2)] ModelStatus Status,
    [property: Id(3)] int RecentErrors,
    [property: Id(4)] DateTime? LastPredictionAt,
    [property: Id(5)] DateTime CheckedAt);

[GenerateSerializer]
public record LoadBalancingInfo(
    [property: Id(0)] string ModelId,
    [property: Id(1)] int InstanceCount,
    [property: Id(2)] int HealthyInstances,
    [property: Id(3)] double TotalRequestsPerMinute,
    [property: Id(4)] TimeSpan AverageProcessingTime,
    [property: Id(5)] List<InstanceMetrics> InstanceMetrics);

[GenerateSerializer]
public record InstanceMetrics(
    [property: Id(0)] string InstanceId,
    [property: Id(1)] double RequestsPerMinute,
    [property: Id(2)] TimeSpan AverageProcessingTime,
    [property: Id(3)] bool IsHealthy,
    [property: Id(4)] DateTime? LastPredictionAt);

[GenerateSerializer]
public record ModelPerformanceMetrics(
    [property: Id(0)] string ModelId,
    [property: Id(1)] TimeSpan Period,
    [property: Id(2)] int TotalRequests,
    [property: Id(3)] int SuccessfulRequests,
    [property: Id(4)] int FailedRequests,
    [property: Id(5)] double SuccessRate,
    [property: Id(6)] TimeSpan AverageProcessingTime,
    [property: Id(7)] double RequestsPerMinute,
    [property: Id(8)] int InstanceCount,
    [property: Id(9)] DateTime GeneratedAt);

// Training Types
[GenerateSerializer]
public record TrainingRequest(
    [property: Id(0)] string JobId,
    [property: Id(1)] string ModelType,
    [property: Id(2)] string DataPath,
    [property: Id(3)] Dictionary<string, object> Parameters,
    [property: Id(4)] string? ValidationDataPath = null);

[GenerateSerializer]
public record TrainingResult(
    [property: Id(0)] bool Success,
    [property: Id(1)] string JobId,
    [property: Id(2)] string? ModelPath = null,
    [property: Id(3)] Dictionary<string, double>? Metrics = null,
    [property: Id(4)] string? Error = null,
    [property: Id(5)] TimeSpan TrainingDuration = default,
    [property: Id(6)] DateTime CompletedAt = default);

[GenerateSerializer]
public record TrainingStatus(
    [property: Id(0)] string JobId,
    [property: Id(1)] TrainingJobStatus Status,
    [property: Id(2)] double Progress,
    [property: Id(3)] string? CurrentStep = null,
    [property: Id(4)] DateTime StartedAt = default,
    [property: Id(5)] TimeSpan ElapsedTime = default,
    [property: Id(6)] TimeSpan? EstimatedTimeRemaining = null);

[GenerateSerializer]
public record TrainingJob(
    [property: Id(0)] string JobId,
    [property: Id(1)] string ModelType,
    [property: Id(2)] TrainingJobStatus Status,
    [property: Id(3)] DateTime StartedAt,
    [property: Id(4)] DateTime? CompletedAt = null,
    [property: Id(5)] string? UserId = null);

// Enums
[GenerateSerializer]
public enum ModelStatus
{
    [Id(0)] NotLoaded,
    [Id(1)] Loading,
    [Id(2)] Loaded,
    [Id(3)] LoadFailed,
    [Id(4)] Unloaded,
    [Id(5)] Error
}

[GenerateSerializer]
public enum DeploymentStatus
{
    [Id(0)] Deploying,
    [Id(1)] Deployed,
    [Id(2)] Failed,
    [Id(3)] Scaling,
    [Id(4)] Undeploying
}

[GenerateSerializer]
public enum ScalingStrategy
{
    [Id(0)] Immediate,
    [Id(1)] Gradual,
    [Id(2)] Auto
}

[GenerateSerializer]
public enum TrainingJobStatus
{
    [Id(0)] Queued,
    [Id(1)] Running,
    [Id(2)] Completed,
    [Id(3)] Failed,
    [Id(4)] Cancelled
}

// Generic prediction output - customize based on your model
[GenerateSerializer]
public record PredictionOutput(
    [property: Id(0)] string Label,
    [property: Id(1)] float Score,
    [property: Id(2)] Dictionary<string, object>? AdditionalData = null);
```

## ASP.NET Core Integration

### Orleans ML Controller

```csharp
namespace DocumentProcessor.API.Controllers;

[ApiController]
[Route("api/ml")]
public class OrleansMLController : ControllerBase
{
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<OrleansMLController> _logger;

    public OrleansMLController(IClusterClient clusterClient, ILogger<OrleansMLController> logger)
    {
        _clusterClient = clusterClient;
        _logger = logger;
    }

    [HttpPost("models/{modelId}/predict")]
    public async Task<ActionResult<PredictionResponse>> Predict(
        string modelId,
        [FromBody] PredictionApiRequest request)
    {
        try
        {
            // Get model manager to find the best instance
            var modelManager = _clusterClient.GetGrain<IMLModelManagerGrain>("default");
            var loadBalancingInfo = await modelManager.GetLoadBalancingInfoAsync(modelId);
            
            if (loadBalancingInfo.HealthyInstances == 0)
            {
                return StatusCode(503, new PredictionResponse(
                    Success: false,
                    Error: "No healthy instances available for model",
                    RequestId: request.RequestId));
            }

            // Select instance with lowest load (simple round-robin could be used too)
            var selectedInstance = loadBalancingInfo.InstanceMetrics
                .Where(i => i.IsHealthy)
                .OrderBy(i => i.RequestsPerMinute)
                .First();

            var modelGrain = _clusterClient.GetGrain<IMLModelGrain>(selectedInstance.InstanceId);

            var predictionRequest = new PredictionRequest(
                RequestId: request.RequestId,
                Input: request.Input,
                Timeout: TimeSpan.FromSeconds(30),
                Metadata: request.Metadata);

            var result = await modelGrain.PredictAsync(predictionRequest);

            return Ok(new PredictionResponse(
                Success: result.Success,
                Predictions: result.Predictions?.ToList(),
                Error: result.Error,
                RequestId: result.RequestId,
                ProcessingTime: result.ProcessingTime,
                ModelVersion: result.ModelVersion,
                InstanceId: selectedInstance.InstanceId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Prediction failed for model {ModelId}, request {RequestId}", 
                modelId, request.RequestId);
            
            return StatusCode(500, new PredictionResponse(
                Success: false,
                Error: "Internal server error",
                RequestId: request.RequestId));
        }
    }

    [HttpPost("models/{modelId}/predict/batch")]
    public async Task<ActionResult<BatchPredictionResponse>> PredictBatch(
        string modelId,
        [FromBody] BatchPredictionApiRequest request)
    {
        try
        {
            var modelManager = _clusterClient.GetGrain<IMLModelManagerGrain>("default");
            var loadBalancingInfo = await modelManager.GetLoadBalancingInfoAsync(modelId);
            
            if (loadBalancingInfo.HealthyInstances == 0)
            {
                return StatusCode(503, new BatchPredictionResponse(
                    Success: false,
                    Error: "No healthy instances available for model",
                    RequestId: request.RequestId));
            }

            // For batch requests, distribute across multiple instances
            var healthyInstances = loadBalancingInfo.InstanceMetrics.Where(i => i.IsHealthy).ToList();
            var batchSize = (int)Math.Ceiling((double)request.Inputs.Count / healthyInstances.Count);
            
            var batchTasks = healthyInstances.Select(async (instance, index) =>
            {
                var startIndex = index * batchSize;
                var batchInputs = request.Inputs.Skip(startIndex).Take(batchSize).ToList();
                
                if (!batchInputs.Any())
                    return new BatchPredictionResult(Success: true, Predictions: new List<PredictionOutput>(), 
                        RequestId: $"{request.RequestId}-{index}", ProcessingTime: TimeSpan.Zero, ProcessedCount: 0);

                var modelGrain = _clusterClient.GetGrain<IMLModelGrain>(instance.InstanceId);
                var batchRequest = new BatchPredictionRequest(
                    RequestId: $"{request.RequestId}-{index}",
                    Inputs: batchInputs,
                    BatchSize: request.BatchSize,
                    Timeout: TimeSpan.FromMinutes(5));

                return await modelGrain.PredictBatchAsync(batchRequest);
            });

            var batchResults = await Task.WhenAll(batchTasks);
            
            var allPredictions = new List<PredictionOutput>();
            var allErrors = new List<string>();
            var totalProcessingTime = TimeSpan.Zero;
            var processedCount = 0;

            foreach (var batchResult in batchResults)
            {
                if (batchResult.Success && batchResult.Predictions != null)
                {
                    allPredictions.AddRange(batchResult.Predictions);
                    processedCount += batchResult.ProcessedCount;
                }
                
                if (batchResult.Errors != null)
                {
                    allErrors.AddRange(batchResult.Errors);
                }

                if (batchResult.ProcessingTime > totalProcessingTime)
                {
                    totalProcessingTime = batchResult.ProcessingTime; // Use max time
                }
            }

            var overallSuccess = allErrors.Count == 0 && batchResults.All(r => r.Success);

            return Ok(new BatchPredictionResponse(
                Success: overallSuccess,
                Predictions: allPredictions,
                RequestId: request.RequestId,
                ProcessingTime: totalProcessingTime,
                ProcessedCount: processedCount,
                TotalInputs: request.Inputs.Count,
                Errors: allErrors.Any() ? allErrors : null,
                InstancesUsed: healthyInstances.Count));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Batch prediction failed for model {ModelId}, request {RequestId}", 
                modelId, request.RequestId);
            
            return StatusCode(500, new BatchPredictionResponse(
                Success: false,
                Error: "Internal server error",
                RequestId: request.RequestId));
        }
    }

    [HttpPost("models/deploy")]
    public async Task<ActionResult<DeploymentResponse>> DeployModel([FromBody] DeployModelApiRequest request)
    {
        try
        {
            var modelManager = _clusterClient.GetGrain<IMLModelManagerGrain>("default");
            
            var deploymentRequest = new ModelDeploymentRequest(
                ModelId: request.ModelId,
                Version: request.Version,
                ModelPath: request.ModelPath,
                InstanceCount: request.InstanceCount,
                WarmupInputs: request.WarmupInputs);

            var result = await modelManager.DeployModelAsync(deploymentRequest);

            return Ok(new DeploymentResponse(
                Success: result.Success,
                ModelId: result.ModelId,
                Version: result.Version,
                InstanceCount: result.InstanceCount,
                InstanceIds: result.InstanceIds,
                Error: result.Error,
                DeployedAt: result.DeployedAt));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Model deployment failed for {ModelId}", request.ModelId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("models")]
    public async Task<ActionResult<ModelsListResponse>> GetModels()
    {
        try
        {
            var modelManager = _clusterClient.GetGrain<IMLModelManagerGrain>("default");
            var models = await modelManager.GetActiveModelsAsync();

            return Ok(new ModelsListResponse(
                Models: models,
                Count: models.Count,
                GeneratedAt: DateTime.UtcNow));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active models");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("models/{modelId}/metrics")]
    public async Task<ActionResult<ModelPerformanceResponse>> GetModelMetrics(
        string modelId, 
        [FromQuery] int periodHours = 1)
    {
        try
        {
            var modelManager = _clusterClient.GetGrain<IMLModelManagerGrain>("default");
            var metrics = await modelManager.GetPerformanceMetricsAsync(modelId, TimeSpan.FromHours(periodHours));

            return Ok(new ModelPerformanceResponse(
                ModelId: metrics.ModelId,
                Metrics: metrics,
                GeneratedAt: DateTime.UtcNow));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get metrics for model {ModelId}", modelId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("models/{modelId}/scale")]
    public async Task<ActionResult<ScalingResponse>> ScaleModel(
        string modelId,
        [FromBody] ScaleModelApiRequest request)
    {
        try
        {
            var modelManager = _clusterClient.GetGrain<IMLModelManagerGrain>("default");
            
            var scalingRequest = new ScalingRequest(
                TargetInstanceCount: request.TargetInstanceCount,
                Strategy: request.Strategy);

            var result = await modelManager.ScaleModelAsync(modelId, scalingRequest);

            return Ok(new ScalingResponse(
                Success: result.Success,
                ModelId: result.ModelId,
                PreviousInstanceCount: result.PreviousInstanceCount,
                NewInstanceCount: result.NewInstanceCount,
                Error: result.Error,
                ScaledAt: result.ScaledAt));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scale model {ModelId}", modelId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("models/{modelId}/health")]
    public async Task<ActionResult<ModelHealthResponse>> GetModelHealth(string modelId)
    {
        try
        {
            var modelManager = _clusterClient.GetGrain<IMLModelManagerGrain>("default");
            var loadBalancingInfo = await modelManager.GetLoadBalancingInfoAsync(modelId);

            var healthChecks = new List<InstanceHealthStatus>();

            foreach (var instance in loadBalancingInfo.InstanceMetrics)
            {
                var modelGrain = _clusterClient.GetGrain<IMLModelGrain>(instance.InstanceId);
                var health = await modelGrain.GetHealthStatusAsync();
                
                healthChecks.Add(new InstanceHealthStatus(
                    InstanceId: instance.InstanceId,
                    IsHealthy: health.IsHealthy,
                    Status: health.Status.ToString(),
                    RecentErrors: health.RecentErrors,
                    LastPredictionAt: health.LastPredictionAt,
                    CheckedAt: health.CheckedAt));
            }

            var overallHealth = healthChecks.All(h => h.IsHealthy);

            return Ok(new ModelHealthResponse(
                ModelId: modelId,
                IsHealthy: overallHealth,
                TotalInstances: loadBalancingInfo.InstanceCount,
                HealthyInstances: healthChecks.Count(h => h.IsHealthy),
                InstanceHealthChecks: healthChecks,
                CheckedAt: DateTime.UtcNow));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed for model {ModelId}", modelId);
            return StatusCode(500, "Internal server error");
        }
    }
}

// API Request/Response DTOs
public record PredictionApiRequest(
    string RequestId,
    object Input,
    Dictionary<string, object>? Metadata = null);

public record PredictionResponse(
    bool Success,
    List<PredictionOutput>? Predictions = null,
    string? Error = null,
    string RequestId = "",
    TimeSpan ProcessingTime = default,
    string? ModelVersion = null,
    string? InstanceId = null);

public record BatchPredictionApiRequest(
    string RequestId,
    List<object> Inputs,
    int? BatchSize = null);

public record BatchPredictionResponse(
    bool Success,
    List<PredictionOutput>? Predictions = null,
    string? Error = null,
    string RequestId = "",
    TimeSpan ProcessingTime = default,
    int ProcessedCount = 0,
    int TotalInputs = 0,
    List<string>? Errors = null,
    int InstancesUsed = 0);

public record DeployModelApiRequest(
    string ModelId,
    string Version,
    string ModelPath,
    int InstanceCount,
    List<object>? WarmupInputs = null);

public record DeploymentResponse(
    bool Success,
    string ModelId,
    string Version,
    int InstanceCount,
    List<string>? InstanceIds,
    string? Error,
    DateTime DeployedAt);

public record ModelsListResponse(
    List<ModelInfo> Models,
    int Count,
    DateTime GeneratedAt);

public record ModelPerformanceResponse(
    string ModelId,
    ModelPerformanceMetrics Metrics,
    DateTime GeneratedAt);

public record ScaleModelApiRequest(
    int TargetInstanceCount,
    ScalingStrategy Strategy = ScalingStrategy.Immediate);

public record ScalingResponse(
    bool Success,
    string ModelId,
    int PreviousInstanceCount,
    int NewInstanceCount,
    string? Error,
    DateTime ScaledAt);

public record ModelHealthResponse(
    string ModelId,
    bool IsHealthy,
    int TotalInstances,
    int HealthyInstances,
    List<InstanceHealthStatus> InstanceHealthChecks,
    DateTime CheckedAt);

public record InstanceHealthStatus(
    string InstanceId,
    bool IsHealthy,
    string Status,
    int RecentErrors,
    DateTime? LastPredictionAt,
    DateTime CheckedAt);
```

## Orleans Configuration

### Service Registration and Configuration

```csharp
namespace DocumentProcessor.Extensions;

public static class OrleansMLServiceCollectionExtensions
{
    public static IServiceCollection AddOrleansML(this IServiceCollection services, IConfiguration configuration)
    {
        // Register Orleans client
        services.AddOrleansClient(clientBuilder =>
        {
            clientBuilder
                .UseConnectionString(configuration.GetConnectionString("Orleans"))
                .ConfigureLogging(logging => logging.AddConsole());
        });

        // Register ML services
        services.AddScoped<IMLModelRepository, MLModelRepository>();
        services.AddScoped<IMLMetricsCollector, MLMetricsCollector>();
        
        // Register health checks
        services.AddHealthChecks()
            .AddCheck<OrleansMLHealthCheck>("orleans-ml");

        return services;
    }

    public static ISiloBuilder AddMLGrains(this ISiloBuilder siloBuilder)
    {
        return siloBuilder
            .AddGrain<MLModelGrain>()
            .AddGrain<MLModelManagerGrain>()
            .AddGrain<MLTrainingGrain>()
            .AddGrain<MLPipelineGrain>();
    }
}

// Orleans Silo Configuration
public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Configure Orleans Silo
        builder.Host.UseOrleans(siloBuilder =>
        {
            siloBuilder
                .UseConnectionString(builder.Configuration.GetConnectionString("Orleans"))
                .ConfigureLogging(logging => logging.AddConsole())
                .AddMLGrains();
        });

        // Add services
        builder.Services.AddOrleansML(builder.Configuration);
        builder.Services.AddControllers();

        var app = builder.Build();

        app.UseRouting();
        app.MapControllers();

        await app.RunAsync();
    }
}
```

**Usage**:

```csharp
// Deploy and use ML models with Orleans
var clusterClient = serviceProvider.GetRequiredService<IClusterClient>();

// Deploy a model
var modelManager = clusterClient.GetGrain<IMLModelManagerGrain>("default");
var deploymentRequest = new ModelDeploymentRequest(
    ModelId: "sentiment-classifier",
    Version: "v1.0",
    ModelPath: "/models/sentiment-v1.mlnet",
    InstanceCount: 3,
    WarmupInputs: new List<object> { "This is a test input" });

var deploymentResult = await modelManager.DeployModelAsync(deploymentRequest);
Console.WriteLine($"Deployment: {deploymentResult.Success}");

// Make predictions
var modelGrain = clusterClient.GetGrain<IMLModelGrain>("sentiment-classifier-instance-0");
var predictionRequest = new PredictionRequest(
    RequestId: Guid.NewGuid().ToString(),
    Input: new { Text = "This movie is amazing!" });

var prediction = await modelGrain.PredictAsync(predictionRequest);
Console.WriteLine($"Prediction: {prediction.Success}");

// Scale model
var scalingRequest = new ScalingRequest(TargetInstanceCount: 5);
var scalingResult = await modelManager.ScaleModelAsync("sentiment-classifier", scalingRequest);
Console.WriteLine($"Scaled from {scalingResult.PreviousInstanceCount} to {scalingResult.NewInstanceCount}");

// Get performance metrics
var metrics = await modelManager.GetPerformanceMetricsAsync("sentiment-classifier", TimeSpan.FromHours(1));
Console.WriteLine($"Success rate: {metrics.SuccessRate:P2}, Avg time: {metrics.AverageProcessingTime.TotalMilliseconds}ms");
```

**Notes**:

- **Distributed Architecture**: Orleans grains provide natural distribution and load balancing for ML models
- **Stateful Model Management**: Each grain maintains model state and metrics independently
- **Automatic Scaling**: Dynamic scaling of model instances based on load and performance requirements
- **Health Monitoring**: Comprehensive health checks and metrics collection at grain level
- **Load Balancing**: Intelligent routing to healthy instances with lowest load
- **Fault Tolerance**: Orleans provides automatic failover and recovery for grain instances

**Performance Considerations**: Orleans grains are optimized for high-throughput scenarios with automatic load balancing, state management, and efficient inter-grain communication patterns.
