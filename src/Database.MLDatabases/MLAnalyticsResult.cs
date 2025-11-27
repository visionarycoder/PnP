namespace Database.MLDatabases;

public class MLAnalyticsResult
{
    public List<ModelPerformanceAnalysis> PerformanceAnalysis { get; }
    
    public MLAnalyticsResult(List<ModelPerformanceAnalysis> performanceAnalysis)
    {
        PerformanceAnalysis = performanceAnalysis;
    }
}