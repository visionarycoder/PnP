namespace Database.MLDatabaseExamples;

public class ModelComparison
{
    public string ModelName { get; set; } = string.Empty;
    public int TotalExperiments { get; set; }
    public double AverageAccuracy { get; set; }
    public double BestAccuracy { get; set; }
    public double WorstAccuracy { get; set; }
    public double AverageTrainingTime { get; set; }
    public double AverageInferenceTime { get; set; }
    public double AverageF1Score { get; set; }
}