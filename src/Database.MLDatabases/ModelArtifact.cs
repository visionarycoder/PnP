using System.Text.Json;

namespace Database.MLDatabases;

public class ModelArtifact
{
    public string Id { get; set; } = string.Empty;
    public string ExperimentId { get; set; } = string.Empty;
    public string ArtifactType { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public JsonDocument? Metadata { get; set; }
    public DateTime CreatedAt { get; set; }
}