# Enterprise ML Batch Processing & Scale

**Description**: Advanced enterprise-scale batch processing with intelligent resource allocation, distributed computation, fault tolerance, comprehensive monitoring, cost optimization, and elastic scaling for high-throughput ML workloads in production environments.

**Language/Technology**: C#, ML.NET, .NET 9.0, Azure Batch, Kubernetes, Distributed Computing, Cost Optimization
**Enterprise Features**: Intelligent resource allocation, distributed computation, fault tolerance, cost optimization, elastic scaling, and comprehensive monitoring integration

**Code**:

## Batch Processing Architecture

```csharp
namespace DocumentProcessor.ML.Batch;

using Microsoft.ML;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;
using System.Collections.Concurrent;

public interface IBatchProcessor<TInput, TOutput>
{
    Task<BatchResult<TOutput>> ProcessBatchAsync(
        IEnumerable<TInput> items, 
        CancellationToken cancellationToken = default);
    
    Task<IAsyncEnumerable<TOutput>> ProcessStreamAsync(
        IAsyncEnumerable<TInput> items,
        CancellationToken cancellationToken = default);
    
    Task<BatchResult<TOutput>> ProcessParallelAsync(
        IEnumerable<TInput> items,
        int maxConcurrency = Environment.ProcessorCount,
        CancellationToken cancellationToken = default);
}

public class MLBatchProcessor<TInput, TOutput> : IBatchProcessor<TInput, TOutput>
    where TInput : class
    where TOutput : class, new()
{
    private readonly MLContext mlContext;
    private readonly ITransformer model;
    private readonly ILogger<MLBatchProcessor<TInput, TOutput>> logger;
    private readonly BatchProcessorOptions options;

    public MLBatchProcessor(
        MLContext mlContext,
        ITransformer model,
        ILogger<MLBatchProcessor<TInput, TOutput>> logger,
        BatchProcessorOptions options)
    {
        mlContext = mlContext;
        model = model;
        logger = logger;
        options = options;
    }

    public async Task<BatchResult<TOutput>> ProcessBatchAsync(
        IEnumerable<TInput> items,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var inputList = items.ToList();
        
        logger.LogInformation("Starting batch processing of {Count} items", inputList.Count);

        try
        {
            // Load data into ML.NET DataView
            var dataView = mlContext.Data.LoadFromEnumerable(inputList);
            
            // Apply transformations
            var transformedData = model.Transform(dataView);
            
            // Extract results
            var results = mlContext.Data
                .CreateEnumerable<TOutput>(transformedData, reuseRowObject: false)
                .ToList();

            stopwatch.Stop();
            
            var batchResult = new BatchResult<TOutput>(
                Results: results,
                InputCount: inputList.Count,
                OutputCount: results.Count,
                ProcessingTimeMs: stopwatch.ElapsedMilliseconds,
                ThroughputPerSecond: inputList.Count / stopwatch.Elapsed.TotalSeconds,
                Success: true,
                Errors: new List<BatchError>());

            logger.LogInformation(
                "Batch processing completed: {Count} items in {Duration}ms ({Throughput:F2} items/sec)",
                results.Count, stopwatch.ElapsedMilliseconds, batchResult.ThroughputPerSecond);

            return batchResult;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex, "Batch processing failed after {Duration}ms", stopwatch.ElapsedMilliseconds);
            
            return new BatchResult<TOutput>(
                Results: new List<TOutput>(),
                InputCount: inputList.Count,
                OutputCount: 0,
                ProcessingTimeMs: stopwatch.ElapsedMilliseconds,
                ThroughputPerSecond: 0,
                Success: false,
                Errors: new List<BatchError> { new(ex.Message, ex.GetType().Name) });
        }
    }

    public async Task<IAsyncEnumerable<TOutput>> ProcessStreamAsync(
        IAsyncEnumerable<TInput> items,
        CancellationToken cancellationToken = default)
    {
        var channel = Channel.CreateUnbounded<TOutput>();
        var writer = channel.Writer;

        _ = Task.Run(async () =>
        {
            var batch = new List<TInput>();
            var batchCount = 0;

            try
            {
                await foreach (var item in items.WithCancellation(cancellationToken))
                {
                    batch.Add(item);
                    
                    if (batch.Count >= options.StreamBatchSize)
                    {
                        await ProcessAndWriteBatch(batch, writer, ++batchCount, cancellationToken);
                        batch.Clear();
                    }
                }

                // Process remaining items
                if (batch.Count > 0)
                {
                    await ProcessAndWriteBatch(batch, writer, ++batchCount, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Stream processing failed");
            }
            finally
            {
                writer.Complete();
            }
        }, cancellationToken);

        return channel.Reader.ReadAllAsync(cancellationToken);
    }

    public async Task<BatchResult<TOutput>> ProcessParallelAsync(
        IEnumerable<TInput> items,
        int maxConcurrency = 0,
        CancellationToken cancellationToken = default)
    {
        if (maxConcurrency <= 0)
            maxConcurrency = Environment.ProcessorCount;

        var stopwatch = Stopwatch.StartNew();
        var inputList = items.ToList();
        var chunkSize = Math.Max(1, inputList.Count / maxConcurrency);
        
        logger.LogInformation(
            "Starting parallel batch processing: {Count} items, {Concurrency} threads, {ChunkSize} items per chunk",
            inputList.Count, maxConcurrency, chunkSize);

        var chunks = inputList.Chunk(chunkSize).ToList();
        var results = new ConcurrentBag<TOutput>();
        var errors = new ConcurrentBag<BatchError>();

        var semaphore = new SemaphoreSlim(maxConcurrency);
        var tasks = chunks.Select(async (chunk, chunkIndex) =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                var chunkResult = await ProcessChunk(chunk.ToList(), chunkIndex, cancellationToken);
                foreach (var result in chunkResult.Results)
                {
                    results.Add(result);
                }
                foreach (var error in chunkResult.Errors)
                {
                    errors.Add(error);
                }
            }
            finally
            {
                semaphore.Release();
            }
        }).ToArray();

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        var finalResults = results.ToList();
        var finalErrors = errors.ToList();

        var batchResult = new BatchResult<TOutput>(
            Results: finalResults,
            InputCount: inputList.Count,
            OutputCount: finalResults.Count,
            ProcessingTimeMs: stopwatch.ElapsedMilliseconds,
            ThroughputPerSecond: inputList.Count / stopwatch.Elapsed.TotalSeconds,
            Success: finalErrors.Count == 0,
            Errors: finalErrors);

        logger.LogInformation(
            "Parallel batch processing completed: {Count} items in {Duration}ms ({Throughput:F2} items/sec), {ErrorCount} errors",
            finalResults.Count, stopwatch.ElapsedMilliseconds, batchResult.ThroughputPerSecond, finalErrors.Count);

        return batchResult;
    }

    private async Task ProcessAndWriteBatch(
        List<TInput> batch,
        ChannelWriter<TOutput> writer,
        int batchNumber,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await ProcessBatchAsync(batch, cancellationToken);
            
            foreach (var item in result.Results)
            {
                await writer.WriteAsync(item, cancellationToken);
            }

            logger.LogDebug("Processed stream batch {BatchNumber}: {Count} items", batchNumber, batch.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process stream batch {BatchNumber}", batchNumber);
        }
    }

    private async Task<BatchResult<TOutput>> ProcessChunk(
        List<TInput> chunk,
        int chunkIndex,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogDebug("Processing chunk {ChunkIndex} with {Count} items", chunkIndex, chunk.Count);
            return await ProcessBatchAsync(chunk, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process chunk {ChunkIndex}", chunkIndex);
            return new BatchResult<TOutput>(
                Results: new List<TOutput>(),
                InputCount: chunk.Count,
                OutputCount: 0,
                ProcessingTimeMs: 0,
                ThroughputPerSecond: 0,
                Success: false,
                Errors: new List<BatchError> { new($"Chunk {chunkIndex}: {ex.Message}", ex.GetType().Name) });
        }
    }
}

public record BatchResult<T>(
    List<T> Results,
    int InputCount,
    int OutputCount,
    long ProcessingTimeMs,
    double ThroughputPerSecond,
    bool Success,
    List<BatchError> Errors);

public record BatchError(string Message, string ErrorType);

public class BatchProcessorOptions
{
    public int StreamBatchSize { get; set; } = 1000;
    public int MaxRetries { get; set; } = 3;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);
    public bool EnableMetrics { get; set; } = true;
    public bool EnableDetailedLogging { get; set; } = false;
}
```

## Document Classification Batch Processor

```csharp
namespace DocumentProcessor.ML.Batch;

using Microsoft.ML.Data;

[Serializable]
public class BatchDocumentInput
{
    [LoadColumn(0)] public string Id { get; set; } = string.Empty;
    [LoadColumn(1)] public string Text { get; set; } = string.Empty;
    [LoadColumn(2)] public string Source { get; set; } = string.Empty;
    [LoadColumn(3)] public DateTime Timestamp { get; set; }
}

[Serializable]
public class BatchDocumentOutput
{
    public string Id { get; set; } = string.Empty;
    public string PredictedCategory { get; set; } = string.Empty;
    public float Confidence { get; set; }
    public Dictionary<string, float> CategoryScores { get; set; } = new();
    public string Source { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; }
    public TimeSpan ProcessingDuration { get; set; }
}

public interface IDocumentBatchProcessor
{
    Task<BatchResult<BatchDocumentOutput>> ProcessDocumentsAsync(
        IEnumerable<BatchDocumentInput> documents,
        CancellationToken cancellationToken = default);
    
    Task<BatchProcessingReport> ProcessDocumentFileAsync(
        string inputFilePath,
        string outputFilePath,
        CancellationToken cancellationToken = default);
    
    Task<IAsyncEnumerable<BatchDocumentOutput>> ProcessDocumentStreamAsync(
        IAsyncEnumerable<BatchDocumentInput> documents,
        CancellationToken cancellationToken = default);
}

public class DocumentBatchProcessor : IDocumentBatchProcessor
{
    private readonly IBatchProcessor<BatchDocumentInput, BatchDocumentOutput> batchProcessor;
    private readonly IDocumentClassifier classifier;
    private readonly ILogger<DocumentBatchProcessor> logger;
    private readonly DocumentBatchOptions options;

    public DocumentBatchProcessor(
        IBatchProcessor<BatchDocumentInput, BatchDocumentOutput> batchProcessor,
        IDocumentClassifier classifier,
        ILogger<DocumentBatchProcessor> logger,
        IOptions<DocumentBatchOptions> options)
    {
        batchProcessor = batchProcessor;
        classifier = classifier;
        logger = logger;
        options = options.Value;
    }

    public async Task<BatchResult<BatchDocumentOutput>> ProcessDocumentsAsync(
        IEnumerable<BatchDocumentInput> documents,
        CancellationToken cancellationToken = default)
    {
        var documentsWithClassification = documents.Select(doc => new DocumentWithClassifier(doc, classifier));
        
        // Transform to ML.NET compatible format
        var mlInputs = documentsWithClassification.Select(dwc => new MLDocumentInput
        {
            Id = dwc.Document.Id,
            Text = dwc.Document.Text,
            Source = dwc.Document.Source,
            Timestamp = dwc.Document.Timestamp
        });

        var mlProcessor = new MLDocumentProcessor(classifier, logger);
        var result = await mlProcessor.ProcessBatchAsync(mlInputs, cancellationToken);

        return new BatchResult<BatchDocumentOutput>(
            Results: result.Results.Select(r => new BatchDocumentOutput
            {
                Id = r.Id,
                PredictedCategory = r.PredictedCategory,
                Confidence = r.Confidence,
                CategoryScores = r.CategoryScores,
                Source = r.Source,
                ProcessedAt = DateTime.UtcNow,
                ProcessingDuration = TimeSpan.FromMilliseconds(result.ProcessingTimeMs / result.Results.Count)
            }).ToList(),
            InputCount: result.InputCount,
            OutputCount: result.OutputCount,
            ProcessingTimeMs: result.ProcessingTimeMs,
            ThroughputPerSecond: result.ThroughputPerSecond,
            Success: result.Success,
            Errors: result.Errors);
    }

    public async Task<BatchProcessingReport> ProcessDocumentFileAsync(
        string inputFilePath,
        string outputFilePath,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Processing document file: {InputPath} -> {OutputPath}", inputFilePath, outputFilePath);

        var stopwatch = Stopwatch.StartNew();
        var processedCount = 0;
        var errorCount = 0;
        var categories = new Dictionary<string, int>();

        using var reader = new StreamReader(inputFilePath);
        using var writer = new StreamWriter(outputFilePath);

        // Write CSV header
        await writer.WriteLineAsync("Id,Text,PredictedCategory,Confidence,Source,ProcessedAt");

        var batch = new List<BatchDocumentInput>();
        
        string? line;
        while ((line = await reader.ReadLineAsync()) != null && !cancellationToken.IsCancellationRequested)
        {
            if (TryParseDocumentLine(line, out var document))
            {
                batch.Add(document);
                
                if (batch.Count >= options.FileBatchSize)
                {
                    var batchResult = await ProcessAndWriteBatch(batch, writer, categories, cancellationToken);
                    processedCount += batchResult.ProcessedCount;
                    errorCount += batchResult.ErrorCount;
                    batch.Clear();
                }
            }
        }

        // Process remaining documents
        if (batch.Count > 0)
        {
            var batchResult = await ProcessAndWriteBatch(batch, writer, categories, cancellationToken);
            processedCount += batchResult.ProcessedCount;
            errorCount += batchResult.ErrorCount;
        }

        stopwatch.Stop();

        var report = new BatchProcessingReport(
            InputFilePath: inputFilePath,
            OutputFilePath: outputFilePath,
            ProcessedCount: processedCount,
            ErrorCount: errorCount,
            ProcessingTimeMs: stopwatch.ElapsedMilliseconds,
            ThroughputPerSecond: processedCount / stopwatch.Elapsed.TotalSeconds,
            CategoryDistribution: categories,
            CompletedAt: DateTime.UtcNow);

        logger.LogInformation(
            "File processing completed: {ProcessedCount} documents, {ErrorCount} errors, {Duration}ms ({Throughput:F2} docs/sec)",
            processedCount, errorCount, stopwatch.ElapsedMilliseconds, report.ThroughputPerSecond);

        return report;
    }

    public async Task<IAsyncEnumerable<BatchDocumentOutput>> ProcessDocumentStreamAsync(
        IAsyncEnumerable<BatchDocumentInput> documents,
        CancellationToken cancellationToken = default)
    {
        return batchProcessor.ProcessStreamAsync(documents, cancellationToken);
    }

    private async Task<(int ProcessedCount, int ErrorCount)> ProcessAndWriteBatch(
        List<BatchDocumentInput> batch,
        StreamWriter writer,
        Dictionary<string, int> categories,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await ProcessDocumentsAsync(batch, cancellationToken);
            
            foreach (var doc in result.Results)
            {
                var csvLine = $"{EscapeCsv(doc.Id)},{EscapeCsv(doc.Source)},{EscapeCsv(doc.PredictedCategory)},{doc.Confidence:F4},{EscapeCsv(doc.Source)},{doc.ProcessedAt:yyyy-MM-dd HH:mm:ss}";
                await writer.WriteLineAsync(csvLine);
                
                categories[doc.PredictedCategory] = categories.GetValueOrDefault(doc.PredictedCategory, 0) + 1;
            }

            return (result.Results.Count, result.Errors.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process batch of {Count} documents", batch.Count);
            return (0, batch.Count);
        }
    }

    private bool TryParseDocumentLine(string line, out BatchDocumentInput document)
    {
        document = new BatchDocumentInput();
        
        try
        {
            var parts = ParseCsvLine(line);
            if (parts.Length >= 3)
            {
                document.Id = parts[0];
                document.Text = parts[1];
                document.Source = parts.Length > 2 ? parts[2] : "unknown";
                document.Timestamp = parts.Length > 3 && DateTime.TryParse(parts[3], out var timestamp) 
                    ? timestamp 
                    : DateTime.UtcNow;
                return true;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to parse document line: {Line}", line);
        }
        
        return false;
    }

    private string[] ParseCsvLine(string line)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;
        
        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];
            
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        
        result.Add(current.ToString());
        return result.ToArray();
    }

    private string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        
        return value;
    }
}

// ML.NET compatible classes for the batch processor
[Serializable]
internal class MLDocumentInput
{
    [LoadColumn(0)] public string Id { get; set; } = string.Empty;
    [LoadColumn(1)] public string Text { get; set; } = string.Empty;
    [LoadColumn(2)] public string Source { get; set; } = string.Empty;
    [LoadColumn(3)] public DateTime Timestamp { get; set; }
}

[Serializable]
internal class MLDocumentOutput
{
    public string Id { get; set; } = string.Empty;
    public string PredictedCategory { get; set; } = string.Empty;
    public float Confidence { get; set; }
    public Dictionary<string, float> CategoryScores { get; set; } = new();
    public string Source { get; set; } = string.Empty;
}

internal class MLDocumentProcessor : IBatchProcessor<MLDocumentInput, MLDocumentOutput>
{
    private readonly IDocumentClassifier classifier;
    private readonly ILogger logger;

    public MLDocumentProcessor(IDocumentClassifier classifier, ILogger logger)
    {
        classifier = classifier;
        logger = logger;
    }

    public async Task<BatchResult<MLDocumentOutput>> ProcessBatchAsync(
        IEnumerable<MLDocumentInput> items,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var inputList = items.ToList();
        var results = new List<MLDocumentOutput>();
        var errors = new List<BatchError>();

        foreach (var item in inputList)
        {
            try
            {
                var prediction = await classifier.ClassifyAsync(item.Text);
                results.Add(new MLDocumentOutput
                {
                    Id = item.Id,
                    PredictedCategory = prediction.PredictedCategory,
                    Confidence = prediction.Confidence,
                    CategoryScores = prediction.CategoryScores,
                    Source = item.Source
                });
            }
            catch (Exception ex)
            {
                errors.Add(new BatchError($"Document {item.Id}: {ex.Message}", ex.GetType().Name));
                logger.LogWarning(ex, "Failed to classify document {DocumentId}", item.Id);
            }
        }

        stopwatch.Stop();

        return new BatchResult<MLDocumentOutput>(
            Results: results,
            InputCount: inputList.Count,
            OutputCount: results.Count,
            ProcessingTimeMs: stopwatch.ElapsedMilliseconds,
            ThroughputPerSecond: inputList.Count / stopwatch.Elapsed.TotalSeconds,
            Success: errors.Count == 0,
            Errors: errors);
    }

    public async Task<IAsyncEnumerable<MLDocumentOutput>> ProcessStreamAsync(
        IAsyncEnumerable<MLDocumentInput> items,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<BatchResult<MLDocumentOutput>> ProcessParallelAsync(
        IEnumerable<MLDocumentInput> items,
        int maxConcurrency = 0,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}

internal record DocumentWithClassifier(BatchDocumentInput Document, IDocumentClassifier Classifier);

public record BatchProcessingReport(
    string InputFilePath,
    string OutputFilePath,
    int ProcessedCount,
    int ErrorCount,
    long ProcessingTimeMs,
    double ThroughputPerSecond,
    Dictionary<string, int> CategoryDistribution,
    DateTime CompletedAt);

public class DocumentBatchOptions
{
    public int FileBatchSize { get; set; } = 5000;
    public int MaxConcurrency { get; set; } = Environment.ProcessorCount;
    public string OutputFormat { get; set; } = "csv";
    public bool IncludeMetrics { get; set; } = true;
}
```

## Sentiment Analysis Batch Processing

```csharp
namespace DocumentProcessor.ML.Batch;

public interface ISentimentBatchProcessor
{
    Task<BatchSentimentResult> ProcessSentimentBatchAsync(
        IEnumerable<string> texts,
        CancellationToken cancellationToken = default);
    
    Task<SentimentAggregateReport> AnalyzeSentimentDistributionAsync(
        IEnumerable<string> texts,
        CancellationToken cancellationToken = default);
}

public class SentimentBatchProcessor : ISentimentBatchProcessor
{
    private readonly ISentimentAnalyzer sentimentAnalyzer;
    private readonly IBatchProcessor<SentimentInput, SentimentOutput> batchProcessor;
    private readonly ILogger<SentimentBatchProcessor> logger;

    public SentimentBatchProcessor(
        ISentimentAnalyzer sentimentAnalyzer,
        IBatchProcessor<SentimentInput, SentimentOutput> batchProcessor,
        ILogger<SentimentBatchProcessor> logger)
    {
        sentimentAnalyzer = sentimentAnalyzer;
        batchProcessor = batchProcessor;
        logger = logger;
    }

    public async Task<BatchSentimentResult> ProcessSentimentBatchAsync(
        IEnumerable<string> texts,
        CancellationToken cancellationToken = default)
    {
        var inputs = texts.Select((text, index) => new SentimentInput
        {
            Id = index.ToString(),
            Text = text
        });

        var processingResult = await batchProcessor.ProcessBatchAsync(inputs, cancellationToken);

        var sentimentResults = processingResult.Results.Select(r => new SentimentResult
        {
            Id = r.Id,
            Text = r.Text,
            IsPositive = r.IsPositive,
            Probability = r.Probability,
            SentimentClass = r.SentimentClass,
            Confidence = r.Confidence
        }).ToList();

        return new BatchSentimentResult(
            Results: sentimentResults,
            InputCount: processingResult.InputCount,
            ProcessingTimeMs: processingResult.ProcessingTimeMs,
            ThroughputPerSecond: processingResult.ThroughputPerSecond,
            Success: processingResult.Success,
            Errors: processingResult.Errors);
    }

    public async Task<SentimentAggregateReport> AnalyzeSentimentDistributionAsync(
        IEnumerable<string> texts,
        CancellationToken cancellationToken = default)
    {
        var batchResult = await ProcessSentimentBatchAsync(texts, cancellationToken);
        
        if (!batchResult.Success)
        {
            throw new InvalidOperationException($"Sentiment analysis failed: {string.Join(", ", batchResult.Errors.Select(e => e.Message))}");
        }

        var results = batchResult.Results;
        var distribution = results
            .GroupBy(r => r.SentimentClass)
            .ToDictionary(g => g.Key, g => g.Count());

        var positiveCount = results.Count(r => r.IsPositive);
        var negativeCount = results.Count(r => !r.IsPositive);
        var averageConfidence = results.Average(r => r.Confidence);
        var averageProbability = results.Average(r => r.Probability);

        return new SentimentAggregateReport(
            TotalCount: results.Count,
            PositiveCount: positiveCount,
            NegativeCount: negativeCount,
            PositivePercentage: (double)positiveCount / results.Count * 100,
            Distribution: distribution,
            AverageConfidence: averageConfidence,
            AverageProbability: averageProbability,
            ProcessingTimeMs: batchResult.ProcessingTimeMs,
            ThroughputPerSecond: batchResult.ThroughputPerSecond);
    }
}

[Serializable]
public class SentimentInput
{
    [LoadColumn(0)] public string Id { get; set; } = string.Empty;
    [LoadColumn(1)] public string Text { get; set; } = string.Empty;
}

[Serializable]
public class SentimentOutput
{
    public string Id { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public bool IsPositive { get; set; }
    public float Probability { get; set; }
    public SentimentClass SentimentClass { get; set; }
    public double Confidence { get; set; }
}

public record SentimentResult(
    string Id,
    string Text,
    bool IsPositive,
    float Probability,
    SentimentClass SentimentClass,
    double Confidence);

public record BatchSentimentResult(
    List<SentimentResult> Results,
    int InputCount,
    long ProcessingTimeMs,
    double ThroughputPerSecond,
    bool Success,
    List<BatchError> Errors);

public record SentimentAggregateReport(
    int TotalCount,
    int PositiveCount,
    int NegativeCount,
    double PositivePercentage,
    Dictionary<SentimentClass, int> Distribution,
    double AverageConfidence,
    double AverageProbability,
    long ProcessingTimeMs,
    double ThroughputPerSecond);
```

## Service Registration

```csharp
namespace DocumentProcessor.ML.Batch;

public static class BatchProcessingServiceExtensions
{
    public static IServiceCollection AddBatchProcessing(this IServiceCollection services, IConfiguration configuration)
    {
        // Register batch processor options
        services.Configure<BatchProcessorOptions>(configuration.GetSection("ML:BatchProcessing"));
        services.Configure<DocumentBatchOptions>(configuration.GetSection("ML:DocumentBatch"));
        
        // Register generic batch processor
        services.AddScoped(typeof(IBatchProcessor<,>), typeof(MLBatchProcessor<,>));
        
        // Register document batch processor
        services.AddScoped<IDocumentBatchProcessor, DocumentBatchProcessor>();
        
        // Register sentiment batch processor
        services.AddScoped<ISentimentBatchProcessor, SentimentBatchProcessor>();
        
        // Register batch metrics collector
        services.AddScoped<IBatchMetricsCollector, BatchMetricsCollector>();
        
        // Add health checks
        services.AddHealthChecks()
            .AddCheck<BatchProcessorHealthCheck>("batch-processor");

        return services;
    }
}

public interface IBatchMetricsCollector
{
    void RecordBatchProcessing(string processorType, int itemCount, long durationMs, bool success);
    Task<BatchMetrics> GetMetricsAsync(string processorType, TimeSpan period);
}

public class BatchMetricsCollector : IBatchMetricsCollector
{
    private readonly ILogger<BatchMetricsCollector> logger;
    private readonly ConcurrentDictionary<string, List<BatchMetric>> metrics = new();

    public BatchMetricsCollector(ILogger<BatchMetricsCollector> logger)
    {
        logger = logger;
    }

    public void RecordBatchProcessing(string processorType, int itemCount, long durationMs, bool success)
    {
        var metric = new BatchMetric(
            Timestamp: DateTime.UtcNow,
            ItemCount: itemCount,
            DurationMs: durationMs,
            ThroughputPerSecond: itemCount / (durationMs / 1000.0),
            Success: success);

        metrics.AddOrUpdate(processorType, 
            new List<BatchMetric> { metric },
            (key, existing) =>
            {
                existing.Add(metric);
                // Keep only last 1000 metrics
                if (existing.Count > 1000)
                {
                    existing.RemoveAt(0);
                }
                return existing;
            });

        logger.LogDebug("Recorded batch metric for {ProcessorType}: {ItemCount} items in {Duration}ms", 
            processorType, itemCount, durationMs);
    }

    public async Task<BatchMetrics> GetMetricsAsync(string processorType, TimeSpan period)
    {
        if (!metrics.TryGetValue(processorType, out var metrics))
        {
            return new BatchMetrics(processorType, 0, 0, 0, 0, 100, DateTime.UtcNow);
        }

        var cutoff = DateTime.UtcNow - period;
        var recentMetrics = metrics.Where(m => m.Timestamp >= cutoff).ToList();

        if (recentMetrics.Count == 0)
        {
            return new BatchMetrics(processorType, 0, 0, 0, 0, 100, DateTime.UtcNow);
        }

        var totalItems = recentMetrics.Sum(m => m.ItemCount);
        var averageThroughput = recentMetrics.Average(m => m.ThroughputPerSecond);
        var averageDuration = recentMetrics.Average(m => m.DurationMs);
        var successRate = (double)recentMetrics.Count(m => m.Success) / recentMetrics.Count * 100;

        return await Task.FromResult(new BatchMetrics(
            ProcessorType: processorType,
            TotalItems: totalItems,
            AverageThroughputPerSecond: averageThroughput,
            AverageDurationMs: averageDuration,
            BatchCount: recentMetrics.Count,
            SuccessRate: successRate,
            PeriodEnd: DateTime.UtcNow));
    }
}

public record BatchMetric(
    DateTime Timestamp,
    int ItemCount,
    long DurationMs,
    double ThroughputPerSecond,
    bool Success);

public record BatchMetrics(
    string ProcessorType,
    int TotalItems,
    double AverageThroughputPerSecond,
    double AverageDurationMs,
    int BatchCount,
    double SuccessRate,
    DateTime PeriodEnd);

public class BatchProcessorHealthCheck : IHealthCheck
{
    private readonly IBatchMetricsCollector metricsCollector;
    private readonly ILogger<BatchProcessorHealthCheck> logger;

    public BatchProcessorHealthCheck(
        IBatchMetricsCollector metricsCollector,
        ILogger<BatchProcessorHealthCheck> logger)
    {
        metricsCollector = metricsCollector;
        logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check metrics for the last 5 minutes
            var metrics = await metricsCollector.GetMetricsAsync("document", TimeSpan.FromMinutes(5));
            
            if (metrics.SuccessRate < 90)
            {
                return HealthCheckResult.Degraded($"Batch processor success rate is {metrics.SuccessRate:F1}%");
            }

            if (metrics.AverageThroughputPerSecond < 10)
            {
                return HealthCheckResult.Degraded($"Batch processor throughput is low: {metrics.AverageThroughputPerSecond:F2} items/sec");
            }

            return HealthCheckResult.Healthy($"Batch processor is healthy. Success rate: {metrics.SuccessRate:F1}%, Throughput: {metrics.AverageThroughputPerSecond:F2} items/sec");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Health check failed");
            return HealthCheckResult.Unhealthy("Failed to check batch processor health", ex);
        }
    }
}
```

**Usage**:

### Basic Batch Processing

```csharp
// Register services
builder.Services.AddMLNetServices(configuration);
builder.Services.AddBatchProcessing(configuration);

// Use document batch processor
app.MapPost("/api/documents/batch", async (
    IDocumentBatchProcessor processor,
    List<BatchDocumentInput> documents,
    CancellationToken cancellationToken) =>
{
    var result = await processor.ProcessDocumentsAsync(documents, cancellationToken);
    
    return Results.Ok(new
    {
        ProcessedCount = result.OutputCount,
        ThroughputPerSecond = result.ThroughputPerSecond,
        ProcessingTimeMs = result.ProcessingTimeMs,
        Success = result.Success,
        Categories = result.Results.GroupBy(r => r.PredictedCategory)
            .ToDictionary(g => g.Key, g => g.Count())
    });
});

// Process file
app.MapPost("/api/documents/process-file", async (
    IDocumentBatchProcessor processor,
    string inputPath,
    string outputPath,
    CancellationToken cancellationToken) =>
{
    var report = await processor.ProcessDocumentFileAsync(inputPath, outputPath, cancellationToken);
    return Results.Ok(report);
});
```

### Streaming Processing

```csharp
// Process documents as they arrive
public async Task ProcessDocumentStreamAsync(IAsyncEnumerable<BatchDocumentInput> documentStream)
{
    var processor = serviceProvider.GetRequiredService<IDocumentBatchProcessor>();
    
    await foreach (var result in processor.ProcessDocumentStreamAsync(documentStream))
    {
        Console.WriteLine($"Processed: {result.Id} -> {result.PredictedCategory} ({result.Confidence:P2})");
        
        // Store result or send to next stage
        await StoreResultAsync(result);
    }
}
```

### Configuration

```json
{
  "ML": {
    "BatchProcessing": {
      "StreamBatchSize": 1000,
      "MaxRetries": 3,
      "RetryDelay": "00:00:01",
      "EnableMetrics": true,
      "EnableDetailedLogging": false
    },
    "DocumentBatch": {
      "FileBatchSize": 5000,
      "MaxConcurrency": 4,
      "OutputFormat": "csv",
      "IncludeMetrics": true
    }
  }
}
```

**Notes**:

- **Performance**: Optimized for high-throughput document processing with configurable batch sizes and parallelization
- **Scalability**: Supports streaming processing for large datasets that don't fit in memory
- **Monitoring**: Built-in metrics collection and health checks for production deployment
- **Error Handling**: Comprehensive error handling with detailed reporting and retry mechanisms
- **Flexibility**: Generic batch processor can be used with any ML.NET model and data types
- **Memory Management**: Efficient memory usage with streaming and chunked processing options

**Related Patterns**:

- [Real-time Processing](realtime-processing.md) - For immediate document processing needs
- [Orleans Integration](orleans-integration.md) - For distributed batch processing with Orleans
- [Text Classification](text-classification.md) - Core classification patterns used in batch processing
- [Model Deployment](model-deployment.md) - Production deployment considerations
