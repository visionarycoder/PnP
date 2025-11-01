# Message Queue Patterns

**Description**: Comprehensive message queuing implementation including queue management, message routing, dead letter queues, retry mechanisms, priority queuing, batch processing, and distributed message queue patterns for building scalable and reliable messaging systems.

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

// Core message queue interfaces
public interface IMessage
{
    Guid MessageId { get; }
    string MessageType { get; }
    DateTime Timestamp { get; }
    int Priority { get; }
    int RetryCount { get; }
    int MaxRetries { get; }
    TimeSpan? DelayUntil { get; }
    IDictionary<string, object> Headers { get; }
    byte[] Body { get; }
    string CorrelationId { get; }
    string ReplyTo { get; }
}

public interface IMessageQueue
{
    Task EnqueueAsync<T>(T message, CancellationToken token = default) where T : class;
    Task EnqueueAsync(IMessage message, CancellationToken token = default);
    Task<IMessage> DequeueAsync(CancellationToken token = default);
    Task<IMessage> DequeueAsync(TimeSpan timeout, CancellationToken token = default);
    Task<IEnumerable<IMessage>> DequeueBatchAsync(int batchSize, CancellationToken token = default);
    Task AcknowledgeAsync(Guid messageId, CancellationToken token = default);
    Task RejectAsync(Guid messageId, bool requeue = false, CancellationToken token = default);
    Task<int> GetQueueLengthAsync(CancellationToken token = default);
    Task PurgeAsync(CancellationToken token = default);
    string QueueName { get; }
}

public interface IMessageHandler<in T> where T : class
{
    Task HandleAsync(T message, IMessageContext context, CancellationToken token = default);
}

public interface IMessageContext
{
    Guid MessageId { get; }
    string MessageType { get; }
    DateTime Timestamp { get; }
    int RetryCount { get; }
    IDictionary<string, object> Headers { get; }
    string CorrelationId { get; }
    Task AcknowledgeAsync();
    Task RejectAsync(bool requeue = false);
    Task DelayAsync(TimeSpan delay);
}

public interface IMessageRouter
{
    Task RouteMessageAsync(IMessage message, CancellationToken token = default);
    void RegisterRoute<T>(string queueName) where T : class;
    void RegisterRoute(string messageType, string queueName);
    void RegisterRoute(Func<IMessage, bool> predicate, string queueName);
}

public interface IDeadLetterQueue
{
    Task SendToDeadLetterAsync(IMessage message, string reason, Exception exception = null, 
        CancellationToken token = default);
    Task<IMessage> GetFromDeadLetterAsync(CancellationToken token = default);
    Task<IEnumerable<IMessage>> GetDeadLettersAsync(int maxCount = 100, CancellationToken token = default);
    Task RequeueFromDeadLetterAsync(Guid messageId, CancellationToken token = default);
    Task<int> GetDeadLetterCountAsync(CancellationToken token = default);
}

// Message implementation
public class Message : IMessage
{
    public Message()
    {
        MessageId = Guid.NewGuid();
        Timestamp = DateTime.UtcNow;
        Headers = new();
        RetryCount = 0;
        MaxRetries = 3;
        Priority = 0;
    }

    public Guid MessageId { get; set; }
    public string MessageType { get; set; }
    public DateTime Timestamp { get; set; }
    public int Priority { get; set; }
    public int RetryCount { get; set; }
    public int MaxRetries { get; set; }
    public TimeSpan? DelayUntil { get; set; }
    public IDictionary<string, object> Headers { get; set; }
    public byte[] Body { get; set; }
    public string CorrelationId { get; set; }
    public string ReplyTo { get; set; }
}

public class TypedMessage<T> : Message where T : class
{
    public TypedMessage(T payload)
    {
        MessageType = typeof(T).Name;
        Payload = payload;
        Body = JsonSerializer.SerializeToUtf8Bytes(payload);
    }

    public T Payload { get; private set; }

    public static TypedMessage<T> FromMessage(IMessage message)
    {
        var payload = JsonSerializer.Deserialize<T>(message.Body);
        return new TypedMessage<T>(payload)
        {
            MessageId = message.MessageId,
            MessageType = message.MessageType,
            Timestamp = message.Timestamp,
            Priority = message.Priority,
            RetryCount = message.RetryCount,
            MaxRetries = message.MaxRetries,
            DelayUntil = message.DelayUntil,
            Headers = message.Headers,
            CorrelationId = message.CorrelationId,
            ReplyTo = message.ReplyTo
        };
    }
}

// In-memory message queue implementation
public class InMemoryMessageQueue : IMessageQueue
{
    private readonly PriorityQueue<IMessage, int> priorityQueue;
    private readonly ConcurrentDictionary<Guid, IMessage> processingMessages;
    private readonly SemaphoreSlim semaphore;
    private readonly ILogger logger;
    private readonly object lockObject = new();
    private volatile bool isDisposed = false;

    public InMemoryMessageQueue(string queueName, ILogger<InMemoryMessageQueue> logger = null)
    {
        QueueName = queueName ?? throw new ArgumentNullException(nameof(queueName));
        this.logger = logger;
        priorityQueue = new PriorityQueue<IMessage, int>();
        processingMessages = new();
        semaphore = new(0);
    }

    public string QueueName { get; }

    public Task EnqueueAsync<T>(T message, CancellationToken token = default) where T : class
    {
        var typedMessage = new TypedMessage<T>(message);
        return EnqueueAsync(typedMessage, token);
    }

    public Task EnqueueAsync(IMessage message, CancellationToken token = default)
    {
        if (isDisposed) throw new ObjectDisposedException(nameof(InMemoryMessageQueue));
        
        lock (lockObject)
        {
            // Check if message should be delayed
            if (message.DelayUntil.HasValue && message.DelayUntil.Value > TimeSpan.Zero)
            {
                _ = Task.Delay(message.DelayUntil.Value, token)
                    .ContinueWith(_ => EnqueueImmediateAsync(message), token);
                return Task.CompletedTask;
            }

            EnqueueImmediateAsync(message);
        }

        logger?.LogTrace("Enqueued message {MessageId} of type {MessageType} to queue {QueueName}",
            message.MessageId, message.MessageType, QueueName);

        return Task.CompletedTask;
    }

    private void EnqueueImmediateAsync(IMessage message)
    {
        lock (lockObject)
        {
            // Higher priority number = higher priority (processed first)
            // Negate priority for min-heap behavior (PriorityQueue is min-heap by default)
            priorityQueue.Enqueue(message, -message.Priority);
            semaphore.Release();
        }
    }

    public async Task<IMessage> DequeueAsync(CancellationToken token = default)
    {
        return await DequeueAsync(Timeout.InfiniteTimeSpan, token).ConfigureAwait(false);
    }

    public async Task<IMessage> DequeueAsync(TimeSpan timeout, CancellationToken token = default)
    {
        if (isDisposed) throw new ObjectDisposedException(nameof(InMemoryMessageQueue));

        var success = await semaphore.WaitAsync(timeout, token).ConfigureAwait(false);
        if (!success) return null;

        lock (lockObject)
        {
            if (priorityQueue.TryDequeue(out var message, out _))
            {
                processingMessages[message.MessageId] = message;
                logger?.LogTrace("Dequeued message {MessageId} from queue {QueueName}", 
                    message.MessageId, QueueName);
                return message;
            }
        }

        return null;
    }

    public async Task<IEnumerable<IMessage>> DequeueBatchAsync(int batchSize, CancellationToken token = default)
    {
        if (isDisposed) throw new ObjectDisposedException(nameof(InMemoryMessageQueue));

        var messages = new();
        
        for (int i = 0; i < batchSize && !token.IsCancellationRequested; i++)
        {
            var message = await DequeueAsync(TimeSpan.FromMilliseconds(100), token).ConfigureAwait(false);
            if (message == null) break;
            
            messages.Add(message);
        }

        logger?.LogTrace("Dequeued batch of {Count} messages from queue {QueueName}", 
            messages.Count, QueueName);

        return messages;
    }

    public Task AcknowledgeAsync(Guid messageId, CancellationToken token = default)
    {
        processingMessages.TryRemove(messageId, out _);
        logger?.LogTrace("Acknowledged message {MessageId} in queue {QueueName}", messageId, QueueName);
        return Task.CompletedTask;
    }

    public async Task RejectAsync(Guid messageId, bool requeue = false, CancellationToken token = default)
    {
        if (processingMessages.TryRemove(messageId, out var message))
        {
            if (requeue)
            {
                message.RetryCount++;
                await EnqueueAsync(message, token).ConfigureAwait(false);
                logger?.LogTrace("Requeued rejected message {MessageId} in queue {QueueName}", 
                    messageId, QueueName);
            }
            else
            {
                logger?.LogTrace("Rejected message {MessageId} in queue {QueueName} without requeue", 
                    messageId, QueueName);
            }
        }
    }

    public Task<int> GetQueueLengthAsync(CancellationToken token = default)
    {
        lock (lockObject)
        {
            return Task.FromResult(priorityQueue.Count + processingMessages.Count);
        }
    }

    public Task PurgeAsync(CancellationToken token = default)
    {
        lock (lockObject)
        {
            priorityQueue.Clear();
            processingMessages.Clear();
            
            // Reset semaphore
            while (semaphore.CurrentCount > 0)
            {
                semaphore.Wait(0);
            }
        }

        logger?.LogInformation("Purged all messages from queue {QueueName}", QueueName);
        return Task.CompletedTask;
    }
}

// Message queue manager
public class MessageQueueManager : IMessageQueueManager
{
    private readonly ConcurrentDictionary<string, IMessageQueue> queues;
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger logger;

    public MessageQueueManager(IServiceProvider serviceProvider, ILogger<MessageQueueManager> logger = null)
    {
        this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        this.logger = logger;
        queues = new();
    }

    public IMessageQueue GetOrCreateQueue(string queueName)
    {
        return queues.GetOrAdd(queueName, name =>
        {
            var queueLogger = serviceProvider.GetService<ILogger<InMemoryMessageQueue>>();
            var queue = new InMemoryMessageQueue(name, queueLogger);
            logger?.LogInformation("Created new queue: {QueueName}", name);
            return queue;
        });
    }

    public IMessageQueue GetQueue(string queueName)
    {
        queues.TryGetValue(queueName, out var queue);
        return queue;
    }

    public async Task<bool> DeleteQueueAsync(string queueName, CancellationToken token = default)
    {
        if (queues.TryRemove(queueName, out var queue))
        {
            await queue.PurgeAsync(token).ConfigureAwait(false);
            logger?.LogInformation("Deleted queue: {QueueName}", queueName);
            return true;
        }
        return false;
    }

    public IEnumerable<string> GetQueueNames()
    {
        return queues.Keys.ToList();
    }

    public async Task<Dictionary<string, int>> GetQueueLengthsAsync(CancellationToken token = default)
    {
        var lengths = new();
        
        foreach (var kvp in queues)
        {
            var length = await kvp.Value.GetQueueLengthAsync(token).ConfigureAwait(false);
            lengths[kvp.Key] = length;
        }

        return lengths;
    }
}

// Message router implementation
public class MessageRouter : IMessageRouter
{
    private readonly List<RouteRule> routeRules;
    private readonly IMessageQueueManager queueManager;
    private readonly ILogger logger;

    public MessageRouter(IMessageQueueManager queueManager, ILogger<MessageRouter> logger = null)
    {
        this.queueManager = queueManager ?? throw new ArgumentNullException(nameof(queueManager));
        this.logger = logger;
        routeRules = new();
    }

    public void RegisterRoute<T>(string queueName) where T : class
    {
        RegisterRoute(typeof(T).Name, queueName);
    }

    public void RegisterRoute(string messageType, string queueName)
    {
        var rule = new RouteRule
        {
            MessageType = messageType,
            QueueName = queueName,
            Predicate = msg => msg.MessageType == messageType
        };

        routeRules.Add(rule);
        logger?.LogInformation("Registered route: {MessageType} -> {QueueName}", messageType, queueName);
    }

    public void RegisterRoute(Func<IMessage, bool> predicate, string queueName)
    {
        var rule = new RouteRule
        {
            QueueName = queueName,
            Predicate = predicate
        };

        routeRules.Add(rule);
        logger?.LogInformation("Registered custom predicate route -> {QueueName}", queueName);
    }

    public async Task RouteMessageAsync(IMessage message, CancellationToken token = default)
    {
        var matchingRules = routeRules.Where(rule => rule.Predicate(message)).ToList();
        
        if (matchingRules.Count == 0)
        {
            logger?.LogWarning("No routing rule found for message {MessageId} of type {MessageType}", 
                message.MessageId, message.MessageType);
            return;
        }

        var routingTasks = matchingRules.Select(async rule =>
        {
            try
            {
                var queue = queueManager.GetOrCreateQueue(rule.QueueName);
                await queue.EnqueueAsync(message, token).ConfigureAwait(false);
                
                logger?.LogTrace("Routed message {MessageId} to queue {QueueName}", 
                    message.MessageId, rule.QueueName);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to route message {MessageId} to queue {QueueName}", 
                    message.MessageId, rule.QueueName);
            }
        });

        await Task.WhenAll(routingTasks).ConfigureAwait(false);
    }

    private class RouteRule
    {
        public string MessageType { get; set; }
        public string QueueName { get; set; }
        public Func<IMessage, bool> Predicate { get; set; }
    }
}

// Dead letter queue implementation
public class DeadLetterQueue : IDeadLetterQueue
{
    private readonly IMessageQueue deadLetterQueue;
    private readonly IMessageQueue originalQueue;
    private readonly ILogger logger;

    public DeadLetterQueue(
        IMessageQueue deadLetterQueue, 
        IMessageQueue originalQueue,
        ILogger<DeadLetterQueue> logger = null)
    {
        this.deadLetterQueue = deadLetterQueue ?? throw new ArgumentNullException(nameof(deadLetterQueue));
        this.originalQueue = originalQueue;
        this.logger = logger;
    }

    public async Task SendToDeadLetterAsync(IMessage message, string reason, 
        Exception exception = null, CancellationToken token = default)
    {
        // Add dead letter metadata
        message.Headers["DeadLetter.Reason"] = reason;
        message.Headers["DeadLetter.Timestamp"] = DateTime.UtcNow;
        message.Headers["DeadLetter.OriginalQueue"] = originalQueue?.QueueName ?? "Unknown";
        
        if (exception != null)
        {
            message.Headers["DeadLetter.Exception"] = exception.ToString();
        }

        await deadLetterQueue.EnqueueAsync(message, token).ConfigureAwait(false);
        
        logger?.LogWarning("Sent message {MessageId} to dead letter queue. Reason: {Reason}", 
            message.MessageId, reason);
    }

    public async Task<IMessage> GetFromDeadLetterAsync(CancellationToken token = default)
    {
        return await deadLetterQueue.DequeueAsync(token).ConfigureAwait(false);
    }

    public async Task<IEnumerable<IMessage>> GetDeadLettersAsync(int maxCount = 100, CancellationToken token = default)
    {
        return await deadLetterQueue.DequeueBatchAsync(maxCount, token).ConfigureAwait(false);
    }

    public async Task RequeueFromDeadLetterAsync(Guid messageId, CancellationToken token = default)
    {
        // In a real implementation, you'd need to track messages by ID
        // For this example, we'll implement a simple approach
        var message = await deadLetterQueue.DequeueAsync(TimeSpan.FromSeconds(1), token).ConfigureAwait(false);
        
        if (message != null && message.MessageId == messageId)
        {
            // Remove dead letter metadata
            message.Headers.Remove("DeadLetter.Reason");
            message.Headers.Remove("DeadLetter.Timestamp");
            message.Headers.Remove("DeadLetter.OriginalQueue");
            message.Headers.Remove("DeadLetter.Exception");
            
            // Reset retry count
            message.RetryCount = 0;
            
            if (originalQueue != null)
            {
                await originalQueue.EnqueueAsync(message, token).ConfigureAwait(false);
                logger?.LogInformation("Requeued message {MessageId} from dead letter queue", messageId);
            }
        }
    }

    public async Task<int> GetDeadLetterCountAsync(CancellationToken token = default)
    {
        return await deadLetterQueue.GetQueueLengthAsync(token).ConfigureAwait(false);
    }
}

// Message context implementation
public class MessageContext : IMessageContext
{
    private readonly IMessage message;
    private readonly IMessageQueue queue;
    private readonly IDeadLetterQueue deadLetterQueue;

    public MessageContext(IMessage message, IMessageQueue queue, IDeadLetterQueue deadLetterQueue = null)
    {
        this.message = message ?? throw new ArgumentNullException(nameof(message));
        this.queue = queue ?? throw new ArgumentNullException(nameof(queue));
        this.deadLetterQueue = deadLetterQueue;
    }

    public Guid MessageId => message.MessageId;
    public string MessageType => message.MessageType;
    public DateTime Timestamp => message.Timestamp;
    public int RetryCount => message.RetryCount;
    public IDictionary<string, object> Headers => message.Headers;
    public string CorrelationId => message.CorrelationId;

    public async Task AcknowledgeAsync()
    {
        await queue.AcknowledgeAsync(MessageId).ConfigureAwait(false);
    }

    public async Task RejectAsync(bool requeue = false)
    {
        if (requeue && RetryCount < message.MaxRetries)
        {
            await queue.RejectAsync(MessageId, requeue: true).ConfigureAwait(false);
        }
        else if (deadLetterQueue != null)
        {
            await deadLetterQueue.SendToDeadLetterAsync(message, 
                "Max retries exceeded or rejected without requeue").ConfigureAwait(false);
        }
        else
        {
            await queue.RejectAsync(MessageId, requeue: false).ConfigureAwait(false);
        }
    }

    public async Task DelayAsync(TimeSpan delay)
    {
        message.DelayUntil = delay;
        await queue.RejectAsync(MessageId, requeue: true).ConfigureAwait(false);
    }
}

// Message processor/consumer
public class MessageProcessor<T> : BackgroundService where T : class
{
    private readonly IMessageQueue queue;
    private readonly IMessageHandler<T> handler;
    private readonly IDeadLetterQueue deadLetterQueue;
    private readonly MessageProcessorOptions options;
    private readonly ILogger logger;

    public MessageProcessor(
        IMessageQueue queue,
        IMessageHandler<T> handler,
        IDeadLetterQueue deadLetterQueue = null,
        IOptions<MessageProcessorOptions> options = null,
        ILogger<MessageProcessor<T>> logger = null)
    {
        this.queue = queue ?? throw new ArgumentNullException(nameof(queue));
        this.handler = handler ?? throw new ArgumentNullException(nameof(handler));
        this.deadLetterQueue = deadLetterQueue;
        this.options = options?.Value ?? new MessageProcessorOptions();
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger?.LogInformation("Message processor started for queue {QueueName}", queue.QueueName);

        var concurrentTasks = new();
        var semaphore = new(options.MaxConcurrency);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await semaphore.WaitAsync(stoppingToken).ConfigureAwait(false);
                
                var processingTask = ProcessMessageBatchAsync(semaphore, stoppingToken);
                concurrentTasks.Add(processingTask);
                
                // Clean up completed tasks
                concurrentTasks.RemoveAll(t => t.IsCompleted);
                
                // Delay between batch processing
                await Task.Delay(options.BatchInterval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error in message processor main loop");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken).ConfigureAwait(false);
            }
        }

        // Wait for all remaining tasks to complete
        await Task.WhenAll(concurrentTasks).ConfigureAwait(false);
        
        logger?.LogInformation("Message processor stopped for queue {QueueName}", queue.QueueName);
    }

    private async Task ProcessMessageBatchAsync(SemaphoreSlim semaphore, CancellationToken stoppingToken)
    {
        try
        {
            var messages = await queue.DequeueBatchAsync(options.BatchSize, stoppingToken).ConfigureAwait(false);
            
            var processingTasks = messages.Select(message => 
                ProcessSingleMessageAsync(message, stoppingToken));
            
            await Task.WhenAll(processingTasks).ConfigureAwait(false);
        }
        finally
        {
            semaphore.Release();
        }
    }

    private async Task ProcessSingleMessageAsync(IMessage message, CancellationToken stoppingToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var context = new MessageContext(message, queue, deadLetterQueue);

        try
        {
            logger?.LogTrace("Processing message {MessageId} of type {MessageType}", 
                message.MessageId, message.MessageType);

            // Deserialize message
            var typedMessage = JsonSerializer.Deserialize<T>(message.Body);
            
            // Handle message
            await handler.HandleAsync(typedMessage, context, stoppingToken).ConfigureAwait(false);
            
            // Acknowledge successful processing
            await context.AcknowledgeAsync().ConfigureAwait(false);
            
            logger?.LogTrace("Successfully processed message {MessageId} in {ElapsedMs}ms", 
                message.MessageId, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to process message {MessageId} of type {MessageType}", 
                message.MessageId, message.MessageType);

            // Handle retry logic
            if (message.RetryCount < message.MaxRetries)
            {
                var retryDelay = CalculateRetryDelay(message.RetryCount);
                message.DelayUntil = retryDelay;
                await context.RejectAsync(requeue: true).ConfigureAwait(false);
                
                logger?.LogWarning("Requeuing message {MessageId} for retry {RetryCount}/{MaxRetries} after {DelayMs}ms", 
                    message.MessageId, message.RetryCount + 1, message.MaxRetries, retryDelay.TotalMilliseconds);
            }
            else
            {
                await context.RejectAsync(requeue: false).ConfigureAwait(false);
                logger?.LogError("Message {MessageId} failed after {MaxRetries} retries, sent to dead letter queue", 
                    message.MessageId, message.MaxRetries);
            }
        }
        finally
        {
            stopwatch.Stop();
        }
    }

    private TimeSpan CalculateRetryDelay(int retryCount)
    {
        // Exponential backoff with jitter
        var baseDelay = options.BaseRetryDelay.TotalMilliseconds;
        var exponentialDelay = Math.Pow(2, retryCount) * baseDelay;
        var jitter = Random.Shared.NextDouble() * 0.1 * exponentialDelay; // 10% jitter
        
        var totalDelay = Math.Min(exponentialDelay + jitter, options.MaxRetryDelay.TotalMilliseconds);
        return TimeSpan.FromMilliseconds(totalDelay);
    }
}

// Message batch processor for high-throughput scenarios
public class MessageBatchProcessor<T> : BackgroundService where T : class
{
    private readonly IMessageQueue queue;
    private readonly IMessageBatchHandler<T> batchHandler;
    private readonly BatchProcessorOptions options;
    private readonly ILogger logger;

    public MessageBatchProcessor(
        IMessageQueue queue,
        IMessageBatchHandler<T> batchHandler,
        IOptions<BatchProcessorOptions> options = null,
        ILogger<MessageBatchProcessor<T>> logger = null)
    {
        this.queue = queue ?? throw new ArgumentNullException(nameof(queue));
        this.batchHandler = batchHandler ?? throw new ArgumentNullException(nameof(batchHandler));
        this.options = options?.Value ?? new BatchProcessorOptions();
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger?.LogInformation("Batch message processor started for queue {QueueName}", queue.QueueName);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var batch = new List<BatchMessageItem<T>>();
                var batchTimeout = DateTime.UtcNow.Add(options.BatchTimeout);

                // Collect batch
                while (batch.Count < options.MaxBatchSize && DateTime.UtcNow < batchTimeout)
                {
                    var message = await queue.DequeueAsync(TimeSpan.FromMilliseconds(100), stoppingToken)
                        .ConfigureAwait(false);
                    
                    if (message != null)
                    {
                        try
                        {
                            var payload = JsonSerializer.Deserialize<T>(message.Body);
                            var context = new MessageContext(message, queue);
                            
                            batch.Add(new BatchMessageItem<T>
                            {
                                Message = message,
                                Payload = payload,
                                Context = context
                            });
                        }
                        catch (Exception ex)
                        {
                            logger?.LogError(ex, "Failed to deserialize message {MessageId}", message.MessageId);
                            await queue.RejectAsync(message.MessageId).ConfigureAwait(false);
                        }
                    }

                    if (stoppingToken.IsCancellationRequested) break;
                }

                // Process batch if not empty
                if (batch.Count > 0)
                {
                    await ProcessBatchAsync(batch, stoppingToken).ConfigureAwait(false);
                }

                // Small delay to prevent tight loop
                if (batch.Count == 0)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(100), stoppingToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error in batch message processor main loop");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken).ConfigureAwait(false);
            }
        }

        logger?.LogInformation("Batch message processor stopped for queue {QueueName}", queue.QueueName);
    }

    private async Task ProcessBatchAsync(List<BatchMessageItem<T>> batch, CancellationToken stoppingToken)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            logger?.LogTrace("Processing batch of {BatchSize} messages", batch.Count);

            var batchContext = new MessageBatchContext<T>(batch);
            await batchHandler.HandleBatchAsync(batchContext, stoppingToken).ConfigureAwait(false);

            // Acknowledge all successful messages
            var acknowledgeTasks = batch
                .Where(item => batchContext.IsSuccess(item.Message.MessageId))
                .Select(item => item.Context.AcknowledgeAsync());

            await Task.WhenAll(acknowledgeTasks).ConfigureAwait(false);

            // Handle failed messages
            var failedMessages = batch
                .Where(item => !batchContext.IsSuccess(item.Message.MessageId))
                .ToList();

            foreach (var failed in failedMessages)
            {
                await failed.Context.RejectAsync(requeue: true).ConfigureAwait(false);
            }

            logger?.LogTrace("Processed batch of {BatchSize} messages in {ElapsedMs}ms ({SuccessCount} successful, {FailCount} failed)",
                batch.Count, stopwatch.ElapsedMilliseconds, 
                batch.Count - failedMessages.Count, failedMessages.Count);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to process message batch of size {BatchSize}", batch.Count);

            // Reject all messages in batch for retry
            var rejectTasks = batch.Select(item => item.Context.RejectAsync(requeue: true));
            await Task.WhenAll(rejectTasks).ConfigureAwait(false);
        }
        finally
        {
            stopwatch.Stop();
        }
    }
}

// Supporting interfaces and classes
public interface IMessageQueueManager
{
    IMessageQueue GetOrCreateQueue(string queueName);
    IMessageQueue GetQueue(string queueName);
    Task<bool> DeleteQueueAsync(string queueName, CancellationToken token = default);
    IEnumerable<string> GetQueueNames();
    Task<Dictionary<string, int>> GetQueueLengthsAsync(CancellationToken token = default);
}

public interface IMessageBatchHandler<in T> where T : class
{
    Task HandleBatchAsync(IMessageBatchContext<T> batchContext, CancellationToken token = default);
}

public interface IMessageBatchContext<out T> where T : class
{
    IEnumerable<BatchMessageItem<T>> Items { get; }
    void MarkSuccess(Guid messageId);
    void MarkFailure(Guid messageId, Exception exception = null);
    bool IsSuccess(Guid messageId);
}

public class BatchMessageItem<T> where T : class
{
    public IMessage Message { get; set; }
    public T Payload { get; set; }
    public IMessageContext Context { get; set; }
}

public class MessageBatchContext<T> : IMessageBatchContext<T> where T : class
{
    private readonly ConcurrentDictionary<Guid, bool> processingResults;

    public MessageBatchContext(IEnumerable<BatchMessageItem<T>> items)
    {
        Items = items ?? throw new ArgumentNullException(nameof(items));
        processingResults = new();
    }

    public IEnumerable<BatchMessageItem<T>> Items { get; }

    public void MarkSuccess(Guid messageId)
    {
        processingResults[messageId] = true;
    }

    public void MarkFailure(Guid messageId, Exception exception = null)
    {
        processingResults[messageId] = false;
    }

    public bool IsSuccess(Guid messageId)
    {
        return processingResults.GetValueOrDefault(messageId, false);
    }
}

// Configuration options
public class MessageProcessorOptions
{
    public int MaxConcurrency { get; set; } = 10;
    public int BatchSize { get; set; } = 10;
    public TimeSpan BatchInterval { get; set; } = TimeSpan.FromMilliseconds(100);
    public TimeSpan BaseRetryDelay { get; set; } = TimeSpan.FromSeconds(1);
    public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromMinutes(5);
}

public class BatchProcessorOptions
{
    public int MaxBatchSize { get; set; } = 100;
    public TimeSpan BatchTimeout { get; set; } = TimeSpan.FromSeconds(5);
}

// Example message types for demonstration
public class OrderCreatedMessage
{
    public Guid OrderId { get; set; }
    public string CustomerEmail { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<OrderItem> Items { get; set; } = new();
}

public class OrderItem
{
    public string ProductId { get; set; }
    public string ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}

public class PaymentProcessedMessage
{
    public Guid PaymentId { get; set; }
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; }
    public PaymentStatus Status { get; set; }
    public DateTime ProcessedAt { get; set; }
}

public enum PaymentStatus
{
    Pending,
    Completed,
    Failed,
    Refunded
}

// Example message handlers
public class OrderCreatedMessageHandler : IMessageHandler<OrderCreatedMessage>
{
    private readonly ILogger logger;

    public OrderCreatedMessageHandler(ILogger<OrderCreatedMessageHandler> logger)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleAsync(OrderCreatedMessage message, IMessageContext context, CancellationToken token = default)
    {
        logger.LogInformation("Processing order created: {OrderId} for customer {CustomerEmail} with amount {Amount}",
            message.OrderId, message.CustomerEmail, message.TotalAmount);

        try
        {
            // Simulate processing
            await Task.Delay(Random.Shared.Next(100, 500), token).ConfigureAwait(false);

            // Simulate occasional failures for demonstration
            if (Random.Shared.NextDouble() < 0.1) // 10% failure rate
            {
                throw new InvalidOperationException($"Simulated processing failure for order {message.OrderId}");
            }

            // Process order logic here
            // - Validate order
            // - Reserve inventory
            // - Create shipment
            // - Send confirmation email

            logger.LogInformation("Successfully processed order {OrderId}", message.OrderId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process order {OrderId}", message.OrderId);
            throw; // Re-throw to trigger retry logic
        }
    }
}

public class PaymentBatchHandler : IMessageBatchHandler<PaymentProcessedMessage>
{
    private readonly ILogger logger;

    public PaymentBatchHandler(ILogger<PaymentBatchHandler> logger)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleBatchAsync(IMessageBatchContext<PaymentProcessedMessage> batchContext, CancellationToken token = default)
    {
        var payments = batchContext.Items.ToList();
        logger.LogInformation("Processing batch of {BatchSize} payments", payments.Count);

        try
        {
            // Batch processing logic
            var successfulPayments = new();
            var failedPayments = new();

            foreach (var payment in payments)
            {
                try
                {
                    // Simulate batch processing
                    await Task.Delay(10, token).ConfigureAwait(false);

                    // Process payment logic here
                    // - Update order status
                    // - Record payment in ledger
                    // - Trigger fulfillment

                    successfulPayments.Add(payment.Message.MessageId);
                    batchContext.MarkSuccess(payment.Message.MessageId);

                    logger.LogTrace("Processed payment {PaymentId} for order {OrderId}",
                        payment.Payload.PaymentId, payment.Payload.OrderId);
                }
                catch (Exception ex)
                {
                    failedPayments.Add(payment.Message.MessageId);
                    batchContext.MarkFailure(payment.Message.MessageId, ex);
                    
                    logger.LogWarning(ex, "Failed to process payment {PaymentId} in batch",
                        payment.Payload.PaymentId);
                }
            }

            logger.LogInformation("Batch processing completed: {SuccessCount} successful, {FailCount} failed",
                successfulPayments.Count, failedPayments.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Critical error during batch processing");
            
            // Mark all as failed
            foreach (var payment in payments)
            {
                batchContext.MarkFailure(payment.Message.MessageId, ex);
            }
        }
    }
}
```

**Usage**:

```csharp
// Example 1: Basic Message Queue Setup
Console.WriteLine("Basic Message Queue Examples:");

// Set up services
var services = new ServiceCollection()
    .AddLogging(builder => builder.AddConsole())
    .AddSingleton<IMessageQueueManager, MessageQueueManager>()
    .AddSingleton<IMessageRouter, MessageRouter>()
    .AddTransient<IMessageHandler<OrderCreatedMessage>, OrderCreatedMessageHandler>()
    .AddTransient<IMessageBatchHandler<PaymentProcessedMessage>, PaymentBatchHandler>()
    .Configure<MessageProcessorOptions>(options =>
    {
        options.MaxConcurrency = 5;
        options.BatchSize = 10;
        options.BatchInterval = TimeSpan.FromMilliseconds(100);
    })
    .Configure<BatchProcessorOptions>(options =>
    {
        options.MaxBatchSize = 50;
        options.BatchTimeout = TimeSpan.FromSeconds(2);
    })
    .BuildServiceProvider();

var queueManager = services.GetRequiredService<IMessageQueueManager>();
var router = services.GetRequiredService<IMessageRouter>();

// Create queues
var orderQueue = queueManager.GetOrCreateQueue("orders");
var paymentQueue = queueManager.GetOrCreateQueue("payments");
var deadLetterQueue = queueManager.GetOrCreateQueue("deadletter");

Console.WriteLine($"Created queues: {string.Join(", ", queueManager.GetQueueNames())}");

// Example 2: Message Routing
Console.WriteLine("\nMessage Routing Examples:");

// Set up routing rules
router.RegisterRoute<OrderCreatedMessage>("orders");
router.RegisterRoute<PaymentProcessedMessage>("payments");

// Custom routing based on message properties
router.RegisterRoute(
    message => message.Headers.ContainsKey("Priority") && 
               message.Headers["Priority"].ToString() == "High",
    "priority-orders");

// Route messages
var orderMessage = new OrderCreatedMessage
{
    OrderId = Guid.NewGuid(),
    CustomerEmail = "customer@example.com",
    TotalAmount = 99.99m,
    CreatedAt = DateTime.UtcNow,
    Items = new List<OrderItem>
    {
        new OrderItem 
        { 
            ProductId = "PROD-001", 
            ProductName = "Widget", 
            Quantity = 2, 
            Price = 49.99m 
        }
    }
};

var typedMessage = new TypedMessage<OrderCreatedMessage>(orderMessage);
await router.RouteMessageAsync(typedMessage);

Console.WriteLine($"Routed order message {orderMessage.OrderId}");

// Example 3: Priority Queue
Console.WriteLine("\nPriority Queue Examples:");

// Send messages with different priorities
var highPriorityOrder = new TypedMessage<OrderCreatedMessage>(new OrderCreatedMessage
{
    OrderId = Guid.NewGuid(),
    CustomerEmail = "vip@example.com",
    TotalAmount = 999.99m,
    CreatedAt = DateTime.UtcNow
})
{
    Priority = 10 // High priority
};

var normalPriorityOrder = new TypedMessage<OrderCreatedMessage>(new OrderCreatedMessage
{
    OrderId = Guid.NewGuid(),
    CustomerEmail = "normal@example.com",
    TotalAmount = 49.99m,
    CreatedAt = DateTime.UtcNow
})
{
    Priority = 5 // Normal priority
};

var lowPriorityOrder = new TypedMessage<OrderCreatedMessage>(new OrderCreatedMessage
{
    OrderId = Guid.NewGuid(),
    CustomerEmail = "bulk@example.com",
    TotalAmount = 9.99m,
    CreatedAt = DateTime.UtcNow
})
{
    Priority = 1 // Low priority
};

await orderQueue.EnqueueAsync(normalPriorityOrder);
await orderQueue.EnqueueAsync(lowPriorityOrder);
await orderQueue.EnqueueAsync(highPriorityOrder);

Console.WriteLine("Enqueued messages with different priorities");

// Dequeue messages (should come out in priority order)
Console.WriteLine("\nDequeuing messages by priority:");
for (int i = 0; i < 3; i++)
{
    var message = await orderQueue.DequeueAsync();
    if (message != null)
    {
        var order = JsonSerializer.Deserialize<OrderCreatedMessage>(message.Body);
        Console.WriteLine($"Priority {message.Priority}: Order {order.OrderId} for {order.CustomerEmail}");
        await orderQueue.AcknowledgeAsync(message.MessageId);
    }
}

// Example 4: Dead Letter Queue
Console.WriteLine("\nDead Letter Queue Examples:");

var dlq = new DeadLetterQueue(
    deadLetterQueue, 
    orderQueue, 
    services.GetService<ILogger<DeadLetterQueue>>());

// Simulate a failed message
var failedMessage = new TypedMessage<OrderCreatedMessage>(new OrderCreatedMessage
{
    OrderId = Guid.NewGuid(),
    CustomerEmail = "problematic@example.com",
    TotalAmount = -50.00m, // Invalid amount
    CreatedAt = DateTime.UtcNow
});

await dlq.SendToDeadLetterAsync(failedMessage, "Invalid order amount", 
    new ArgumentException("Order amount cannot be negative"));

Console.WriteLine($"Sent message to dead letter queue: {failedMessage.MessageId}");

// Check dead letter queue
var deadLetterCount = await dlq.GetDeadLetterCountAsync();
Console.WriteLine($"Dead letter queue contains {deadLetterCount} messages");

// Retrieve and inspect dead letter
var deadLetter = await dlq.GetFromDeadLetterAsync();
if (deadLetter != null)
{
    Console.WriteLine($"Dead letter reason: {deadLetter.Headers["DeadLetter.Reason"]}");
    Console.WriteLine($"Original queue: {deadLetter.Headers["DeadLetter.OriginalQueue"]}");
}

// Example 5: Message Processing with Retry Logic
Console.WriteLine("\nMessage Processing with Retry Examples:");

// Create message processor
var orderHandler = services.GetRequiredService<IMessageHandler<OrderCreatedMessage>>();
var processorOptions = services.GetRequiredService<IOptions<MessageProcessorOptions>>();

var processor = new MessageProcessor<OrderCreatedMessage>(
    orderQueue, 
    orderHandler, 
    dlq,
    processorOptions,
    services.GetService<ILogger<MessageProcessor<OrderCreatedMessage>>>());

// Start processing in background
var cts = new CancellationTokenSource();
var processingTask = processor.StartAsync(cts.Token);

// Enqueue some test messages
var testMessages = Enumerable.Range(1, 10).Select(i => new OrderCreatedMessage
{
    OrderId = Guid.NewGuid(),
    CustomerEmail = $"test{i}@example.com",
    TotalAmount = i * 10.0m,
    CreatedAt = DateTime.UtcNow
});

foreach (var testMessage in testMessages)
{
    await orderQueue.EnqueueAsync(testMessage);
}

Console.WriteLine("Enqueued 10 test messages for processing");

// Let it process for a while
await Task.Delay(TimeSpan.FromSeconds(3));

// Example 6: Batch Message Processing
Console.WriteLine("\nBatch Message Processing Examples:");

var paymentHandler = services.GetRequiredService<IMessageBatchHandler<PaymentProcessedMessage>>();
var batchOptions = services.GetRequiredService<IOptions<BatchProcessorOptions>>();

var batchProcessor = new MessageBatchProcessor<PaymentProcessedMessage>(
    paymentQueue,
    paymentHandler,
    batchOptions,
    services.GetService<ILogger<MessageBatchProcessor<PaymentProcessedMessage>>>());

// Start batch processing
var batchProcessingTask = batchProcessor.StartAsync(cts.Token);

// Enqueue payment messages
var paymentMessages = testMessages.Select(order => new PaymentProcessedMessage
{
    PaymentId = Guid.NewGuid(),
    OrderId = order.OrderId,
    Amount = order.TotalAmount,
    PaymentMethod = "CreditCard",
    Status = PaymentStatus.Completed,
    ProcessedAt = DateTime.UtcNow
});

foreach (var payment in paymentMessages)
{
    await paymentQueue.EnqueueAsync(payment);
}

Console.WriteLine("Enqueued payment messages for batch processing");

// Let batch processing run
await Task.Delay(TimeSpan.FromSeconds(2));

// Example 7: Queue Management and Monitoring
Console.WriteLine("\nQueue Management and Monitoring Examples:");

// Check queue lengths
var queueLengths = await queueManager.GetQueueLengthsAsync();
foreach (var kvp in queueLengths)
{
    Console.WriteLine($"Queue '{kvp.Key}': {kvp.Value} messages");
}

// Delayed message example
Console.WriteLine("\nDelayed Message Examples:");
var delayedMessage = new TypedMessage<OrderCreatedMessage>(new OrderCreatedMessage
{
    OrderId = Guid.NewGuid(),
    CustomerEmail = "delayed@example.com",
    TotalAmount = 75.00m,
    CreatedAt = DateTime.UtcNow
})
{
    DelayUntil = TimeSpan.FromSeconds(5) // Process after 5 seconds
};

await orderQueue.EnqueueAsync(delayedMessage);
Console.WriteLine($"Enqueued delayed message to be processed in 5 seconds");

// Message correlation example
Console.WriteLine("\nMessage Correlation Examples:");
var correlationId = Guid.NewGuid().ToString();

var correlatedOrder = new TypedMessage<OrderCreatedMessage>(new OrderCreatedMessage
{
    OrderId = Guid.NewGuid(),
    CustomerEmail = "correlated@example.com",
    TotalAmount = 125.00m,
    CreatedAt = DateTime.UtcNow
})
{
    CorrelationId = correlationId,
    ReplyTo = "order-responses"
};

correlatedOrder.Headers["RequestId"] = "REQ-123";
correlatedOrder.Headers["Source"] = "WebAPI";

await orderQueue.EnqueueAsync(correlatedOrder);
Console.WriteLine($"Enqueued correlated message with correlation ID: {correlationId}");

// Cleanup
cts.Cancel();
await Task.WhenAll(processingTask, batchProcessingTask);

Console.WriteLine("\nMessage queue pattern examples completed!");
```

**Notes**:

- Implement comprehensive message queuing patterns for reliable asynchronous communication
- Use priority queues to handle messages based on importance and urgency
- Implement dead letter queues for handling failed messages and poison message scenarios  
- Use retry mechanisms with exponential backoff and jitter to handle transient failures
- Support both single message and batch message processing for different throughput requirements
- Implement message routing for directing messages to appropriate queues based on content or metadata
- Use correlation IDs for tracking related messages across distributed systems
- Support delayed message delivery for scheduled processing scenarios
- Implement proper acknowledgment patterns to ensure message delivery guarantees
- Use concurrent processing with configurable concurrency limits for scalability
- Implement proper error handling and logging for observability and debugging
- Support message headers and metadata for additional context and routing information
- Use background services for continuous message processing in hosted environments
- Implement queue management features like purging, monitoring, and administrative operations

**Prerequisites**:

- Understanding of asynchronous messaging patterns and message-oriented middleware
- Knowledge of producer-consumer patterns and concurrent programming
- Familiarity with retry patterns and error handling strategies  
- Experience with dependency injection and background services in .NET
- Understanding of serialization and data contracts for message payloads
- Knowledge of distributed system reliability patterns

**Related Snippets**:

- [Event Sourcing](event-sourcing.md) - Event sourcing patterns for audit trails and replay
- [Pub-Sub Pattern](pub-sub.md) - Publisher-subscriber patterns for event distribution  
- [Saga Patterns](saga-patterns.md) - Distributed transaction coordination patterns
