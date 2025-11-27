using System.Diagnostics;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace CSharp.ProducerConsumer;

// Extension method for ToAsyncEnumerable

class Program
{
    static async Task Main(string[] args)
    {
        // Create logger factory for demonstration
        using var loggerFactory = LoggerFactory.Create(builder =>
            builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        var logger = loggerFactory.CreateLogger("ProducerConsumer");

        Console.WriteLine("Producer-Consumer Pattern Demonstrations");
        Console.WriteLine("======================================");

// Example 1: Basic Producer-Consumer with Channels
Console.WriteLine("\n1. Basic Producer-Consumer Examples:");

var channel = Channel.CreateBounded<string>(100);
var producer = new ChannelProducer<string>(channel.Writer, logger);
var consumer = new ChannelConsumer<string>(channel.Reader, logger);
var metrics = new ProducerConsumerMetrics();

// Subscribe to events for metrics
producer.ItemProduced += (sender, args) => 
    metrics.RecordItemProduced(TimeSpan.FromMilliseconds(1), args.QueueSize);

consumer.ItemConsumed += (sender, args) => 
    metrics.RecordItemConsumed(args.ProcessingTime);

// Start producer task
var producerTask = Task.Run(async () =>
{
    for (int i = 1; i <= 500; i++)
    {
        await producer.ProduceAsync($"Message-{i}");
        
        if (i % 100 == 0)
        {
            Console.WriteLine($"  Produced {i} messages");
        }
        
        await Task.Delay(1);
    }
    
    await producer.CompleteAsync();
});

// Start consumer task
var consumerTask = Task.Run(async () =>
{
    var consumedCount = 0;
    
    await foreach (var message in consumer.ConsumeAllAsync())
    {
        consumedCount++;
        
        // Simulate processing work
        await Task.Delay(2);
        
        if (consumedCount % 100 == 0)
        {
            Console.WriteLine($"  Consumed {consumedCount} messages");
        }
    }
    
    Console.WriteLine($"  Total consumed: {consumedCount}");
});

await Task.WhenAll(producerTask, consumerTask);

var stats = metrics.GetStats();
Console.WriteLine($"  Throughput - Producing: {stats.ProducingThroughput:F0}/sec, " +
                 $"Consuming: {stats.ConsumingThroughput:F0}/sec");
Console.WriteLine($"  Peak queue size: {stats.PeakQueueSize}, Efficiency: {stats.Efficiency:P1}");

// Example 2: Priority Producer-Consumer
Console.WriteLine("\n2. Priority Producer-Consumer Examples:");

var priorities = new[] { 1, 2, 3, 4, 5 }; // 5 is highest priority
var prioritySystem = new PriorityProducerConsumer<string>(priorities, 200, logger);

// Producer tasks with different priorities
var priorityProducerTasks = priorities.Select(priority =>
    Task.Run(async () =>
    {
        for (int i = 1; i <= 20; i++)
        {
            var message = $"Priority-{priority}-Message-{i}";
            await prioritySystem.ProduceAsync(message, priority);
            
            // Higher priority items produced less frequently
            await Task.Delay(priority * 5);
        }
    })
).ToArray();

// Consumer task
var priorityConsumerTask = Task.Run(async () =>
{
    var consumedByPriority = new Dictionary<int, int>();
    var totalConsumed = 0;
    
    await foreach (var (message, priority) in prioritySystem.ConsumeAllAsync())
    {
        consumedByPriority[priority] = consumedByPriority.GetValueOrDefault(priority) + 1;
        totalConsumed++;
        
        if (totalConsumed % 10 == 0)
        {
            Console.WriteLine($"  Consumed {totalConsumed} messages. Last: {message}");
        }
        
        // Stop when all producers are done and no items remain
        if (priorityProducerTasks.All(t => t.IsCompleted) && 
            priorities.All(p => prioritySystem.GetQueueCount(p) == 0))
        {
            break;
        }
    }
    
    Console.WriteLine("  Priority consumption distribution:");
    foreach (var kvp in consumedByPriority.OrderByDescending(x => x.Key))
    {
        Console.WriteLine($"    Priority {kvp.Key}: {kvp.Value} items");
    }
});

await Task.WhenAll(priorityProducerTasks);
prioritySystem.CompleteProduction();
await priorityConsumerTask;

// Example 3: Batch Processing Pipeline
Console.WriteLine("\n3. Batch Processing Examples:");

// Batch processor that simulates data transformation
var batchProcessor = new BatchProcessor<int, string>(
    async (batch, cancellationToken) =>
    {
        // Simulate batch processing work
        await Task.Delay(25, cancellationToken);
        
        return batch.Select(x => $"Processed-{x}-{DateTime.UtcNow.Ticks % 10000}");
    },
    batchSize: 10,
    batchTimeout: TimeSpan.FromMilliseconds(200),
    logger: logger
);

// Process stream of data
var streamData = Enumerable.Range(1, 85);
var batchResults = new List<string>();

var batchTask = Task.Run(async () =>
{
    await foreach (var result in batchProcessor.ProcessStreamAsync(streamData.ToAsyncEnumerable()))
    {
        batchResults.Add(result);
        
        if (batchResults.Count % 20 == 0)
        {
            Console.WriteLine($"  Batch processed {batchResults.Count} results");
        }
    }
});

await batchTask;

Console.WriteLine($"  Batch processing completed. Total results: {batchResults.Count}");

// Example 4: Backpressure-Aware Producer
Console.WriteLine("\n4. Backpressure Producer Examples:");

var backpressureProducer = new BackpressureProducer<int>(capacity: 250, initialRate: 150, logger: logger);
var backpressureMetrics = new ProducerConsumerMetrics();

// Fast producer
var fastProducerTask = Task.Run(async () =>
{
    for (int i = 1; i <= 1000; i++)
    {
        var stopwatch = Stopwatch.StartNew();
        await backpressureProducer.ProduceAsync(i);
        stopwatch.Stop();
        
        backpressureMetrics.RecordItemProduced(stopwatch.Elapsed, backpressureProducer.QueueSize);
        
        if (i % 100 == 0)
        {
            Console.WriteLine($"  Produced {i} items. Rate: {backpressureProducer.CurrentRate}/sec, " +
                            $"Queue: {backpressureProducer.QueueSize}");
        }
    }
    
    await backpressureProducer.CompleteAsync();
});

// Slow consumer
var slowConsumerTask = Task.Run(async () =>
{
    var consumedCount = 0;
    
    await foreach (var item in backpressureProducer.Reader.ReadAllAsync())
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Simulate slow processing
        await Task.Delay(Random.Shared.Next(1, 8));
        
        stopwatch.Stop();
        consumedCount++;
        
        backpressureMetrics.RecordItemConsumed(stopwatch.Elapsed);
        
        if (consumedCount % 100 == 0)
        {
            Console.WriteLine($"  Consumed {consumedCount} items");
        }
    }
    
    Console.WriteLine($"  Backpressure consumer finished: {consumedCount} items");
});

await Task.WhenAll(fastProducerTask, slowConsumerTask);

var backpressureStats = backpressureMetrics.GetStats();
Console.WriteLine($"  Backpressure Results - Efficiency: {backpressureStats.Efficiency:P2}, " +
                 $"Peak Queue: {backpressureStats.PeakQueueSize}");

// Example 5: Multi-Producer Multi-Consumer Scenario
Console.WriteLine("\n5. Multi-Producer Multi-Consumer Examples:");

var multiChannel = Channel.CreateBounded<WorkItem>(500);
var workItemMetrics = new ProducerConsumerMetrics();

// Multiple producers
var producerTasks = Enumerable.Range(1, 3).Select(producerId =>
    Task.Run(async () =>
    {
        var multiProducer = new ChannelProducer<WorkItem>(multiChannel.Writer, logger);
        
        for (int i = 1; i <= 50; i++)
        {
            var workItem = new WorkItem($"Producer-{producerId}-Work-{i}", 
                TimeSpan.FromMilliseconds(Random.Shared.Next(10, 50)));
            
            var stopwatch = Stopwatch.StartNew();
            await multiProducer.ProduceAsync(workItem);
            stopwatch.Stop();
            
            workItemMetrics.RecordItemProduced(stopwatch.Elapsed);
            
            await Task.Delay(Random.Shared.Next(5, 15));
        }
    })
).ToArray();

// Multiple consumers
var consumerTasks = Enumerable.Range(1, 2).Select(consumerId =>
    Task.Run(async () =>
    {
        var multiConsumer = new ChannelConsumer<WorkItem>(multiChannel.Reader, logger);
        var processedCount = 0;
        
        try
        {
            while (true)
            {
                var workItem = await multiConsumer.ConsumeAsync();
                var stopwatch = Stopwatch.StartNew();
                
                // Simulate work processing
                await Task.Delay(workItem.ProcessingTime);
                
                stopwatch.Stop();
                processedCount++;
                
                workItemMetrics.RecordItemConsumed(stopwatch.Elapsed);
                
                if (processedCount % 25 == 0)
                {
                    Console.WriteLine($"  Consumer-{consumerId} processed {processedCount} items");
                }
            }
        }
        catch (InvalidOperationException)
        {
            // Channel completed
            Console.WriteLine($"  Consumer-{consumerId} finished with {processedCount} items");
        }
    })
).ToArray();

// Wait for all producers to complete
await Task.WhenAll(producerTasks);
multiChannel.Writer.Complete();

// Wait for all consumers to complete
await Task.WhenAll(consumerTasks);

var multiStats = workItemMetrics.GetStats();
Console.WriteLine($"  Multi-producer/consumer results:");
Console.WriteLine($"    Items produced: {multiStats.ItemsProduced}");
Console.WriteLine($"    Items consumed: {multiStats.ItemsConsumed}");
Console.WriteLine($"    Producing throughput: {multiStats.ProducingThroughput:F1}/sec");
Console.WriteLine($"    Consuming throughput: {multiStats.ConsumingThroughput:F1}/sec");
Console.WriteLine($"    System efficiency: {multiStats.Efficiency:P2}");

// Example 6: Performance Comparison
Console.WriteLine("\n6. Performance Comparison Examples:");

const int itemCount = 5000;
var capacities = new[] { 10, 100, 500, 2500 };

foreach (var capacity in capacities)
{
    var testChannel = Channel.CreateBounded<int>(capacity);
    var testMetrics = new ProducerConsumerMetrics();
    
    var testStopwatch = Stopwatch.StartNew();
    
    var testProducer = Task.Run(async () =>
    {
        for (int i = 0; i < itemCount; i++)
        {
            var itemStopwatch = Stopwatch.StartNew();
            await testChannel.Writer.WriteAsync(i);
            itemStopwatch.Stop();
            
            testMetrics.RecordItemProduced(itemStopwatch.Elapsed, 
                testChannel.Reader.CanCount ? testChannel.Reader.Count : 0);
        }
        testChannel.Writer.Complete();
    });
    
    var testConsumer = Task.Run(async () =>
    {
        await foreach (var item in testChannel.Reader.ReadAllAsync())
        {
            var itemStopwatch = Stopwatch.StartNew();
            // Minimal processing
            itemStopwatch.Stop();
            
            testMetrics.RecordItemConsumed(itemStopwatch.Elapsed);
        }
    });
    
    await Task.WhenAll(testProducer, testConsumer);
    testStopwatch.Stop();
    
    var testStats = testMetrics.GetStats();
    Console.WriteLine($"  Capacity {capacity,4}: {testStopwatch.ElapsedMilliseconds,4}ms total, " +
                     $"{testStats.ProducingThroughput,8:F0}/sec producing, " +
                     $"peak queue: {testStats.PeakQueueSize,4}");
}

// Cleanup
producer?.Dispose();
consumer?.Dispose();
prioritySystem?.Dispose();
batchProcessor?.Dispose();
backpressureProducer?.Dispose();

Console.WriteLine("\n✅ Producer-Consumer pattern demonstrations completed!");
        Console.WriteLine("\nKey Observations:");
        Console.WriteLine("- Channel-based patterns provide high-performance producer-consumer scenarios");
        Console.WriteLine("- Priority queues enable importance-based message processing");
        Console.WriteLine("- Batch processing improves throughput for bulk operations");  
        Console.WriteLine("- Backpressure control prevents memory exhaustion in high-load scenarios");
        Console.WriteLine("- Multi-producer/multi-consumer patterns scale with concurrent processing");
        Console.WriteLine("- Proper capacity sizing significantly impacts performance characteristics");
    }
}