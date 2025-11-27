using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

using Microsoft.Extensions.Logging;

namespace CSharp.ProducerConsumer;

public class BatchProcessor<TInput, TOutput> : IPipeline<TInput, TOutput>, IDisposable
{
    private readonly Func<IReadOnlyList<TInput>, CancellationToken, Task<IEnumerable<TOutput>>> batchProcessor;
    private readonly Channel<TInput> inputChannel;
    private readonly Channel<TOutput> outputChannel;
    private readonly int batchSize;
    private readonly TimeSpan batchTimeout;
    private readonly ILogger? logger;
    private readonly CancellationTokenSource cancellationTokenSource;
    private readonly Task processingTask;
    private volatile bool isDisposed = false;

    public BatchProcessor(
        Func<IReadOnlyList<TInput>, CancellationToken, Task<IEnumerable<TOutput>>> batchProcessor,
        int batchSize = 100,
        TimeSpan? batchTimeout = null,
        int inputCapacity = 1000,
        int outputCapacity = 1000,
        ILogger? logger = null)
    {
        this.batchProcessor = batchProcessor ?? throw new ArgumentNullException(nameof(batchProcessor));
        this.batchSize = batchSize;
        this.batchTimeout = batchTimeout ?? TimeSpan.FromSeconds(1);
        this.logger = logger;

        // Create bounded channels
        var inputOptions = new BoundedChannelOptions(inputCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        };

        var outputOptions = new BoundedChannelOptions(outputCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = true
        };

        inputChannel = Channel.CreateBounded<TInput>(inputOptions);
        outputChannel = Channel.CreateBounded<TOutput>(outputOptions);
        cancellationTokenSource = new CancellationTokenSource();

        // Start background processing task
        processingTask = ProcessBatchesAsync(cancellationTokenSource.Token);
    }

    public async Task<TOutput> ProcessAsync(TInput input, CancellationToken cancellationToken = default)
    {
        if (isDisposed) throw new ObjectDisposedException(nameof(BatchProcessor<TInput, TOutput>));

        await inputChannel.Writer.WriteAsync(input, cancellationToken);
        return await outputChannel.Reader.ReadAsync(cancellationToken);
    }

    public async Task<IEnumerable<TOutput>> ProcessBatchAsync(IEnumerable<TInput> inputs, CancellationToken cancellationToken = default)
    {
        if (isDisposed) throw new ObjectDisposedException(nameof(BatchProcessor<TInput, TOutput>));

        var inputList = inputs.ToList();
        var outputs = new List<TOutput>();

        foreach (var input in inputList)
        {
            await inputChannel.Writer.WriteAsync(input, cancellationToken);
        }

        for (int i = 0; i < inputList.Count; i++)
        {
            var output = await outputChannel.Reader.ReadAsync(cancellationToken);
            outputs.Add(output);
        }

        return outputs;
    }

    public async IAsyncEnumerable<TOutput> ProcessStreamAsync(
        IAsyncEnumerable<TInput> inputs, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (isDisposed) throw new ObjectDisposedException(nameof(BatchProcessor<TInput, TOutput>));

        var inputTask = Task.Run(async () =>
        {
            await foreach (var input in inputs.WithCancellation(cancellationToken))
            {
                await inputChannel.Writer.WriteAsync(input, cancellationToken);
            }
        }, cancellationToken);

        await foreach (var output in outputChannel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return output;
        }

        await inputTask;
    }

    private async Task ProcessBatchesAsync(CancellationToken cancellationToken)
    {
        try
        {
            var batch = new List<TInput>();
            var lastBatchTime = DateTime.UtcNow;

            await foreach (var input in inputChannel.Reader.ReadAllAsync(cancellationToken))
            {
                batch.Add(input);

                var shouldProcessBatch = batch.Count >= batchSize ||
                                         DateTime.UtcNow - lastBatchTime >= batchTimeout;

                if (shouldProcessBatch && batch.Count > 0)
                {
                    await ProcessBatch(batch, cancellationToken);
                    batch.Clear();
                    lastBatchTime = DateTime.UtcNow;
                }
            }

            if (batch.Count > 0)
            {
                await ProcessBatch(batch, cancellationToken);
            }

            outputChannel.Writer.Complete();
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error in batch processing loop");
            outputChannel.Writer.Complete(ex);
        }
    }

    private async Task ProcessBatch(List<TInput> batch, CancellationToken cancellationToken)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            var outputs = await batchProcessor(batch.AsReadOnly(), cancellationToken);
            stopwatch.Stop();
            
            logger?.LogDebug("Processed batch of {BatchSize} items in {ElapsedMs}ms", 
                batch.Count, stopwatch.ElapsedMilliseconds);

            foreach (var output in outputs)
            {
                await outputChannel.Writer.WriteAsync(output, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error processing batch of {BatchSize} items", batch.Count);
            throw;
        }
    }

    public void Dispose()
    {
        if (!isDisposed)
        {
            isDisposed = true;
            
            inputChannel.Writer.Complete();
            cancellationTokenSource.Cancel();
            
            try
            {
                processingTask.Wait(TimeSpan.FromSeconds(10));
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Error waiting for processing task to complete");
            }
            
            cancellationTokenSource?.Dispose();
        }
    }
}