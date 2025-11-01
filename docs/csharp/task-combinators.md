# Task Combinators

**Description**: Comprehensive advanced Task coordination patterns including task combinators, parallel execution strategies, task cancellation handling, timeout management, complex async workflows, task scheduling, and high-performance async coordination for building robust concurrent applications.

**Language/Technology**: C# / .NET

**Code**:

```csharp
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Runtime.CompilerServices;
using System.Collections.Immutable;
using System.Runtime.ExceptionServices;

// Base interfaces for task coordination
public interface ITaskCombinator
{
    Task<T> Race<T>(params Task<T>[] tasks);
    Task<T> Race<T>(IEnumerable<Task<T>> tasks, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> All<T>(params Task<T>[] tasks);
    Task<IReadOnlyList<T>> All<T>(IEnumerable<Task<T>> tasks, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaskResult<T>>> AllSettled<T>(params Task<T>[] tasks);
    Task<IReadOnlyList<TaskResult<T>>> AllSettled<T>(IEnumerable<Task<T>> tasks, CancellationToken cancellationToken = default);
    Task<T> Any<T>(params Task<T>[] tasks);
    Task<T> Any<T>(IEnumerable<Task<T>> tasks, CancellationToken cancellationToken = default);
}

public interface IAsyncWorkflowBuilder<T>
{
    IAsyncWorkflowBuilder<TResult> Then<TResult>(Func<T, Task<TResult>> continuation);
    IAsyncWorkflowBuilder<TResult> Then<TResult>(Func<T, TResult> continuation);
    IAsyncWorkflowBuilder<T> Catch(Func<Exception, Task<T>> errorHandler);
    IAsyncWorkflowBuilder<T> Catch(Func<Exception, T> errorHandler);
    IAsyncWorkflowBuilder<T> Finally(Func<Task> finallyAction);
    IAsyncWorkflowBuilder<T> Finally(Action finallyAction);
    IAsyncWorkflowBuilder<T> Timeout(TimeSpan timeout);
    IAsyncWorkflowBuilder<T> Retry(int maxAttempts, TimeSpan delay);
    Task<T> ExecuteAsync(CancellationToken cancellationToken = default);
}

// Task result wrapper for settled operations
public class TaskResult<T>
{
    public bool IsSuccess { get; }
    public T Value { get; }
    public Exception Exception { get; }
    public TaskStatus Status { get; }

    private TaskResult(bool isSuccess, T value, Exception exception, TaskStatus status)
    {
        IsSuccess = isSuccess;
        Value = value;
        Exception = exception;
        Status = status;
    }

    public static TaskResult<T> Success(T value) => new TaskResult<T>(true, value, null, TaskStatus.RanToCompletion);
    public static TaskResult<T> Failure(Exception exception) => new TaskResult<T>(false, default(T), exception, TaskStatus.Faulted);
    public static TaskResult<T> Canceled() => new TaskResult<T>(false, default(T), null, TaskStatus.Canceled);

    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<Exception, TResult> onFailure, Func<TResult> onCanceled = null)
    {
        return Status switch
        {
            TaskStatus.RanToCompletion => onSuccess(Value),
            TaskStatus.Faulted => onFailure(Exception),
            TaskStatus.Canceled => onCanceled?.Invoke() ?? onFailure(new OperationCanceledException()),
            _ => throw new InvalidOperationException($"Unexpected task status: {Status}")
        };
    }

    public override string ToString()
    {
        return Status switch
        {
            TaskStatus.RanToCompletion => $"Success: {Value}",
            TaskStatus.Faulted => $"Failure: {Exception?.Message}",
            TaskStatus.Canceled => "Canceled",
            _ => $"Status: {Status}"
        };
    }
}

// Comprehensive task combinator implementation
public class TaskCombinator : ITaskCombinator
{
    private readonly ILogger logger;

    public TaskCombinator(ILogger logger = null)
    {
        this.logger = logger;
    }

    public async Task<T> Race<T>(params Task<T>[] tasks)
    {
        return await Race(tasks.AsEnumerable());
    }

    public async Task<T> Race<T>(IEnumerable<Task<T>> tasks, CancellationToken cancellationToken = default)
    {
        var taskList = tasks.ToList();
        if (taskList.Count == 0)
            throw new ArgumentException("At least one task must be provided", nameof(tasks));

        logger?.LogTrace("Starting race with {TaskCount} tasks", taskList.Count);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var completedTask = await Task.WhenAny(taskList);
            
            // Cancel remaining tasks
            cts.Cancel();
            
            stopwatch.Stop();
            logger?.LogDebug("Race completed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);

            return await completedTask; // This will throw if the completed task faulted
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger?.LogError(ex, "Race failed after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    public async Task<IReadOnlyList<T>> All<T>(params Task<T>[] tasks)
    {
        return await All(tasks.AsEnumerable());
    }

    public async Task<IReadOnlyList<T>> All<T>(IEnumerable<Task<T>> tasks, CancellationToken cancellationToken = default)
    {
        var taskList = tasks.ToList();
        if (taskList.Count == 0)
            return Array.Empty<T>();

        logger?.LogTrace("Starting All with {TaskCount} tasks", taskList.Count);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var results = await Task.WhenAll(taskList).WaitAsync(cancellationToken);
            
            stopwatch.Stop();
            logger?.LogDebug("All completed successfully in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            
            return results;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger?.LogError(ex, "All failed after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    public async Task<IReadOnlyList<TaskResult<T>>> AllSettled<T>(params Task<T>[] tasks)
    {
        return await AllSettled(tasks.AsEnumerable());
    }

    public async Task<IReadOnlyList<TaskResult<T>>> AllSettled<T>(IEnumerable<Task<T>> tasks, CancellationToken cancellationToken = default)
    {
        var taskList = tasks.ToList();
        if (taskList.Count == 0)
            return Array.Empty<TaskResult<T>>();

        logger?.LogTrace("Starting AllSettled with {TaskCount} tasks", taskList.Count);

        var stopwatch = Stopwatch.StartNew();
        var results = new List<TaskResult<T>>();

        // Wait for all tasks to complete, regardless of outcome
        await Task.WhenAll(taskList.Select(async task =>
        {
            try
            {
                var result = await task.WaitAsync(cancellationToken);
                lock (results)
                {
                    results.Add(TaskResult<T>.Success(result));
                }
            }
            catch (OperationCanceledException)
            {
                lock (results)
                {
                    results.Add(TaskResult<T>.Canceled());
                }
            }
            catch (Exception ex)
            {
                lock (results)
                {
                    results.Add(TaskResult<T>.Failure(ex));
                }
            }
        }));

        stopwatch.Stop();

        var successCount = results.Count(r => r.IsSuccess);
        var failureCount = results.Count - successCount;

        logger?.LogDebug("AllSettled completed in {ElapsedMs}ms: {SuccessCount} success, {FailureCount} failures",
            stopwatch.ElapsedMilliseconds, successCount, failureCount);

        return results;
    }

    public async Task<T> Any<T>(params Task<T>[] tasks)
    {
        return await Any(tasks.AsEnumerable());
    }

    public async Task<T> Any<T>(IEnumerable<Task<T>> tasks, CancellationToken cancellationToken = default)
    {
        var taskList = tasks.ToList();
        if (taskList.Count == 0)
            throw new ArgumentException("At least one task must be provided", nameof(tasks));

        logger?.LogTrace("Starting Any with {TaskCount} tasks", taskList.Count);

        var stopwatch = Stopwatch.StartNew();
        var exceptions = new();
        var completedTasks = 0;
        var tcs = new TaskCompletionSource<T>();

        // Setup cancellation
        using var registration = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));

        foreach (var task in taskList)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    var result = await task;
                    
                    if (tcs.TrySetResult(result))
                    {
                        stopwatch.Stop();
                        logger?.LogDebug("Any succeeded in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                    }
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                        
                        if (++completedTasks == taskList.Count)
                        {
                            // All tasks failed
                            var aggregateException = new AggregateException(exceptions);
                            
                            if (tcs.TrySetException(aggregateException))
                            {
                                stopwatch.Stop();
                                logger?.LogError(aggregateException, "Any failed - all tasks faulted after {ElapsedMs}ms", 
                                    stopwatch.ElapsedMilliseconds);
                            }
                        }
                    }
                }
            });
        }

        return await tcs.Task;
    }
}

// Async workflow builder for complex task orchestration
public class AsyncWorkflowBuilder<T> : IAsyncWorkflowBuilder<T>
{
    private readonly Task<T> initialTask;
    private readonly ILogger logger;

    public AsyncWorkflowBuilder(Task<T> initialTask, ILogger logger = null)
    {
        this.initialTask = initialTask ?? throw new ArgumentNullException(nameof(initialTask));
        this.logger = logger;
    }

    public static AsyncWorkflowBuilder<TResult> Start<TResult>(Func<Task<TResult>> taskFactory, ILogger logger = null)
    {
        var task = Task.Run(taskFactory);
        return new AsyncWorkflowBuilder<TResult>(task, logger);
    }

    public static AsyncWorkflowBuilder<TResult> FromResult<TResult>(TResult value, ILogger logger = null)
    {
        return new AsyncWorkflowBuilder<TResult>(Task.FromResult(value), logger);
    }

    public IAsyncWorkflowBuilder<TResult> Then<TResult>(Func<T, Task<TResult>> continuation)
    {
        var continuationTask = initialTask.ContinueWith(async previousTask =>
        {
            try
            {
                var result = await previousTask;
                logger?.LogTrace("Workflow continuation executing");
                return await continuation(result);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Workflow continuation failed");
                throw;
            }
        }, TaskContinuationOptions.NotOnCanceled).Unwrap();

        return new AsyncWorkflowBuilder<TResult>(continuationTask, logger);
    }

    public IAsyncWorkflowBuilder<TResult> Then<TResult>(Func<T, TResult> continuation)
    {
        return Then(value => Task.FromResult(continuation(value)));
    }

    public IAsyncWorkflowBuilder<T> Catch(Func<Exception, Task<T>> errorHandler)
    {
        var handledTask = initialTask.ContinueWith(async previousTask =>
        {
            if (previousTask.IsFaulted)
            {
                var exception = previousTask.Exception?.GetBaseException() ?? new Exception("Unknown error");
                logger?.LogWarning(exception, "Workflow error being handled");
                
                try
                {
                    return await errorHandler(exception);
                }
                catch (Exception handlerEx)
                {
                    logger?.LogError(handlerEx, "Error handler failed");
                    throw;
                }
            }
            else if (previousTask.IsCanceled)
            {
                throw new OperationCanceledException();
            }
            else
            {
                return previousTask.Result;
            }
        }, TaskContinuationOptions.ExecuteSynchronously).Unwrap();

        return new AsyncWorkflowBuilder<T>(handledTask, logger);
    }

    public IAsyncWorkflowBuilder<T> Catch(Func<Exception, T> errorHandler)
    {
        return Catch(ex => Task.FromResult(errorHandler(ex)));
    }

    public IAsyncWorkflowBuilder<T> Finally(Func<Task> finallyAction)
    {
        var finalizedTask = initialTask.ContinueWith(async previousTask =>
        {
            try
            {
                await finallyAction();
                logger?.LogTrace("Workflow finally block executed");
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Finally block failed");
                // Don't throw - finally blocks shouldn't change the outcome
            }

            // Preserve original task result/exception
            if (previousTask.IsFaulted)
            {
                ExceptionDispatchInfo.Capture(previousTask.Exception.GetBaseException()).Throw();
            }
            else if (previousTask.IsCanceled)
            {
                throw new OperationCanceledException();
            }

            return previousTask.Result;
        }, TaskContinuationOptions.ExecuteSynchronously).Unwrap();

        return new AsyncWorkflowBuilder<T>(finalizedTask, logger);
    }

    public IAsyncWorkflowBuilder<T> Finally(Action finallyAction)
    {
        return Finally(() =>
        {
            finallyAction();
            return Task.CompletedTask;
        });
    }

    public IAsyncWorkflowBuilder<T> Timeout(TimeSpan timeout)
    {
        var timeoutTask = initialTask.WaitAsync(timeout);
        return new AsyncWorkflowBuilder<T>(timeoutTask, logger);
    }

    public IAsyncWorkflowBuilder<T> Retry(int maxAttempts, TimeSpan delay)
    {
        var retryTask = RetryInternalAsync(maxAttempts, delay);
        return new AsyncWorkflowBuilder<T>(retryTask, logger);
    }

    private async Task<T> RetryInternalAsync(int maxAttempts, TimeSpan delay)
    {
        Exception lastException = null;
        
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                logger?.LogTrace("Retry attempt {Attempt} of {MaxAttempts}", attempt, maxAttempts);
                return await initialTask;
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                lastException = ex;
                logger?.LogWarning(ex, "Retry attempt {Attempt} failed, retrying after {DelayMs}ms", 
                    attempt, delay.TotalMilliseconds);
                
                await Task.Delay(delay);
            }
        }

        logger?.LogError(lastException, "All retry attempts failed");
        throw lastException ?? new Exception("All retry attempts failed");
    }

    public Task<T> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        return initialTask.WaitAsync(cancellationToken);
    }
}

// Parallel execution coordinator with concurrency control
public class ParallelExecutor
{
    private readonly SemaphoreSlim concurrencyLimiter;
    private readonly ILogger logger;
    private readonly ParallelExecutorOptions options;

    public ParallelExecutor(ParallelExecutorOptions options = null, ILogger logger = null)
    {
        this.options = options ?? ParallelExecutorOptions.Default;
        this.logger = logger;
        concurrencyLimiter = new(this.options.MaxConcurrency, this.options.MaxConcurrency);
    }

    public async Task<IReadOnlyList<TResult>> ExecuteAsync<TInput, TResult>(
        IEnumerable<TInput> inputs,
        Func<TInput, CancellationToken, Task<TResult>> processor,
        CancellationToken cancellationToken = default)
    {
        var inputList = inputs.ToList();
        if (inputList.Count == 0)
            return Array.Empty<TResult>();

        logger?.LogDebug("Starting parallel execution of {InputCount} items with max concurrency {MaxConcurrency}",
            inputList.Count, options.MaxConcurrency);

        var stopwatch = Stopwatch.StartNew();
        var results = new();
        var exceptions = new();

        var tasks = inputList.Select(async (input, index) =>
        {
            await concurrencyLimiter.WaitAsync(cancellationToken);
            
            try
            {
                var itemStopwatch = Stopwatch.StartNew();
                var result = await processor(input, cancellationToken);
                itemStopwatch.Stop();
                
                results.Add((index, result));
                
                logger?.LogTrace("Processed item {Index} in {ElapsedMs}ms", index, itemStopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
                logger?.LogError(ex, "Failed to process item {Index}", index);
                
                if (options.FailFast)
                {
                    throw;
                }
            }
            finally
            {
                concurrencyLimiter.Release();
            }
        });

        await Task.WhenAll(tasks);
        
        stopwatch.Stop();

        if (exceptions.Count > 0 && options.FailFast)
        {
            throw new AggregateException(exceptions);
        }

        // Sort results by original index to maintain order
        var sortedResults = results
            .OrderBy(x => x.Index)
            .Select(x => x.Result)
            .ToList();

        logger?.LogInformation("Parallel execution completed in {ElapsedMs}ms: {SuccessCount}/{TotalCount} items processed",
            stopwatch.ElapsedMilliseconds, results.Count, inputList.Count);

        return sortedResults;
    }

    public async Task<IReadOnlyList<TaskResult<TResult>>> ExecuteAllSettledAsync<TInput, TResult>(
        IEnumerable<TInput> inputs,
        Func<TInput, CancellationToken, Task<TResult>> processor,
        CancellationToken cancellationToken = default)
    {
        var inputList = inputs.ToList();
        if (inputList.Count == 0)
            return Array.Empty<TaskResult<TResult>>();

        logger?.LogDebug("Starting parallel AllSettled execution of {InputCount} items", inputList.Count);

        var stopwatch = Stopwatch.StartNew();
        var results = new TaskResult<TResult>[inputList.Count];

        var tasks = inputList.Select(async (input, index) =>
        {
            await concurrencyLimiter.WaitAsync(cancellationToken);
            
            try
            {
                var result = await processor(input, cancellationToken);
                results[index] = TaskResult<TResult>.Success(result);
            }
            catch (OperationCanceledException)
            {
                results[index] = TaskResult<TResult>.Canceled();
            }
            catch (Exception ex)
            {
                results[index] = TaskResult<TResult>.Failure(ex);
            }
            finally
            {
                concurrencyLimiter.Release();
            }
        });

        await Task.WhenAll(tasks);
        
        stopwatch.Stop();

        var successCount = results.Count(r => r.IsSuccess);
        logger?.LogInformation("Parallel AllSettled completed in {ElapsedMs}ms: {SuccessCount}/{TotalCount} succeeded",
            stopwatch.ElapsedMilliseconds, successCount, inputList.Count);

        return results;
    }

    public void Dispose()
    {
        concurrencyLimiter?.Dispose();
    }
}

public class ParallelExecutorOptions
{
    public int MaxConcurrency { get; set; } = Environment.ProcessorCount;
    public bool FailFast { get; set; } = true;

    public static ParallelExecutorOptions Default => new ParallelExecutorOptions();

    public static ParallelExecutorOptions Create(int maxConcurrency, bool failFast = true)
    {
        return new ParallelExecutorOptions
        {
            MaxConcurrency = maxConcurrency,
            FailFast = failFast
        };
    }
}

// Advanced timeout and cancellation manager
public class TimeoutManager : IDisposable
{
    private readonly ConcurrentDictionary<string, CancellationTokenSource> activeCancellations;
    private readonly ILogger logger;
    private volatile bool isDisposed = false;

    public TimeoutManager(ILogger logger = null)
    {
        activeCancellations = new();
        this.logger = logger;
    }

    public async Task<T> WithTimeoutAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        TimeSpan timeout,
        string operationId = null,
        CancellationToken cancellationToken = default)
    {
        if (isDisposed) throw new ObjectDisposedException(nameof(TimeoutManager));

        operationId ??= Guid.NewGuid().ToString("N")[..8];

        using var timeoutCts = new CancellationTokenSource(timeout);
        using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, timeoutCts.Token);

        activeCancellations[operationId] = combinedCts;

        try
        {
            logger?.LogTrace("Starting operation {OperationId} with {TimeoutMs}ms timeout",
                operationId, timeout.TotalMilliseconds);

            var stopwatch = Stopwatch.StartNew();
            var result = await operation(combinedCts.Token);
            stopwatch.Stop();

            logger?.LogDebug("Operation {OperationId} completed successfully in {ElapsedMs}ms",
                operationId, stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
        {
            logger?.LogWarning("Operation {OperationId} timed out after {TimeoutMs}ms",
                operationId, timeout.TotalMilliseconds);
            throw new TimeoutException($"Operation {operationId} timed out after {timeout.TotalMilliseconds}ms");
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Operation {OperationId} failed", operationId);
            throw;
        }
        finally
        {
            activeCancellations.TryRemove(operationId, out _);
        }
    }

    public async Task<TaskResult<T>> WithTimeoutSafeAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        TimeSpan timeout,
        string operationId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await WithTimeoutAsync(operation, timeout, operationId, cancellationToken);
            return TaskResult<T>.Success(result);
        }
        catch (OperationCanceledException)
        {
            return TaskResult<T>.Canceled();
        }
        catch (Exception ex)
        {
            return TaskResult<T>.Failure(ex);
        }
    }

    public bool TryCancelOperation(string operationId)
    {
        if (activeCancellations.TryRemove(operationId, out var cts))
        {
            cts.Cancel();
            logger?.LogInformation("Canceled operation {OperationId}", operationId);
            return true;
        }

        return false;
    }

    public void CancelAllOperations()
    {
        var operationIds = activeCancellations.Keys.ToList();
        
        foreach (var operationId in operationIds)
        {
            TryCancelOperation(operationId);
        }

        logger?.LogInformation("Canceled {OperationCount} active operations", operationIds.Count);
    }

    public int ActiveOperationCount => activeCancellations.Count;

    public void Dispose()
    {
        if (!isDisposed)
        {
            isDisposed = true;
            CancelAllOperations();
        }
    }
}

// Task scheduler with priority and throttling
public class PriorityTaskScheduler : IDisposable
{
    private readonly SortedDictionary<int, Queue<TaskItem>> priorityQueues;
    private readonly SemaphoreSlim concurrencyLimiter;
    private readonly Timer processingTimer;
    private readonly ILogger logger;
    private readonly object lockObject = new();
    private volatile bool isDisposed = false;

    public PriorityTaskScheduler(int maxConcurrency = 10, TimeSpan? processingInterval = null, ILogger logger = null)
    {
        priorityQueues = new SortedDictionary<int, Queue<TaskItem>>(Comparer<int>.Create((x, y) => y.CompareTo(x))); // Higher priority first
        concurrencyLimiter = new(maxConcurrency, maxConcurrency);
        this.logger = logger;

        var interval = processingInterval ?? TimeSpan.FromMilliseconds(10);
        processingTimer = new Timer(ProcessQueue, null, interval, interval);
    }

    public Task<T> ScheduleAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        int priority = 0,
        CancellationToken cancellationToken = default)
    {
        if (isDisposed) throw new ObjectDisposedException(nameof(PriorityTaskScheduler));

        var tcs = new TaskCompletionSource<T>();
        var taskItem = new TaskItem(
            async ct =>
            {
                try
                {
                    var result = await operation(ct);
                    tcs.SetResult(result);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            },
            priority,
            cancellationToken);

        lock (lockObject)
        {
            if (!priorityQueues.ContainsKey(priority))
            {
                priorityQueues[priority] = new();
            }
            
            priorityQueues[priority].Enqueue(taskItem);
        }

        logger?.LogTrace("Scheduled task with priority {Priority}, queue size: {QueueSize}",
            priority, GetTotalQueueSize());

        return tcs.Task;
    }

    public Task ScheduleAsync(
        Func<CancellationToken, Task> operation,
        int priority = 0,
        CancellationToken cancellationToken = default)
    {
        return ScheduleAsync(async ct =>
        {
            await operation(ct);
            return true; // Dummy result
        }, priority, cancellationToken);
    }

    private async void ProcessQueue(object state)
    {
        if (isDisposed) return;

        TaskItem taskItem = null;

        lock (lockObject)
        {
            foreach (var kvp in priorityQueues)
            {
                var queue = kvp.Value;
                if (queue.Count > 0)
                {
                    taskItem = queue.Dequeue();
                    
                    // Remove empty queues to keep dictionary clean
                    if (queue.Count == 0)
                    {
                        priorityQueues.Remove(kvp.Key);
                    }
                    break;
                }
            }
        }

        if (taskItem != null && !taskItem.CancellationToken.IsCancellationRequested)
        {
            await concurrencyLimiter.WaitAsync();
            
            _ = Task.Run(async () =>
            {
                try
                {
                    logger?.LogTrace("Executing task with priority {Priority}", taskItem.Priority);
                    
                    var stopwatch = Stopwatch.StartNew();
                    await taskItem.Operation(taskItem.CancellationToken);
                    stopwatch.Stop();
                    
                    logger?.LogTrace("Task completed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Task execution failed");
                }
                finally
                {
                    concurrencyLimiter.Release();
                }
            });
        }
    }

    public int GetTotalQueueSize()
    {
        lock (lockObject)
        {
            return priorityQueues.Values.Sum(q => q.Count);
        }
    }

    public int GetQueueSize(int priority)
    {
        lock (lockObject)
        {
            return priorityQueues.TryGetValue(priority, out var queue) ? queue.Count : 0;
        }
    }

    public void Dispose()
    {
        if (!isDisposed)
        {
            isDisposed = true;
            
            processingTimer?.Dispose();
            concurrencyLimiter?.Dispose();
            
            logger?.LogInformation("Priority task scheduler disposed");
        }
    }

    private class TaskItem
    {
        public Func<CancellationToken, Task> Operation { get; }
        public int Priority { get; }
        public CancellationToken CancellationToken { get; }

        public TaskItem(Func<CancellationToken, Task> operation, int priority, CancellationToken cancellationToken)
        {
            Operation = operation;
            Priority = priority;
            CancellationToken = cancellationToken;
        }
    }
}

// Task performance metrics and monitoring
public class TaskCoordinationMetrics
{
    private volatile long totalTasksExecuted = 0;
    private volatile long totalTasksFailed = 0;
    private volatile long totalTasksCanceled = 0;
    private volatile long totalExecutionTime = 0;
    private volatile long peakConcurrency = 0;
    private volatile long currentConcurrency = 0;
    private readonly ConcurrentDictionary<string, long> operationCounts = new();
    private readonly object lockObject = new();
    private DateTime startTime = DateTime.UtcNow;

    public long TotalTasksExecuted => totalTasksExecuted;
    public long TotalTasksFailed => totalTasksFailed;
    public long TotalTasksCanceled => totalTasksCanceled;
    public long CurrentConcurrency => currentConcurrency;
    public long PeakConcurrency => peakConcurrency;

    public TimeSpan AverageExecutionTime => totalTasksExecuted > 0 
        ? TimeSpan.FromTicks(totalExecutionTime / totalTasksExecuted)
        : TimeSpan.Zero;

    public double TasksPerSecond
    {
        get
        {
            var elapsed = DateTime.UtcNow - startTime;
            return elapsed.TotalSeconds > 0 ? totalTasksExecuted / elapsed.TotalSeconds : 0.0;
        }
    }

    public double SuccessRate
    {
        get
        {
            var total = totalTasksExecuted + totalTasksFailed + totalTasksCanceled;
            return total > 0 ? (double)totalTasksExecuted / total : 0.0;
        }
    }

    public void RecordTaskStarted()
    {
        var newConcurrency = Interlocked.Increment(ref currentConcurrency);
        
        // Update peak concurrency
        var currentPeak = peakConcurrency;
        while (newConcurrency > currentPeak && 
               Interlocked.CompareExchange(ref peakConcurrency, newConcurrency, currentPeak) != currentPeak)
        {
            currentPeak = peakConcurrency;
        }
    }

    public void RecordTaskCompleted(TimeSpan executionTime, string operationType = null)
    {
        Interlocked.Decrement(ref currentConcurrency);
        Interlocked.Increment(ref totalTasksExecuted);
        Interlocked.Add(ref totalExecutionTime, executionTime.Ticks);

        if (!string.IsNullOrEmpty(operationType))
        {
            operationCounts.AddOrUpdate(operationType, 1, (key, value) => value + 1);
        }
    }

    public void RecordTaskFailed()
    {
        Interlocked.Decrement(ref currentConcurrency);
        Interlocked.Increment(ref totalTasksFailed);
    }

    public void RecordTaskCanceled()
    {
        Interlocked.Decrement(ref currentConcurrency);
        Interlocked.Increment(ref totalTasksCanceled);
    }

    public void Reset()
    {
        lock (lockObject)
        {
            totalTasksExecuted = 0;
            totalTasksFailed = 0;
            totalTasksCanceled = 0;
            totalExecutionTime = 0;
            peakConcurrency = 0;
            currentConcurrency = 0;
            operationCounts.Clear();
            startTime = DateTime.UtcNow;
        }
    }

    public TaskCoordinationStats GetStats()
    {
        return new TaskCoordinationStats
        {
            TotalTasksExecuted = TotalTasksExecuted,
            TotalTasksFailed = TotalTasksFailed,
            TotalTasksCanceled = TotalTasksCanceled,
            CurrentConcurrency = CurrentConcurrency,
            PeakConcurrency = PeakConcurrency,
            AverageExecutionTime = AverageExecutionTime,
            TasksPerSecond = TasksPerSecond,
            SuccessRate = SuccessRate,
            OperationCounts = operationCounts.ToImmutableDictionary()
        };
    }
}

public class TaskCoordinationStats
{
    public long TotalTasksExecuted { get; set; }
    public long TotalTasksFailed { get; set; }
    public long TotalTasksCanceled { get; set; }
    public long CurrentConcurrency { get; set; }
    public long PeakConcurrency { get; set; }
    public TimeSpan AverageExecutionTime { get; set; }
    public double TasksPerSecond { get; set; }
    public double SuccessRate { get; set; }
    public ImmutableDictionary<string, long> OperationCounts { get; set; }
}

// Monitored task wrapper for automatic metrics collection
public class MonitoredTask
{
    private readonly TaskCoordinationMetrics metrics;
    private readonly ILogger logger;

    public MonitoredTask(TaskCoordinationMetrics metrics, ILogger logger = null)
    {
        this.metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        this.logger = logger;
    }

    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        string operationType = null,
        CancellationToken cancellationToken = default)
    {
        metrics.RecordTaskStarted();
        
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            logger?.LogTrace("Starting monitored task: {OperationType}", operationType ?? "Unknown");
            
            var result = await operation(cancellationToken);
            
            stopwatch.Stop();
            metrics.RecordTaskCompleted(stopwatch.Elapsed, operationType);
            
            logger?.LogDebug("Monitored task completed in {ElapsedMs}ms: {OperationType}",
                stopwatch.ElapsedMilliseconds, operationType ?? "Unknown");
            
            return result;
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            metrics.RecordTaskCanceled();
            
            logger?.LogInformation("Monitored task canceled after {ElapsedMs}ms: {OperationType}",
                stopwatch.ElapsedMilliseconds, operationType ?? "Unknown");
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            metrics.RecordTaskFailed();
            
            logger?.LogError(ex, "Monitored task failed after {ElapsedMs}ms: {OperationType}",
                stopwatch.ElapsedMilliseconds, operationType ?? "Unknown");
            throw;
        }
    }

    public Task ExecuteAsync(
        Func<CancellationToken, Task> operation,
        string operationType = null,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(async ct =>
        {
            await operation(ct);
            return true; // Dummy result
        }, operationType, cancellationToken);
    }
}
```

**Usage**:

```csharp
// Example 1: Task Combinator Patterns
Console.WriteLine("Task Combinator Examples:");

var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger("TaskExample");
var combinator = new TaskCombinator(logger);

// Race - first to complete wins
var raceTasks = new[]
{
    Task.Delay(1000).ContinueWith(_ => "Slow task"),
    Task.Delay(500).ContinueWith(_ => "Fast task"),
    Task.Delay(750).ContinueWith(_ => "Medium task")
};

var winner = await combinator.Race(raceTasks);
Console.WriteLine($"Race winner: {winner}");

// All - wait for all to complete
var allTasks = new[]
{
    Task.Delay(100).ContinueWith(_ => "Task 1"),
    Task.Delay(200).ContinueWith(_ => "Task 2"),
    Task.Delay(150).ContinueWith(_ => "Task 3")
};

var allResults = await combinator.All(allTasks);
Console.WriteLine($"All results: {string.Join(", ", allResults)}");

// AllSettled - wait for all, capture successes and failures
var settledTasks = new[]
{
    Task.Delay(100).ContinueWith<string>(_ => "Success 1"),
    Task.FromException<string>(new InvalidOperationException("Simulated failure")),
    Task.Delay(200).ContinueWith<string>(_ => "Success 2")
};

var settledResults = await combinator.AllSettled(settledTasks);
foreach (var result in settledResults)
{
    Console.WriteLine($"Settled result: {result}");
}

// Any - first success wins, aggregate failures if all fail
var anyTasks = new[]
{
    Task.Delay(300).ContinueWith<int>(_ => throw new Exception("Delayed failure")),
    Task.Delay(100).ContinueWith(_ => 42),
    Task.Delay(200).ContinueWith(_ => 100)
};

var anyResult = await combinator.Any(anyTasks);
Console.WriteLine($"Any result: {anyResult}");

// Example 2: Async Workflow Builder
Console.WriteLine("\nAsync Workflow Examples:");

var workflowResult = await AsyncWorkflowBuilder<int>
    .Start(() => Task.FromResult(10), logger)
    .Then(x => x * 2)
    .Then(async x => 
    {
        await Task.Delay(100);
        return x + 5;
    })
    .Catch(ex => 
    {
        Console.WriteLine($"Workflow error: {ex.Message}");
        return 0;
    })
    .Timeout(TimeSpan.FromSeconds(5))
    .Retry(3, TimeSpan.FromMilliseconds(100))
    .Finally(() => Console.WriteLine("Workflow completed"))
    .ExecuteAsync();

Console.WriteLine($"Workflow result: {workflowResult}");

// Example 3: Parallel Execution with Concurrency Control
Console.WriteLine("\nParallel Execution Examples:");

var parallelExecutor = new ParallelExecutor(
    ParallelExecutorOptions.Create(maxConcurrency: 3, failFast: false), 
    logger);

var inputs = Enumerable.Range(1, 20).ToList();

var parallelResults = await parallelExecutor.ExecuteAsync(
    inputs,
    async (input, cancellationToken) =>
    {
        // Simulate work with variable duration
        var delay = Random.Shared.Next(50, 200);
        await Task.Delay(delay, cancellationToken);
        
        // Simulate occasional failures
        if (input % 7 == 0)
            throw new InvalidOperationException($"Simulated failure for input {input}");
        
        return $"Processed-{input}";
    });

Console.WriteLine($"Parallel execution completed: {parallelResults.Count} results");

// AllSettled version that captures all outcomes
var allSettledResults = await parallelExecutor.ExecuteAllSettledAsync(
    inputs,
    async (input, cancellationToken) =>
    {
        await Task.Delay(Random.Shared.Next(50, 150), cancellationToken);
        
        if (input % 5 == 0)
            throw new Exception($"Error processing {input}");
        
        return input * input;
    });

var successfulResults = allSettledResults.Where(r => r.IsSuccess).ToList();
var failedResults = allSettledResults.Where(r => !r.IsSuccess).ToList();

Console.WriteLine($"AllSettled: {successfulResults.Count} succeeded, {failedResults.Count} failed");

// Example 4: Timeout Manager
Console.WriteLine("\nTimeout Manager Examples:");

var timeoutManager = new TimeoutManager(logger);

// Operation with timeout
try
{
    var timeoutResult = await timeoutManager.WithTimeoutAsync(
        async cancellationToken =>
        {
            // Simulate long-running operation
            await Task.Delay(2000, cancellationToken);
            return "Operation completed";
        },
        TimeSpan.FromMilliseconds(500), // Will timeout
        "long-operation");
    
    Console.WriteLine($"Timeout result: {timeoutResult}");
}
catch (TimeoutException ex)
{
    Console.WriteLine($"Operation timed out: {ex.Message}");
}

// Safe timeout that returns result wrapper
var safeTimeoutResult = await timeoutManager.WithTimeoutSafeAsync(
    async cancellationToken =>
    {
        await Task.Delay(100, cancellationToken);
        return "Quick operation";
    },
    TimeSpan.FromSeconds(1),
    "quick-operation");

Console.WriteLine($"Safe timeout result: {safeTimeoutResult}");

// Example 5: Priority Task Scheduler
Console.WriteLine("\nPriority Task Scheduler Examples:");

var priorityScheduler = new PriorityTaskScheduler(maxConcurrency: 2, logger: logger);

// Schedule tasks with different priorities
var priorityTasks = new List<Task<string>>();

for (int i = 1; i <= 10; i++)
{
    var priority = i % 3; // 0, 1, 2 priority levels
    var taskId = i;
    
    var task = priorityScheduler.ScheduleAsync(
        async cancellationToken =>
        {
            await Task.Delay(Random.Shared.Next(100, 300), cancellationToken);
            return $"Task-{taskId} (Priority-{priority})";
        },
        priority);
    
    priorityTasks.Add(task);
}

Console.WriteLine($"Scheduled {priorityTasks.Count} tasks with varying priorities");

// Wait for some tasks to complete and show execution order
for (int i = 0; i < 5; i++)
{
    var completed = await Task.WhenAny(priorityTasks.Where(t => !t.IsCompleted));
    var result = await completed;
    Console.WriteLine($"Completed: {result}");
}

// Wait for all remaining tasks
var remainingResults = await Task.WhenAll(priorityTasks);
Console.WriteLine($"All priority tasks completed: {remainingResults.Length} total");

// Example 6: Task Coordination Metrics
Console.WriteLine("\nTask Coordination Metrics Examples:");

var metrics = new TaskCoordinationMetrics();
var monitoredTask = new MonitoredTask(metrics, logger);

// Execute monitored tasks
var monitoredTasks = Enumerable.Range(1, 15).Select(async i =>
{
    return await monitoredTask.ExecuteAsync(
        async cancellationToken =>
        {
            await Task.Delay(Random.Shared.Next(50, 200), cancellationToken);
            
            // Simulate occasional failures
            if (i % 6 == 0)
                throw new Exception($"Simulated error in task {i}");
            
            return $"Result-{i}";
        },
        $"Operation-Type-{i % 3}"); // Group operations by type
});

try
{
    var monitoredResults = await Task.WhenAll(monitoredTasks);
    Console.WriteLine($"Monitored tasks completed: {monitoredResults.Length}");
}
catch (Exception)
{
    Console.WriteLine("Some monitored tasks failed (expected)");
}

var stats = metrics.GetStats();
Console.WriteLine($"Task Metrics:");
Console.WriteLine($"  Executed: {stats.TotalTasksExecuted}");
Console.WriteLine($"  Failed: {stats.TotalTasksFailed}");
Console.WriteLine($"  Canceled: {stats.TotalTasksCanceled}");
Console.WriteLine($"  Success Rate: {stats.SuccessRate:P2}");
Console.WriteLine($"  Average Execution Time: {stats.AverageExecutionTime.TotalMilliseconds:F1}ms");
Console.WriteLine($"  Tasks/Second: {stats.TasksPerSecond:F1}");
Console.WriteLine($"  Peak Concurrency: {stats.PeakConcurrency}");

foreach (var operationType in stats.OperationCounts)
{
    Console.WriteLine($"  {operationType.Key}: {operationType.Value} executions");
}

// Example 7: Complex Workflow Orchestration
Console.WriteLine("\nComplex Workflow Examples:");

// Simulate a complex business workflow
var workflowSteps = new[]
{
    ("Validate Input", TimeSpan.FromMilliseconds(50)),
    ("Fetch User Data", TimeSpan.FromMilliseconds(100)),
    ("Process Business Logic", TimeSpan.FromMilliseconds(200)),
    ("Update Database", TimeSpan.FromMilliseconds(150)),
    ("Send Notifications", TimeSpan.FromMilliseconds(75))
};

var complexWorkflowResult = await AsyncWorkflowBuilder<Dictionary<string, object>>
    .FromResult(new Dictionary<string, object>(), logger)
    .Then(async context =>
    {
        // Step 1: Validation
        await Task.Delay(workflowSteps[0].Item2);
        context["validation"] = "passed";
        Console.WriteLine($"✓ {workflowSteps[0].Item1}");
        return context;
    })
    .Then(async context =>
    {
        // Step 2: Fetch data (with simulated failure retry)
        for (int attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                await Task.Delay(workflowSteps[1].Item2);
                
                // Simulate intermittent failure
                if (attempt < 3 && Random.Shared.Next(2) == 0)
                    throw new Exception("Temporary data fetch error");
                
                context["userData"] = new { UserId = 123, Name = "John Doe" };
                Console.WriteLine($"✓ {workflowSteps[1].Item1} (attempt {attempt})");
                break;
            }
            catch (Exception ex) when (attempt < 3)
            {
                Console.WriteLine($"✗ {workflowSteps[1].Item1} failed (attempt {attempt}): {ex.Message}");
                await Task.Delay(50); // Brief retry delay
            }
        }
        return context;
    })
    .Then(async context =>
    {
        // Step 3: Business logic processing
        await Task.Delay(workflowSteps[2].Item2);
        context["businessResult"] = "processed successfully";
        Console.WriteLine($"✓ {workflowSteps[2].Item1}");
        return context;
    })
    .Then(async context =>
    {
        // Step 4: Database update
        await Task.Delay(workflowSteps[3].Item2);
        context["dbResult"] = "updated";
        Console.WriteLine($"✓ {workflowSteps[3].Item1}");
        return context;
    })
    .Then(async context =>
    {
        // Step 5: Notifications (parallel execution)
        var notificationTasks = new[]
        {
            Task.Delay(25).ContinueWith(_ => "Email sent"),
            Task.Delay(30).ContinueWith(_ => "SMS sent"),
            Task.Delay(20).ContinueWith(_ => "Push notification sent")
        };
        
        var notifications = await Task.WhenAll(notificationTasks);
        context["notifications"] = notifications;
        Console.WriteLine($"✓ {workflowSteps[4].Item1}: {string.Join(", ", notifications)}");
        return context;
    })
    .Catch(ex =>
    {
        Console.WriteLine($"✗ Workflow failed: {ex.Message}");
        return new Dictionary<string, object> { ["error"] = ex.Message };
    })
    .Finally(() => Console.WriteLine("📋 Complex workflow completed"))
    .Timeout(TimeSpan.FromSeconds(5))
    .ExecuteAsync();

Console.WriteLine($"Complex workflow result: {complexWorkflowResult.Count} steps completed");

// Example 8: Performance Comparison
Console.WriteLine("\nPerformance Comparison Examples:");

const int taskCount = 1000;
const int maxConcurrency = 10;

// Test 1: Standard Task.WhenAll
var standardStopwatch = Stopwatch.StartNew();

var standardTasks = Enumerable.Range(1, taskCount).Select(async i =>
{
    await Task.Delay(1);
    return i * i;
});

var standardResults = await Task.WhenAll(standardTasks);
standardStopwatch.Stop();

Console.WriteLine($"Standard Task.WhenAll: {standardStopwatch.ElapsedMilliseconds}ms for {taskCount} tasks");

// Test 2: Parallel Executor with concurrency control
var parallelStopwatch = Stopwatch.StartNew();

var controlledResults = await parallelExecutor.ExecuteAsync(
    Enumerable.Range(1, taskCount),
    async (i, ct) =>
    {
        await Task.Delay(1, ct);
        return i * i;
    });

parallelStopwatch.Stop();

Console.WriteLine($"Parallel Executor: {parallelStopwatch.ElapsedMilliseconds}ms for {taskCount} tasks " +
                 $"(max concurrency: {maxConcurrency})");

// Test 3: Priority Scheduler
var priorityStopwatch = Stopwatch.StartNew();

var priorityTaskList = Enumerable.Range(1, taskCount).Select(i =>
    priorityScheduler.ScheduleAsync(
        async ct =>
        {
            await Task.Delay(1, ct);
            return i * i;
        },
        priority: i % 5)).ToArray();

var priorityResults = await Task.WhenAll(priorityTaskList);
priorityStopwatch.Stop();

Console.WriteLine($"Priority Scheduler: {priorityStopwatch.ElapsedMilliseconds}ms for {taskCount} tasks");

// Cleanup
parallelExecutor?.Dispose();
timeoutManager?.Dispose();
priorityScheduler?.Dispose();

Console.WriteLine("\nTask Combinator examples completed!");
```

**Notes**:

- Implement comprehensive task coordination patterns for complex async workflows and parallel execution
- Support task combinators (Race, All, AllSettled, Any) for different coordination strategies
- Use async workflow builders for composable, chainable async operations with error handling
- Implement parallel execution with configurable concurrency limits and failure handling strategies
- Support advanced timeout management with operation tracking and cancellation capabilities
- Provide priority-based task scheduling for workload management and resource optimization
- Use comprehensive performance metrics and monitoring for operational visibility
- Support both fail-fast and fault-tolerant execution patterns depending on requirements
- Implement proper cancellation token propagation throughout all coordination patterns
- Use extensive error handling and retry mechanisms for robust distributed system coordination
- Support both typed and untyped task operations with consistent patterns
- Provide performance comparison examples between different coordination strategies and standard approaches

**Prerequisites**:

- Understanding of Task-based asynchronous programming and async/await patterns
- Knowledge of cancellation tokens and timeout handling mechanisms
- Familiarity with parallel programming concepts and concurrency control
- Experience with error handling strategies in distributed and concurrent systems
- Understanding of performance optimization techniques for async operations

**Related Snippets**:

- [Async Enumerable](async-enumerable.md) - Streaming data processing with IAsyncEnumerable patterns
- [Producer Consumer](producer-consumer.md) - Producer-consumer patterns with channels and backpressure handling
- [Retry Pattern](retry-pattern.md) - Resilient retry mechanisms with exponential backoff
