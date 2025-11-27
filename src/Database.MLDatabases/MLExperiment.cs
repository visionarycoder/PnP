using System.Text.Json;

namespace Database.MLDatabases;

public class MLExperiment
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ModelType { get; set; } = string.Empty;
    public JsonDocument? Parameters { get; set; }
    public JsonDocument? Metrics { get; set; }
    public ExperimentStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}