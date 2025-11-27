namespace Database.MLDatabases;

public class DuckDBMLProvider
{
    private readonly List<ModelPerformanceMetric> metrics = new();
    private readonly List<MLPrediction> predictions = new();
    
    public async Task<MLAnalyticsResult> AnalyzeModelPerformanceAsync(string experimentId)
    {
        await Task.Delay(20);
        
        var experimentMetrics = metrics.Where(m => m.ExperimentId == experimentId).ToList();
        
        var performanceAnalysis = experimentMetrics
            .GroupBy(m => m.ModelName)
            .Select(g => new ModelPerformanceAnalysis
            {
                ModelName = g.Key,
                AverageAccuracy = g.Average(m => m.Accuracy),
                AccuracyStandardDeviation = CalculateStandardDeviation(g.Select(m => m.Accuracy)),
                TotalRuns = g.Count(),
                P95InferenceTime = g.Select(m => m.InferenceTimeMs).OrderByDescending(t => t).Take((int)Math.Ceiling(g.Count() * 0.05)).FirstOrDefault(),
                AverageMemoryUsage = g.Average(m => m.MemoryUsageMb)
            })
            .ToList();
        
        Console.WriteLine($"   📊 Analyzed performance for {performanceAnalysis.Count} models in experiment {experimentId}");
        return new MLAnalyticsResult(performanceAnalysis);
    }
    
    public async Task StoreBatchPredictionsAsync(IEnumerable<MLPrediction> newPredictions)
    {
        await Task.Delay(15);
        
        var predictionsList = newPredictions.ToList();
        predictions.AddRange(predictionsList);
        Console.WriteLine($"   💾 Stored {predictionsList.Count} batch predictions");
    }
    
    public async Task RecordModelPerformanceAsync(ModelPerformanceMetric metric)
    {
        await Task.Delay(5);
        metrics.Add(metric);
        Console.WriteLine($"   📈 Recorded performance for {metric.ModelName}: Accuracy={metric.Accuracy:F3}, Inference={metric.InferenceTimeMs:F1}ms");
    }
    
    public async Task<ModelPerformanceTrend[]> GetPerformanceTrendAsync(string modelName, TimeSpan timeWindow)
    {
        await Task.Delay(15);
        
        var startTime = DateTime.UtcNow.Subtract(timeWindow);
        var modelMetrics = metrics
            .Where(m => m.ModelName == modelName && m.Timestamp >= startTime)
            .GroupBy(m => m.Timestamp.Date) // Group by day for trend analysis
            .Select(g => new ModelPerformanceTrend
            {
                Hour = g.Key,
                ModelName = modelName,
                AverageAccuracy = g.Average(m => m.Accuracy),
                AverageInferenceTime = g.Average(m => m.InferenceTimeMs),
                RequestCount = g.Count(),
                P95InferenceTime = g.Select(m => m.InferenceTimeMs).OrderByDescending(t => t).Take((int)Math.Ceiling(g.Count() * 0.05)).FirstOrDefault(),
                P99InferenceTime = g.Select(m => m.InferenceTimeMs).OrderByDescending(t => t).Take((int)Math.Ceiling(g.Count() * 0.01)).FirstOrDefault()
            })
            .ToArray();
        
        Console.WriteLine($"   📈 Generated {modelMetrics.Length} trend points for {modelName}");
        return modelMetrics;
    }
    
    private static double CalculateStandardDeviation(IEnumerable<double> values)
    {
        var enumerable = values.ToList();
        if (!enumerable.Any()) return 0;
        
        var avg = enumerable.Average();
        var sum = enumerable.Sum(v => Math.Pow(v - avg, 2));
        return Math.Sqrt(sum / enumerable.Count);
    }
}