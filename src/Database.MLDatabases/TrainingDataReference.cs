namespace Database.MLDatabases;

public class TrainingDataReference
{
    public string Id { get; set; } = string.Empty;
    public string ExperimentId { get; set; } = string.Empty;
    public string DatasetName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public int RowCount { get; set; }
    public int FeatureCount { get; set; }
    public DateTime CreatedAt { get; set; }
}