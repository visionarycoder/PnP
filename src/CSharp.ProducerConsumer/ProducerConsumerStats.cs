namespace CSharp.ProducerConsumer;

public class ProducerConsumerStats
{
    public long ItemsProduced { get; set; }
    public long ItemsConsumed { get; set; }
    public long ItemsInFlight { get; set; }
    public long PeakQueueSize { get; set; }
    public double ProducingThroughput { get; set; }
    public double ConsumingThroughput { get; set; }
    public TimeSpan AverageProducingTime { get; set; }
    public TimeSpan AverageConsumingTime { get; set; }
    public double Efficiency { get; set; }
}