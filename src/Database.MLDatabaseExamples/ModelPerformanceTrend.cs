namespace Database.MLDatabaseExamples;

public class ModelPerformanceTrend
{
    public DateTime Date { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public double AverageAccuracy { get; set; }
    public double AccuracyStdDev { get; set; }
    public double AverageInferenceTime { get; set; }
    public double P95InferenceTime { get; set; }
    public int ExperimentCount { get; set; }
}