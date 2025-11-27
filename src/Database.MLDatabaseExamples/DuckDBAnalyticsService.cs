namespace Database.MLDatabaseExamples;

public class DuckDBAnalyticsService
{
    private readonly List<ExperimentPerformance> performances = new();
    private readonly List<ModelPrediction> predictions = new();
    
    public async Task RecordExperimentPerformanceAsync(ExperimentPerformance performance)
    {
        await Task.Delay(10);
        performances.Add(performance);
    }
    
    public async Task<List<ModelComparison>> CompareModelsAsync(string[] modelNames)
    {
        await Task.Delay(10);
        
        var comparisons = new List<ModelComparison>();
        
        foreach (var modelName in modelNames)
        {
            var modelPerformances = performances.Where(p => p.ModelName == modelName).ToList();
            
            if (modelPerformances.Any())
            {
                comparisons.Add(new ModelComparison
                {
                    ModelName = modelName,
                    TotalExperiments = modelPerformances.Count,
                    AverageAccuracy = modelPerformances.Average(p => p.Accuracy),
                    BestAccuracy = modelPerformances.Max(p => p.Accuracy),
                    WorstAccuracy = modelPerformances.Min(p => p.Accuracy),
                    AverageTrainingTime = modelPerformances.Average(p => p.TrainingTimeSeconds),
                    AverageInferenceTime = modelPerformances.Average(p => p.InferenceTimeMs),
                    AverageF1Score = modelPerformances.Average(p => p.F1Score)
                });
            }
        }
        
        return comparisons.OrderByDescending(c => c.AverageAccuracy).ToList();
    }
    
    public async Task StoreBatchPredictionsAsync(List<ModelPrediction> newPredictions)
    {
        await Task.Delay(10);
        predictions.AddRange(newPredictions);
    }
    
    public async Task<List<ModelPerformanceTrend>> GetPerformanceTrendsAsync(string modelName, int days = 30)
    {
        await Task.Delay(10);
        
        var startDate = DateTime.UtcNow.AddDays(-days);
        var modelPerformances = performances
            .Where(p => p.ModelName == modelName)
            .GroupBy(p => p.ExperimentId) // Group by experiment for trend analysis
            .Select((g, index) => new ModelPerformanceTrend
            {
                Date = startDate.AddDays(index), // Simulate dates for demo
                ModelName = modelName,
                AverageAccuracy = g.Average(p => p.Accuracy),
                AccuracyStdDev = CalculateStandardDeviation(g.Select(p => p.Accuracy)),
                AverageInferenceTime = g.Average(p => p.InferenceTimeMs),
                P95InferenceTime = g.Select(p => p.InferenceTimeMs).OrderByDescending(x => x).Take((int)Math.Ceiling(g.Count() * 0.05)).FirstOrDefault(),
                ExperimentCount = g.Count()
            })
            .ToList();
        
        return modelPerformances;
    }
    
    private static double CalculateStandardDeviation(IEnumerable<double> values)
    {
        var enumerable = values.ToList();
        var avg = enumerable.Average();
        var sum = enumerable.Sum(v => Math.Pow(v - avg, 2));
        return Math.Sqrt(sum / enumerable.Count);
    }
}