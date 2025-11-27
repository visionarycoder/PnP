namespace CSharp.ProducerConsumer;

public class ProducerConsumerMetrics
{
    private long itemsProduced = 0;
    private long itemsConsumed = 0;
    private long totalProducingTime = 0;
    private long totalConsumingTime = 0;
    private long peakQueueSize = 0;
    private readonly object lockObject = new();
    private DateTime startTime = DateTime.UtcNow;

    public long ItemsProduced => itemsProduced;
    public long ItemsConsumed => itemsConsumed;
    public long ItemsInFlight => itemsProduced - itemsConsumed;
    public long PeakQueueSize => peakQueueSize;
    
    public double ProducingThroughput
    {
        get
        {
            var elapsed = DateTime.UtcNow - startTime;
            return elapsed.TotalSeconds > 0 ? itemsProduced / elapsed.TotalSeconds : 0.0;
        }
    }
    
    public double ConsumingThroughput
    {
        get
        {
            var elapsed = DateTime.UtcNow - startTime;
            return elapsed.TotalSeconds > 0 ? itemsConsumed / elapsed.TotalSeconds : 0.0;
        }
    }

    public TimeSpan AverageProducingTime => itemsProduced > 0 
        ? TimeSpan.FromTicks(totalProducingTime / itemsProduced) 
        : TimeSpan.Zero;

    public TimeSpan AverageConsumingTime => itemsConsumed > 0 
        ? TimeSpan.FromTicks(totalConsumingTime / itemsConsumed) 
        : TimeSpan.Zero;

    public void RecordItemProduced(TimeSpan producingTime, long queueSize = 0)
    {
        Interlocked.Increment(ref itemsProduced);
        Interlocked.Add(ref totalProducingTime, producingTime.Ticks);
        
        var currentPeak = peakQueueSize;
        while (queueSize > currentPeak && 
               Interlocked.CompareExchange(ref peakQueueSize, queueSize, currentPeak) != currentPeak)
        {
            currentPeak = peakQueueSize;
        }
    }

    public void RecordItemConsumed(TimeSpan consumingTime)
    {
        Interlocked.Increment(ref itemsConsumed);
        Interlocked.Add(ref totalConsumingTime, consumingTime.Ticks);
    }

    public void Reset()
    {
        lock (lockObject)
        {
            itemsProduced = 0;
            itemsConsumed = 0;
            totalProducingTime = 0;
            totalConsumingTime = 0;
            peakQueueSize = 0;
            startTime = DateTime.UtcNow;
        }
    }

    public ProducerConsumerStats GetStats()
    {
        return new ProducerConsumerStats
        {
            ItemsProduced = ItemsProduced,
            ItemsConsumed = ItemsConsumed,
            ItemsInFlight = ItemsInFlight,
            PeakQueueSize = PeakQueueSize,
            ProducingThroughput = ProducingThroughput,
            ConsumingThroughput = ConsumingThroughput,
            AverageProducingTime = AverageProducingTime,
            AverageConsumingTime = AverageConsumingTime,
            Efficiency = ProducingThroughput > 0 ? ConsumingThroughput / ProducingThroughput : 0.0
        };
    }
}