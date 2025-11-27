namespace Database.MLDatabaseExamples;

public class ExperimentPerformance
{
    public Guid ExperimentId { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public string DatasetName { get; set; } = string.Empty;
    public double Accuracy { get; set; }
    public double Precision { get; set; }
    public double Recall { get; set; }
    public double F1Score { get; set; }
    public int TrainingTimeSeconds { get; set; }
    public double InferenceTimeMs { get; set; }
    public double MemoryUsageMb { get; set; }
}