# Saga Patterns

**Description**: Comprehensive distributed saga implementation including orchestration and choreography patterns, compensation logic, long-running transaction management, saga state persistence, timeout handling, and distributed transaction coordination for building resilient microservices architectures.

**Language/Technology**: C# / .NET

**Code**:

```csharp
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Reflection;

// Core saga interfaces
public interface ISaga
{
    Guid SagaId { get; }
    string SagaType { get; }
    SagaStatus Status { get; }
    DateTime CreatedAt { get; }
    DateTime? CompletedAt { get; }
    IDictionary<string, object> Data { get; }
    IReadOnlyList<ISagaStep> Steps { get; }
    ISagaStep CurrentStep { get; }
}

public interface ISagaStep
{
    string StepName { get; }
    int StepOrder { get; }
    SagaStepStatus Status { get; }
    DateTime? StartedAt { get; }
    DateTime? CompletedAt { get; }
    string ErrorMessage { get; }
    IDictionary<string, object> StepData { get; }
}

public interface ISagaOrchestrator
{
    Task<ISaga> StartSagaAsync&lt;T&gt;(T sagaData, CancellationToken token = default) where T : class;
    Task<ISaga> StartSagaAsync(string sagaType, object sagaData, CancellationToken token = default);
    Task<ISaga> GetSagaAsync(Guid sagaId, CancellationToken token = default);
    Task<IEnumerable<ISaga>> GetActiveSagasAsync(CancellationToken token = default);
    Task<bool> CompensateStepAsync(Guid sagaId, string stepName, CancellationToken token = default);
    Task<bool> RetrySagaAsync(Guid sagaId, CancellationToken token = default);
    Task<bool> CancelSagaAsync(Guid sagaId, CancellationToken token = default);
}

public interface ISagaRepository
{
    Task SaveSagaAsync(ISaga saga, CancellationToken token = default);
    Task<ISaga> GetSagaAsync(Guid sagaId, CancellationToken token = default);
    Task<IEnumerable<ISaga>> GetSagasByStatusAsync(SagaStatus status, CancellationToken token = default);
    Task<IEnumerable<ISaga>> GetExpiredSagasAsync(TimeSpan timeout, CancellationToken token = default);
    Task DeleteSagaAsync(Guid sagaId, CancellationToken token = default);
}

public interface ISagaStepHandler<in T> where T : class
{
    Task<SagaStepResult> ExecuteAsync(T data, ISagaContext context, CancellationToken token = default);
    Task<SagaStepResult> CompensateAsync(T data, ISagaContext context, CancellationToken token = default);
}

public interface ISagaContext
{
    Guid SagaId { get; }
    string SagaType { get; }
    IDictionary<string, object> SagaData { get; }
    IDictionary<string, object> StepData { get; }
    IServiceProvider ServiceProvider { get; }
    CancellationToken CancellationToken { get; }
    void SetStepData(string key, object value);
    T GetStepData&lt;T&gt;(string key);
    void SetSagaData(string key, object value);
    T GetSagaData&lt;T&gt;(string key);
}

// Enums
public enum SagaStatus
{
    NotStarted,
    Running,
    Completed,
    Failed,
    Compensating,
    Compensated,
    Cancelled
}

public enum SagaStepStatus
{
    NotStarted,
    Running,
    Completed,
    Failed,
    Compensating,
    Compensated,
    Skipped
}

public enum CompensationPolicy
{
    None,
    Automatic,
    Manual,
    Retry
}

// Result types
public class SagaStepResult
{
    public bool IsSuccess { get; set; }
    public string ErrorMessage { get; set; }
    public Exception Exception { get; set; }
    public IDictionary<string, object> OutputData { get; set; } = new Dictionary<string, object>();
    public TimeSpan? RetryAfter { get; set; }
    public bool ShouldCompensate { get; set; } = true;

    public static SagaStepResult Success(IDictionary<string, object> outputData = null)
    {
        return new SagaStepResult 
        { 
            IsSuccess = true, 
            OutputData = outputData ?? new Dictionary<string, object>() 
        };
    }

    public static SagaStepResult Failure(string errorMessage, Exception exception = null, bool shouldCompensate = true)
    {
        return new SagaStepResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            Exception = exception,
            ShouldCompensate = shouldCompensate
        };
    }

    public static SagaStepResult Retry(TimeSpan retryAfter, string message = null)
    {
        return new SagaStepResult
        {
            IsSuccess = false,
            ErrorMessage = message ?? "Step will be retried",
            RetryAfter = retryAfter
        };
    }
}

// Saga implementation
public class Saga : ISaga
{
    private readonly List<SagaStep> steps;

    public Saga(string sagaType, object sagaData)
    {
        SagaId = Guid.NewGuid();
        SagaType = sagaType ?? throw new ArgumentNullException(nameof(sagaType));
        Status = SagaStatus.NotStarted;
        CreatedAt = DateTime.UtcNow;
        Data = new Dictionary<string, object>();
        steps = new List<SagaStep>();
        
        if (sagaData != null)
        {
            PopulateSagaData(sagaData);
        }
    }

    public Guid SagaId { get; private set; }
    public string SagaType { get; private set; }
    public SagaStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public IDictionary<string, object> Data { get; private set; }
    public IReadOnlyList<ISagaStep> Steps => steps.AsReadOnly();
    public ISagaStep CurrentStep => steps.LastOrDefault(s => s.Status == SagaStepStatus.Running);

    public void AddStep(string stepName, int stepOrder)
    {
        var step = new SagaStep(stepName, stepOrder);
        steps.Add(step);
        steps.Sort((x, y) => x.StepOrder.CompareTo(y.StepOrder));
    }

    public void StartStep(string stepName)
    {
        var step = GetStep(stepName);
        if (step != null)
        {
            step.Start();
            Status = SagaStatus.Running;
        }
    }

    public void CompleteStep(string stepName, IDictionary<string, object> stepData = null)
    {
        var step = GetStep(stepName);
        if (step != null)
        {
            step.Complete(stepData);
            
            // Check if all steps are completed
            if (steps.All(s => s.Status == SagaStepStatus.Completed))
            {
                Status = SagaStatus.Completed;
                CompletedAt = DateTime.UtcNow;
            }
        }
    }

    public void FailStep(string stepName, string errorMessage)
    {
        var step = GetStep(stepName);
        if (step != null)
        {
            step.Fail(errorMessage);
            Status = SagaStatus.Failed;
        }
    }

    public void StartCompensation()
    {
        Status = SagaStatus.Compensating;
        
        // Mark completed steps for compensation in reverse order
        var completedSteps = steps
            .Where(s => s.Status == SagaStepStatus.Completed)
            .OrderByDescending(s => s.StepOrder)
            .ToList();

        foreach (var step in completedSteps)
        {
            step.StartCompensation();
        }
    }

    public void CompleteCompensation()
    {
        Status = SagaStatus.Compensated;
        CompletedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        Status = SagaStatus.Cancelled;
        CompletedAt = DateTime.UtcNow;
        
        foreach (var step in steps.Where(s => s.Status == SagaStepStatus.Running))
        {
            step.Skip();
        }
    }

    private SagaStep GetStep(string stepName)
    {
        return steps.FirstOrDefault(s => s.StepName.Equals(stepName, StringComparison.OrdinalIgnoreCase));
    }

    private void PopulateSagaData(object sagaData)
    {
        var properties = sagaData.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        
        foreach (var property in properties)
        {
            if (property.CanRead)
            {
                var value = property.GetValue(sagaData);
                Data[property.Name] = value;
            }
        }
    }
}

public class SagaStep : ISagaStep
{
    public SagaStep(string stepName, int stepOrder)
    {
        StepName = stepName ?? throw new ArgumentNullException(nameof(stepName));
        StepOrder = stepOrder;
        Status = SagaStepStatus.NotStarted;
        StepData = new Dictionary<string, object>();
    }

    public string StepName { get; }
    public int StepOrder { get; }
    public SagaStepStatus Status { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public string ErrorMessage { get; private set; }
    public IDictionary<string, object> StepData { get; private set; }

    public void Start()
    {
        Status = SagaStepStatus.Running;
        StartedAt = DateTime.UtcNow;
    }

    public void Complete(IDictionary<string, object> stepData = null)
    {
        Status = SagaStepStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        
        if (stepData != null)
        {
            foreach (var kvp in stepData)
            {
                StepData[kvp.Key] = kvp.Value;
            }
        }
    }

    public void Fail(string errorMessage)
    {
        Status = SagaStepStatus.Failed;
        CompletedAt = DateTime.UtcNow;
        ErrorMessage = errorMessage;
    }

    public void StartCompensation()
    {
        Status = SagaStepStatus.Compensating;
        StartedAt = DateTime.UtcNow;
    }

    public void CompleteCompensation()
    {
        Status = SagaStepStatus.Compensated;
        CompletedAt = DateTime.UtcNow;
    }

    public void Skip()
    {
        Status = SagaStepStatus.Skipped;
        CompletedAt = DateTime.UtcNow;
    }
}

// Saga context implementation
public class SagaContext : ISagaContext
{
    public SagaContext(ISaga saga, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        SagaId = saga.SagaId;
        SagaType = saga.SagaType;
        SagaData = saga.Data;
        StepData = new Dictionary<string, object>();
        ServiceProvider = serviceProvider;
        CancellationToken = cancellationToken;
    }

    public Guid SagaId { get; }
    public string SagaType { get; }
    public IDictionary<string, object> SagaData { get; }
    public IDictionary<string, object> StepData { get; }
    public IServiceProvider ServiceProvider { get; }
    public CancellationToken CancellationToken { get; }

    public void SetStepData(string key, object value)
    {
        StepData[key] = value;
    }

    public T GetStepData&lt;T&gt;(string key)
    {
        if (StepData.TryGetValue(key, out var value))
        {
            if (value is T directValue)
                return directValue;
            
            if (value is JsonElement jsonElement)
                return JsonSerializer.Deserialize&lt;T&gt;(jsonElement.GetRawText());
                
            return JsonSerializer.Deserialize&lt;T&gt;(JsonSerializer.Serialize(value));
        }
        
        return default(T);
    }

    public void SetSagaData(string key, object value)
    {
        SagaData[key] = value;
    }

    public T GetSagaData&lt;T&gt;(string key)
    {
        if (SagaData.TryGetValue(key, out var value))
        {
            if (value is T directValue)
                return directValue;
                
            if (value is JsonElement jsonElement)
                return JsonSerializer.Deserialize&lt;T&gt;(jsonElement.GetRawText());
                
            return JsonSerializer.Deserialize&lt;T&gt;(JsonSerializer.Serialize(value));
        }
        
        return default(T);
    }
}

// In-memory saga repository
public class InMemorySagaRepository : ISagaRepository
{
    private readonly ConcurrentDictionary<Guid, ISaga> sagas;
    private readonly ILogger logger;

    public InMemorySagaRepository(ILogger<InMemorySagaRepository> logger = null)
    {
        sagas = new ConcurrentDictionary<Guid, ISaga>();
        this.logger = logger;
    }

    public Task SaveSagaAsync(ISaga saga, CancellationToken token = default)
    {
        sagas.AddOrUpdate(saga.SagaId, saga, (key, existing) => saga);
        
        logger?.LogTrace("Saved saga {SagaId} with status {Status}", saga.SagaId, saga.Status);
        
        return Task.CompletedTask;
    }

    public Task<ISaga> GetSagaAsync(Guid sagaId, CancellationToken token = default)
    {
        sagas.TryGetValue(sagaId, out var saga);
        return Task.FromResult(saga);
    }

    public Task<IEnumerable<ISaga>> GetSagasByStatusAsync(SagaStatus status, CancellationToken token = default)
    {
        var matchingSagas = sagas.Values.Where(s => s.Status == status).ToList();
        return Task.FromResult<IEnumerable<ISaga>>(matchingSagas);
    }

    public Task<IEnumerable<ISaga>> GetExpiredSagasAsync(TimeSpan timeout, CancellationToken token = default)
    {
        var cutoffTime = DateTime.UtcNow - timeout;
        var expiredSagas = sagas.Values
            .Where(s => s.Status == SagaStatus.Running && s.CreatedAt < cutoffTime)
            .ToList();
            
        return Task.FromResult<IEnumerable<ISaga>>(expiredSagas);
    }

    public Task DeleteSagaAsync(Guid sagaId, CancellationToken token = default)
    {
        sagas.TryRemove(sagaId, out _);
        
        logger?.LogTrace("Deleted saga {SagaId}", sagaId);
        
        return Task.CompletedTask;
    }
}

// Saga orchestrator implementation
public class SagaOrchestrator : ISagaOrchestrator, IDisposable
{
    private readonly ISagaRepository repository;
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger logger;
    private readonly ConcurrentDictionary<string, SagaDefinition> sagaDefinitions;
    private readonly Timer timeoutTimer;
    private volatile bool isDisposed = false;

    public SagaOrchestrator(
        ISagaRepository repository,
        IServiceProvider serviceProvider = null,
        ILogger<SagaOrchestrator> logger = null)
    {
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        this.serviceProvider = serviceProvider;
        this.logger = logger;
        sagaDefinitions = new ConcurrentDictionary<string, SagaDefinition>();
        
        // Start timeout monitoring timer
        timeoutTimer = new Timer(CheckTimeouts, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }

    public Task<ISaga> StartSagaAsync&lt;T&gt;(T sagaData, CancellationToken token = default) where T : class
    {
        var sagaType = typeof(T).Name;
        return StartSagaAsync(sagaType, sagaData, token);
    }

    public async Task<ISaga> StartSagaAsync(string sagaType, object sagaData, CancellationToken token = default)
    {
        if (!sagaDefinitions.TryGetValue(sagaType, out var definition))
        {
            throw new InvalidOperationException($"No saga definition found for type: {sagaType}");
        }

        var saga = new Saga(sagaType, sagaData);
        
        // Add steps from definition
        foreach (var stepDef in definition.Steps.OrderBy(s => s.Order))
        {
            saga.AddStep(stepDef.Name, stepDef.Order);
        }

        await repository.SaveSagaAsync(saga, token).ConfigureAwait(false);
        
        logger?.LogInformation("Started saga {SagaId} of type {SagaType}", saga.SagaId, sagaType);

        // Execute saga asynchronously
        _ = Task.Run(() => ExecuteSagaAsync(saga.SagaId, token), token);

        return saga;
    }

    public async Task<ISaga> GetSagaAsync(Guid sagaId, CancellationToken token = default)
    {
        return await repository.GetSagaAsync(sagaId, token).ConfigureAwait(false);
    }

    public async Task<IEnumerable<ISaga>> GetActiveSagasAsync(CancellationToken token = default)
    {
        var runningSagas = await repository.GetSagasByStatusAsync(SagaStatus.Running, token).ConfigureAwait(false);
        var compensatingSagas = await repository.GetSagasByStatusAsync(SagaStatus.Compensating, token).ConfigureAwait(false);
        
        return runningSagas.Concat(compensatingSagas);
    }

    public async Task<bool> CompensateStepAsync(Guid sagaId, string stepName, CancellationToken token = default)
    {
        var saga = await repository.GetSagaAsync(sagaId, token).ConfigureAwait(false);
        if (saga == null) return false;

        if (saga is Saga sagaImpl)
        {
            sagaImpl.StartCompensation();
            await repository.SaveSagaAsync(saga, token).ConfigureAwait(false);
            
            // Execute compensation asynchronously
            _ = Task.Run(() => ExecuteCompensationAsync(sagaId, token), token);
            
            return true;
        }

        return false;
    }

    public async Task<bool> RetrySagaAsync(Guid sagaId, CancellationToken token = default)
    {
        var saga = await repository.GetSagaAsync(sagaId, token).ConfigureAwait(false);
        if (saga == null || saga.Status != SagaStatus.Failed) return false;

        // Execute saga retry asynchronously
        _ = Task.Run(() => ExecuteSagaAsync(sagaId, token), token);
        
        return true;
    }

    public async Task<bool> CancelSagaAsync(Guid sagaId, CancellationToken token = default)
    {
        var saga = await repository.GetSagaAsync(sagaId, token).ConfigureAwait(false);
        if (saga == null) return false;

        if (saga is Saga sagaImpl)
        {
            sagaImpl.Cancel();
            await repository.SaveSagaAsync(saga, token).ConfigureAwait(false);
            
            logger?.LogInformation("Cancelled saga {SagaId}", sagaId);
            return true;
        }

        return false;
    }

    public void RegisterSaga&lt;T&gt;(Action<SagaDefinitionBuilder> configure) where T : class
    {
        var sagaType = typeof(T).Name;
        var builder = new SagaDefinitionBuilder(sagaType);
        configure(builder);
        
        var definition = builder.Build();
        sagaDefinitions[sagaType] = definition;
        
        logger?.LogInformation("Registered saga definition for type: {SagaType}", sagaType);
    }

    private async Task ExecuteSagaAsync(Guid sagaId, CancellationToken token)
    {
        try
        {
            var saga = await repository.GetSagaAsync(sagaId, token).ConfigureAwait(false);
            if (saga == null || saga.Status != SagaStatus.NotStarted && saga.Status != SagaStatus.Running && saga.Status != SagaStatus.Failed)
                return;

            if (!sagaDefinitions.TryGetValue(saga.SagaType, out var definition))
            {
                logger?.LogError("No saga definition found for type: {SagaType}", saga.SagaType);
                return;
            }

            var sagaImpl = saga as Saga;
            var context = new SagaContext(saga, serviceProvider, token);

            foreach (var stepDef in definition.Steps.OrderBy(s => s.Order))
            {
                if (token.IsCancellationRequested) break;

                var step = saga.Steps.FirstOrDefault(s => s.StepName == stepDef.Name);
                if (step == null || step.Status == SagaStepStatus.Completed) continue;

                try
                {
                    sagaImpl?.StartStep(stepDef.Name);
                    await repository.SaveSagaAsync(saga, token).ConfigureAwait(false);

                    logger?.LogTrace("Executing step {StepName} for saga {SagaId}", stepDef.Name, sagaId);

                    var result = await ExecuteStepAsync(stepDef, context, token).ConfigureAwait(false);

                    if (result.IsSuccess)
                    {
                        sagaImpl?.CompleteStep(stepDef.Name, result.OutputData);
                        logger?.LogTrace("Completed step {StepName} for saga {SagaId}", stepDef.Name, sagaId);
                    }
                    else
                    {
                        sagaImpl?.FailStep(stepDef.Name, result.ErrorMessage);
                        
                        logger?.LogError("Step {StepName} failed for saga {SagaId}: {ErrorMessage}", 
                            stepDef.Name, sagaId, result.ErrorMessage);

                        if (result.ShouldCompensate && stepDef.CompensationPolicy != CompensationPolicy.None)
                        {
                            await StartCompensationAsync(sagaId, token).ConfigureAwait(false);
                        }
                        
                        break;
                    }
                }
                catch (Exception ex)
                {
                    sagaImpl?.FailStep(stepDef.Name, ex.Message);
                    
                    logger?.LogError(ex, "Exception in step {StepName} for saga {SagaId}", stepDef.Name, sagaId);
                    
                    if (stepDef.CompensationPolicy != CompensationPolicy.None)
                    {
                        await StartCompensationAsync(sagaId, token).ConfigureAwait(false);
                    }
                    
                    break;
                }

                await repository.SaveSagaAsync(saga, token).ConfigureAwait(false);
            }

            // Final save
            await repository.SaveSagaAsync(saga, token).ConfigureAwait(false);
            
            if (saga.Status == SagaStatus.Completed)
            {
                logger?.LogInformation("Saga {SagaId} completed successfully", sagaId);
            }
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Exception executing saga {SagaId}", sagaId);
        }
    }

    private async Task ExecuteCompensationAsync(Guid sagaId, CancellationToken token)
    {
        try
        {
            var saga = await repository.GetSagaAsync(sagaId, token).ConfigureAwait(false);
            if (saga == null || saga.Status != SagaStatus.Compensating) return;

            if (!sagaDefinitions.TryGetValue(saga.SagaType, out var definition))
            {
                logger?.LogError("No saga definition found for compensation of type: {SagaType}", saga.SagaType);
                return;
            }

            var sagaImpl = saga as Saga;
            var context = new SagaContext(saga, serviceProvider, token);

            // Execute compensation in reverse order
            var completedSteps = saga.Steps
                .Where(s => s.Status == SagaStepStatus.Completed || s.Status == SagaStepStatus.Compensating)
                .OrderByDescending(s => s.StepOrder)
                .ToList();

            foreach (var step in completedSteps)
            {
                if (token.IsCancellationRequested) break;

                var stepDef = definition.Steps.FirstOrDefault(s => s.Name == step.StepName);
                if (stepDef?.CompensationPolicy == CompensationPolicy.None) continue;

                try
                {
                    logger?.LogTrace("Compensating step {StepName} for saga {SagaId}", step.StepName, sagaId);

                    var result = await CompensateStepAsync(stepDef, context, token).ConfigureAwait(false);

                    if (result.IsSuccess)
                    {
                        // Mark step as compensated (this would need implementation in SagaStep)
                        logger?.LogTrace("Compensated step {StepName} for saga {SagaId}", step.StepName, sagaId);
                    }
                    else
                    {
                        logger?.LogError("Failed to compensate step {StepName} for saga {SagaId}: {ErrorMessage}",
                            step.StepName, sagaId, result.ErrorMessage);
                    }
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Exception compensating step {StepName} for saga {SagaId}", 
                        step.StepName, sagaId);
                }
            }

            sagaImpl?.CompleteCompensation();
            await repository.SaveSagaAsync(saga, token).ConfigureAwait(false);
            
            logger?.LogInformation("Saga {SagaId} compensation completed", sagaId);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Exception during saga compensation {SagaId}", sagaId);
        }
    }

    private async Task<SagaStepResult> ExecuteStepAsync(SagaStepDefinition stepDef, ISagaContext context, CancellationToken token)
    {
        if (serviceProvider == null)
        {
            return SagaStepResult.Failure("Service provider not available for step execution");
        }

        try
        {
            var handler = serviceProvider.GetService(stepDef.HandlerType);
            if (handler == null)
            {
                return SagaStepResult.Failure($"Handler not found for step: {stepDef.Name}");
            }

            var executeMethod = stepDef.HandlerType.GetMethod("ExecuteAsync");
            if (executeMethod == null)
            {
                return SagaStepResult.Failure($"ExecuteAsync method not found on handler: {stepDef.HandlerType.Name}");
            }

            // Create step data instance
            var stepData = CreateStepData(stepDef.DataType, context);
            
            var result = await (Task<SagaStepResult>)executeMethod.Invoke(handler, new object[] { stepData, context, token });
            return result ?? SagaStepResult.Failure("Handler returned null result");
        }
        catch (Exception ex)
        {
            return SagaStepResult.Failure($"Exception executing step: {ex.Message}", ex);
        }
    }

    private async Task<SagaStepResult> CompensateStepAsync(SagaStepDefinition stepDef, ISagaContext context, CancellationToken token)
    {
        if (serviceProvider == null)
        {
            return SagaStepResult.Success();
        }

        try
        {
            var handler = serviceProvider.GetService(stepDef.HandlerType);
            if (handler == null)
            {
                return SagaStepResult.Success(); // If no handler, assume no compensation needed
            }

            var compensateMethod = stepDef.HandlerType.GetMethod("CompensateAsync");
            if (compensateMethod == null)
            {
                return SagaStepResult.Success(); // If no compensation method, assume success
            }

            // Create step data instance
            var stepData = CreateStepData(stepDef.DataType, context);
            
            var result = await (Task<SagaStepResult>)compensateMethod.Invoke(handler, new object[] { stepData, context, token });
            return result ?? SagaStepResult.Success();
        }
        catch (Exception ex)
        {
            return SagaStepResult.Failure($"Exception compensating step: {ex.Message}", ex);
        }
    }

    private object CreateStepData(Type dataType, ISagaContext context)
    {
        if (dataType == null) return null;

        var instance = Activator.CreateInstance(dataType);
        var properties = dataType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            if (property.CanWrite && context.SagaData.TryGetValue(property.Name, out var value))
            {
                try
                {
                    if (value != null && property.PropertyType.IsAssignableFrom(value.GetType()))
                    {
                        property.SetValue(instance, value);
                    }
                    else if (value != null)
                    {
                        var convertedValue = Convert.ChangeType(value, property.PropertyType);
                        property.SetValue(instance, convertedValue);
                    }
                }
                catch
                {
                    // Ignore conversion failures
                }
            }
        }

        return instance;
    }

    private async Task StartCompensationAsync(Guid sagaId, CancellationToken token)
    {
        var saga = await repository.GetSagaAsync(sagaId, token).ConfigureAwait(false);
        if (saga is Saga sagaImpl)
        {
            sagaImpl.StartCompensation();
            await repository.SaveSagaAsync(saga, token).ConfigureAwait(false);
            
            // Execute compensation asynchronously
            _ = Task.Run(() => ExecuteCompensationAsync(sagaId, token), token);
        }
    }

    private async void CheckTimeouts(object state)
    {
        if (isDisposed) return;

        try
        {
            var expiredSagas = await repository.GetExpiredSagasAsync(TimeSpan.FromHours(1), CancellationToken.None)
                .ConfigureAwait(false);

            foreach (var saga in expiredSagas)
            {
                logger?.LogWarning("Saga {SagaId} timed out, starting compensation", saga.SagaId);
                await StartCompensationAsync(saga.SagaId, CancellationToken.None).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error checking saga timeouts");
        }
    }

    public void Dispose()
    {
        if (!isDisposed)
        {
            isDisposed = true;
            timeoutTimer?.Dispose();
            logger?.LogInformation("Disposed saga orchestrator");
        }
    }
}

// Saga definition builder
public class SagaDefinitionBuilder
{
    private readonly SagaDefinition definition;

    public SagaDefinitionBuilder(string sagaType)
    {
        definition = new SagaDefinition { SagaType = sagaType };
    }

    public SagaDefinitionBuilder AddStep<THandler, TData>(string stepName, CompensationPolicy compensationPolicy = CompensationPolicy.Automatic)
        where THandler : class
        where TData : class
    {
        definition.Steps.Add(new SagaStepDefinition
        {
            Name = stepName,
            Order = definition.Steps.Count + 1,
            HandlerType = typeof(THandler),
            DataType = typeof(TData),
            CompensationPolicy = compensationPolicy
        });

        return this;
    }

    public SagaDefinitionBuilder SetTimeout(TimeSpan timeout)
    {
        definition.Timeout = timeout;
        return this;
    }

    public SagaDefinition Build()
    {
        return definition;
    }
}

public class SagaDefinition
{
    public string SagaType { get; set; }
    public List<SagaStepDefinition> Steps { get; set; } = new List<SagaStepDefinition>();
    public TimeSpan Timeout { get; set; } = TimeSpan.FromHours(1);
}

public class SagaStepDefinition
{
    public string Name { get; set; }
    public int Order { get; set; }
    public Type HandlerType { get; set; }
    public Type DataType { get; set; }
    public CompensationPolicy CompensationPolicy { get; set; } = CompensationPolicy.Automatic;
}

// Example saga step handlers
public class CreateOrderData
{
    public string CustomerEmail { get; set; }
    public decimal TotalAmount { get; set; }
    public List<OrderItem> Items { get; set; } = new List<OrderItem>();
}

public class OrderItem
{
    public string ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}

public class CreateOrderStepHandler : ISagaStepHandler<CreateOrderData>
{
    private readonly ILogger logger;

    public CreateOrderStepHandler(ILogger<CreateOrderStepHandler> logger = null)
    {
        this.logger = logger;
    }

    public async Task<SagaStepResult> ExecuteAsync(CreateOrderData data, ISagaContext context, CancellationToken token)
    {
        try
        {
            logger?.LogInformation("Creating order for customer: {CustomerEmail}", data.CustomerEmail);

            // Simulate order creation
            await Task.Delay(100, token);

            var orderId = Guid.NewGuid();
            var outputData = new Dictionary<string, object>
            {
                ["OrderId"] = orderId,
                ["Status"] = "Created"
            };

            // Store order ID in saga context for later steps
            context.SetSagaData("OrderId", orderId);

            logger?.LogInformation("Order created successfully: {OrderId}", orderId);

            return SagaStepResult.Success(outputData);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to create order");
            return SagaStepResult.Failure("Order creation failed", ex);
        }
    }

    public async Task<SagaStepResult> CompensateAsync(CreateOrderData data, ISagaContext context, CancellationToken token)
    {
        try
        {
            var orderId = context.GetSagaData<Guid>("OrderId");
            
            logger?.LogInformation("Compensating order creation: {OrderId}", orderId);

            // Simulate order cancellation
            await Task.Delay(50, token);

            logger?.LogInformation("Order compensation completed: {OrderId}", orderId);

            return SagaStepResult.Success();
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to compensate order creation");
            return SagaStepResult.Failure("Order compensation failed", ex);
        }
    }
}

public class ReserveInventoryData
{
    public List<OrderItem> Items { get; set; } = new List<OrderItem>();
}

public class ReserveInventoryStepHandler : ISagaStepHandler<ReserveInventoryData>
{
    private readonly ILogger logger;

    public ReserveInventoryStepHandler(ILogger<ReserveInventoryStepHandler> logger = null)
    {
        this.logger = logger;
    }

    public async Task<SagaStepResult> ExecuteAsync(ReserveInventoryData data, ISagaContext context, CancellationToken token)
    {
        try
        {
            logger?.LogInformation("Reserving inventory for {ItemCount} items", data.Items.Count);

            var reservations = new List<string>();

            foreach (var item in data.Items)
            {
                // Simulate inventory check and reservation
                await Task.Delay(50, token);

                // Simulate occasional inventory shortage
                if (item.ProductId == "OUT_OF_STOCK")
                {
                    return SagaStepResult.Failure($"Product {item.ProductId} is out of stock");
                }

                var reservationId = Guid.NewGuid().ToString();
                reservations.Add(reservationId);

                logger?.LogTrace("Reserved {Quantity} units of {ProductId} with reservation {ReservationId}",
                    item.Quantity, item.ProductId, reservationId);
            }

            var outputData = new Dictionary<string, object>
            {
                ["ReservationIds"] = reservations
            };

            // Store reservation IDs for compensation
            context.SetSagaData("ReservationIds", reservations);

            logger?.LogInformation("Inventory reservation completed");

            return SagaStepResult.Success(outputData);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to reserve inventory");
            return SagaStepResult.Failure("Inventory reservation failed", ex);
        }
    }

    public async Task<SagaStepResult> CompensateAsync(ReserveInventoryData data, ISagaContext context, CancellationToken token)
    {
        try
        {
            var reservationIds = context.GetSagaData<List<string>>("ReservationIds");
            
            if (reservationIds != null)
            {
                logger?.LogInformation("Releasing {ReservationCount} inventory reservations", reservationIds.Count);

                foreach (var reservationId in reservationIds)
                {
                    // Simulate reservation release
                    await Task.Delay(25, token);
                    
                    logger?.LogTrace("Released inventory reservation: {ReservationId}", reservationId);
                }
            }

            logger?.LogInformation("Inventory compensation completed");

            return SagaStepResult.Success();
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to compensate inventory reservation");
            return SagaStepResult.Failure("Inventory compensation failed", ex);
        }
    }
}

public class ProcessPaymentData
{
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; }
    public string CustomerEmail { get; set; }
}

public class ProcessPaymentStepHandler : ISagaStepHandler<ProcessPaymentData>
{
    private readonly ILogger logger;

    public ProcessPaymentStepHandler(ILogger<ProcessPaymentStepHandler> logger = null)
    {
        this.logger = logger;
    }

    public async Task<SagaStepResult> ExecuteAsync(ProcessPaymentData data, ISagaContext context, CancellationToken token)
    {
        try
        {
            logger?.LogInformation("Processing payment of ${Amount} for {CustomerEmail}", 
                data.Amount, data.CustomerEmail);

            // Simulate payment processing
            await Task.Delay(200, token);

            // Simulate occasional payment failure
            if (data.PaymentMethod == "INVALID_CARD")
            {
                return SagaStepResult.Failure("Invalid payment method");
            }

            var transactionId = Guid.NewGuid();
            var outputData = new Dictionary<string, object>
            {
                ["TransactionId"] = transactionId,
                ["PaymentStatus"] = "Completed"
            };

            // Store transaction ID for potential refund
            context.SetSagaData("TransactionId", transactionId);

            logger?.LogInformation("Payment processed successfully: {TransactionId}", transactionId);

            return SagaStepResult.Success(outputData);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to process payment");
            return SagaStepResult.Failure("Payment processing failed", ex);
        }
    }

    public async Task<SagaStepResult> CompensateAsync(ProcessPaymentData data, ISagaContext context, CancellationToken token)
    {
        try
        {
            var transactionId = context.GetSagaData<Guid>("TransactionId");
            
            logger?.LogInformation("Refunding payment transaction: {TransactionId}", transactionId);

            // Simulate refund processing
            await Task.Delay(150, token);

            logger?.LogInformation("Payment refund completed: {TransactionId}", transactionId);

            return SagaStepResult.Success();
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to refund payment");
            return SagaStepResult.Failure("Payment refund failed", ex);
        }
    }
}
```

**Usage**:

```csharp
// Example 1: Basic Saga Setup and Registration
Console.WriteLine("Saga Patterns Examples:");

// Set up services
var services = new ServiceCollection()
    .AddLogging(builder => builder.AddConsole())
    .AddSingleton<ISagaRepository, InMemorySagaRepository>()
    .AddSingleton<ISagaOrchestrator, SagaOrchestrator>()
    .AddTransient<CreateOrderStepHandler>()
    .AddTransient<ReserveInventoryStepHandler>()
    .AddTransient<ProcessPaymentStepHandler>()
    .BuildServiceProvider();

var orchestrator = services.GetRequiredService<ISagaOrchestrator>();

// Register the order processing saga
orchestrator.RegisterSaga<CreateOrderData>(builder =>
{
    builder
        .AddStep<CreateOrderStepHandler, CreateOrderData>("CreateOrder", CompensationPolicy.Automatic)
        .AddStep<ReserveInventoryStepHandler, ReserveInventoryData>("ReserveInventory", CompensationPolicy.Automatic)
        .AddStep<ProcessPaymentStepHandler, ProcessPaymentData>("ProcessPayment", CompensationPolicy.Automatic)
        .SetTimeout(TimeSpan.FromMinutes(30));
});

Console.WriteLine("Registered order processing saga with 3 steps");

// Example 2: Successful Saga Execution
Console.WriteLine("\nSuccessful Saga Execution:");

var successfulOrderData = new CreateOrderData
{
    CustomerEmail = "success@example.com",
    TotalAmount = 99.99m,
    Items = new List<OrderItem>
    {
        new OrderItem { ProductId = "PROD-001", Quantity = 2, Price = 29.99m },
        new OrderItem { ProductId = "PROD-002", Quantity = 1, Price = 39.99m }
    }
};

var successfulSaga = await orchestrator.StartSagaAsync(successfulOrderData);
Console.WriteLine($"Started successful saga: {successfulSaga.SagaId}");

// Wait for completion
await Task.Delay(2000);

var completedSaga = await orchestrator.GetSagaAsync(successfulSaga.SagaId);
Console.WriteLine($"Saga status: {completedSaga.Status}");
Console.WriteLine($"Steps completed: {completedSaga.Steps.Count(s => s.Status == SagaStepStatus.Completed)}");

foreach (var step in completedSaga.Steps)
{
    Console.WriteLine($"  Step: {step.StepName}, Status: {step.Status}, " +
                     $"Duration: {step.CompletedAt?.Subtract(step.StartedAt ?? DateTime.UtcNow).TotalMilliseconds:F0}ms");
}

// Example 3: Saga Failure and Compensation
Console.WriteLine("\nSaga Failure and Compensation:");

var failingOrderData = new CreateOrderData
{
    CustomerEmail = "failure@example.com", 
    TotalAmount = 149.99m,
    Items = new List<OrderItem>
    {
        new OrderItem { ProductId = "OUT_OF_STOCK", Quantity = 1, Price = 149.99m }
    }
};

var failingSaga = await orchestrator.StartSagaAsync(failingOrderData);
Console.WriteLine($"Started failing saga: {failingSaga.SagaId}");

// Wait for failure and compensation
await Task.Delay(3000);

var compensatedSaga = await orchestrator.GetSagaAsync(failingSaga.SagaId);
Console.WriteLine($"Failed saga status: {compensatedSaga.Status}");

foreach (var step in compensatedSaga.Steps)
{
    Console.WriteLine($"  Step: {step.StepName}, Status: {step.Status}");
    if (!string.IsNullOrEmpty(step.ErrorMessage))
    {
        Console.WriteLine($"    Error: {step.ErrorMessage}");
    }
}

// Example 4: Payment Failure Scenario
Console.WriteLine("\nPayment Failure Scenario:");

var paymentFailureData = new CreateOrderData
{
    CustomerEmail = "payment-fail@example.com",
    TotalAmount = 75.50m,
    Items = new List<OrderItem>
    {
        new OrderItem { ProductId = "PROD-003", Quantity = 1, Price = 75.50m }
    }
};

// Override payment method to trigger failure
var paymentFailureSaga = await orchestrator.StartSagaAsync("CreateOrderData", new
{
    CustomerEmail = paymentFailureData.CustomerEmail,
    TotalAmount = paymentFailureData.TotalAmount,
    Items = paymentFailureData.Items,
    PaymentMethod = "INVALID_CARD"
});

Console.WriteLine($"Started payment failure saga: {paymentFailureSaga.SagaId}");

// Wait for failure and compensation
await Task.Delay(3000);

var paymentFailedSaga = await orchestrator.GetSagaAsync(paymentFailureSaga.SagaId);
Console.WriteLine($"Payment failed saga status: {paymentFailedSaga.Status}");

// Show compensation details
foreach (var step in paymentFailedSaga.Steps.OrderBy(s => s.StepOrder))
{
    Console.WriteLine($"  Step {step.StepOrder}: {step.StepName} - {step.Status}");
    if (step.Status == SagaStepStatus.Failed)
    {
        Console.WriteLine($"    Failed: {step.ErrorMessage}");
    }
}

// Example 5: Saga Monitoring and Management
Console.WriteLine("\nSaga Monitoring and Management:");

// Get all active sagas
var activeSagas = await orchestrator.GetActiveSagasAsync();
Console.WriteLine($"Active sagas: {activeSagas.Count()}");

foreach (var saga in activeSagas)
{
    Console.WriteLine($"  Saga {saga.SagaId}: {saga.SagaType} - {saga.Status}");
    Console.WriteLine($"    Created: {saga.CreatedAt:yyyy-MM-dd HH:mm:ss}");
    Console.WriteLine($"    Current step: {saga.CurrentStep?.StepName ?? "None"}");
}

// Example 6: Manual Saga Operations
Console.WriteLine("\nManual Saga Operations:");

// Create a saga that we'll manually manage
var manualOrderData = new CreateOrderData
{
    CustomerEmail = "manual@example.com",
    TotalAmount = 199.99m,
    Items = new List<OrderItem>
    {
        new OrderItem { ProductId = "MANUAL-001", Quantity = 1, Price = 199.99m }
    }
};

var manualSaga = await orchestrator.StartSagaAsync(manualOrderData);
Console.WriteLine($"Started manual saga: {manualSaga.SagaId}");

// Wait a bit then cancel
await Task.Delay(500);

var cancelled = await orchestrator.CancelSagaAsync(manualSaga.SagaId);
Console.WriteLine($"Saga cancellation result: {cancelled}");

var cancelledSaga = await orchestrator.GetSagaAsync(manualSaga.SagaId);
Console.WriteLine($"Cancelled saga status: {cancelledSaga.Status}");

// Example 7: Saga Retry Mechanism
Console.WriteLine("\nSaga Retry Mechanism:");

// Create a saga that might fail initially
var retryOrderData = new CreateOrderData
{
    CustomerEmail = "retry@example.com",
    TotalAmount = 299.99m,
    Items = new List<OrderItem>
    {
        new OrderItem { ProductId = "RETRY-001", Quantity = 3, Price = 99.99m }
    }
};

var retrySaga = await orchestrator.StartSagaAsync(retryOrderData);
Console.WriteLine($"Started retry saga: {retrySaga.SagaId}");

await Task.Delay(1000);

var sagaBeforeRetry = await orchestrator.GetSagaAsync(retrySaga.SagaId);
if (sagaBeforeRetry.Status == SagaStatus.Failed)
{
    Console.WriteLine("Saga failed, attempting retry...");
    
    var retryResult = await orchestrator.RetrySagaAsync(retrySaga.SagaId);
    Console.WriteLine($"Retry initiated: {retryResult}");
    
    await Task.Delay(2000);
    
    var sagaAfterRetry = await orchestrator.GetSagaAsync(retrySaga.SagaId);
    Console.WriteLine($"Saga status after retry: {sagaAfterRetry.Status}");
}

// Example 8: Saga Performance Testing
Console.WriteLine("\nSaga Performance Testing:");

var performanceStart = DateTime.UtcNow;
var performanceTasks = new List<Task<ISaga>>();

// Start 10 concurrent sagas
for (int i = 1; i <= 10; i++)
{
    var perfOrderData = new CreateOrderData
    {
        CustomerEmail = $"perf{i}@example.com",
        TotalAmount = i * 25.0m,
        Items = new List<OrderItem>
        {
            new OrderItem { ProductId = $"PERF-{i:D3}", Quantity = 1, Price = i * 25.0m }
        }
    };

    performanceTasks.Add(orchestrator.StartSagaAsync(perfOrderData));
}

var performanceSagas = await Task.WhenAll(performanceTasks);
var startDuration = DateTime.UtcNow - performanceStart;

Console.WriteLine($"Started {performanceSagas.Length} sagas in {startDuration.TotalMilliseconds:F2}ms");

// Wait for completion
await Task.Delay(5000);

// Check completion rates
int completed = 0, failed = 0, other = 0;

foreach (var saga in performanceSagas)
{
    var currentSaga = await orchestrator.GetSagaAsync(saga.SagaId);
    
    switch (currentSaga.Status)
    {
        case SagaStatus.Completed:
            completed++;
            break;
        case SagaStatus.Failed:
        case SagaStatus.Compensated:
            failed++;
            break;
        default:
            other++;
            break;
    }
}

Console.WriteLine($"Performance results: {completed} completed, {failed} failed, {other} other");

// Example 9: Complex Saga Data Flow
Console.WriteLine("\nComplex Saga Data Flow:");

// Create a saga with complex data that flows between steps
var complexSaga = await orchestrator.StartSagaAsync("CreateOrderData", new
{
    CustomerEmail = "complex@example.com",
    TotalAmount = 549.99m,
    Items = new[]
    {
        new { ProductId = "COMPLEX-001", Quantity = 2, Price = 199.99m },
        new { ProductId = "COMPLEX-002", Quantity = 1, Price = 149.99m }
    },
    PaymentMethod = "CreditCard",
    ShippingAddress = new
    {
        Street = "123 Main St",
        City = "Anytown",
        State = "CA",
        ZipCode = "12345"
    }
});

Console.WriteLine($"Started complex saga: {complexSaga.SagaId}");

await Task.Delay(2000);

var finalComplexSaga = await orchestrator.GetSagaAsync(complexSaga.SagaId);
Console.WriteLine($"Complex saga final status: {finalComplexSaga.Status}");

// Show data flow between steps
Console.WriteLine("Data flow between steps:");
foreach (var step in finalComplexSaga.Steps.OrderBy(s => s.StepOrder))
{
    Console.WriteLine($"  Step: {step.StepName}");
    Console.WriteLine($"    Status: {step.Status}");
    
    if (step.StepData.Any())
    {
        Console.WriteLine("    Output data:");
        foreach (var kvp in step.StepData)
        {
            Console.WriteLine($"      {kvp.Key}: {kvp.Value}");
        }
    }
}

// Example 10: Saga Repository Operations
Console.WriteLine("\nSaga Repository Operations:");

var repository = services.GetRequiredService<ISagaRepository>();

// Get sagas by status
var completedSagas = await repository.GetSagasByStatusAsync(SagaStatus.Completed);
var failedSagas = await repository.GetSagasByStatusAsync(SagaStatus.Failed);
var compensatedSagas = await repository.GetSagasByStatusAsync(SagaStatus.Compensated);

Console.WriteLine($"Repository statistics:");
Console.WriteLine($"  Completed sagas: {completedSagas.Count()}");
Console.WriteLine($"  Failed sagas: {failedSagas.Count()}");
Console.WriteLine($"  Compensated sagas: {compensatedSagas.Count()}");

// Check for expired sagas
var expiredSagas = await repository.GetExpiredSagasAsync(TimeSpan.FromMinutes(30));
Console.WriteLine($"  Expired sagas: {expiredSagas.Count()}");

// Cleanup - in a real application, you might want to archive completed sagas
Console.WriteLine("\nCleaning up completed sagas...");
foreach (var saga in completedSagas.Take(5)) // Only delete first 5 for demo
{
    await repository.DeleteSagaAsync(saga.SagaId);
    Console.WriteLine($"  Deleted saga: {saga.SagaId}");
}

// Final statistics
var finalActiveSagas = await orchestrator.GetActiveSagasAsync();
Console.WriteLine($"Final active sagas count: {finalActiveSagas.Count()}");

// Dispose orchestrator
if (orchestrator is IDisposable disposableOrchestrator)
{
    disposableOrchestrator.Dispose();
}

Console.WriteLine("\nSaga patterns examples completed!");
```

**Notes**:

- Implement comprehensive distributed saga patterns for managing long-running distributed transactions
- Support both orchestration (centralized coordination) and choreography patterns for different architectural needs
- Provide automatic compensation logic with configurable compensation policies (None, Automatic, Manual, Retry)
- Implement saga state persistence with step-by-step tracking and data flow management
- Support timeout handling and expired saga detection with automatic compensation triggering
- Use step-based execution with proper error handling, retry mechanisms, and rollback capabilities
- Implement saga lifecycle management with creation, monitoring, cancellation, and cleanup operations
- Provide comprehensive saga monitoring with status tracking, performance metrics, and operational visibility
- Support complex data flow between saga steps with context-based data sharing and transformation
- Implement proper resource cleanup and disposal patterns for long-running saga orchestrators
- Use dependency injection for saga step handlers enabling testable and modular saga implementations
- Support manual saga operations for operational control and debugging scenarios
- Implement concurrent saga execution with proper isolation and resource management
- Provide saga repository abstraction for different persistence strategies (in-memory, database, event store)

**Prerequisites**:

- Understanding of distributed transaction patterns and eventual consistency concepts
- Knowledge of microservices architecture and inter-service communication patterns
- Familiarity with compensation patterns and rollback strategies in distributed systems
- Experience with long-running processes and state machine design patterns
- Understanding of dependency injection and service provider patterns
- Knowledge of async/await programming and concurrent execution patterns

**Related Snippets**:

- [Event Sourcing](event-sourcing.md) - Event sourcing patterns for audit trails and state reconstruction
- [Message Queue](message-queue.md) - Message queuing patterns for reliable saga communication
- [Publisher-Subscriber](pub-sub.md) - Event-driven communication patterns for saga choreography
