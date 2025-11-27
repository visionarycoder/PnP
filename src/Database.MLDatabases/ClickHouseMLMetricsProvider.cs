namespace Database.MLDatabases;

public class ClickHouseMLMetricsProvider
{
    private readonly List<ModelPerformanceMetric> timeSeriesData = new();
    
    public async Task RecordModelPerformanceAsync(ModelPerformanceMetric metric)
    {
        await Task.Delay(5);
        timeSeriesData.Add(metric);
        Console.WriteLine($"   ⏰ Recorded time-series metric: {metric.ModelName} at {metric.Timestamp:HH:mm:ss}");
    }
    
    public async Task<ModelPerformanceTrend[]> GetPerformanceTrendAsync(string modelName, TimeSpan timeWindow)
    {
        await Task.Delay(10);
        
        var startTime = DateTime.UtcNow.Subtract(timeWindow);
        var trends = timeSeriesData
            .Where(m => m.ModelName == modelName && m.Timestamp >= startTime)
            .GroupBy(m => new DateTime(m.Timestamp.Year, m.Timestamp.Month, m.Timestamp.Day, m.Timestamp.Hour, 0, 0)) // Group by hour
            .Select(g => new ModelPerformanceTrend
            {
                Hour = g.Key,
                ModelName = modelName,
                AverageAccuracy = g.Average(m => m.Accuracy),
                AverageInferenceTime = g.Average(m => m.InferenceTimeMs),
                RequestCount = g.Count(),
                P95InferenceTime = g.Select(m => m.InferenceTimeMs).OrderByDescending(x => x).Take(Math.Max(1, (int)(g.Count() * 0.05))).FirstOrDefault(),
                P99InferenceTime = g.Select(m => m.InferenceTimeMs).OrderByDescending(x => x).Take(Math.Max(1, (int)(g.Count() * 0.01))).FirstOrDefault()
            })
            .OrderBy(t => t.Hour)
            .ToArray();
        
        Console.WriteLine($"   📊 Generated {trends.Length} hourly performance trends for {modelName}");
        return trends;
    }
}