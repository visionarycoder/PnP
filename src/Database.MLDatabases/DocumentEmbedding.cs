using System.Text.Json;

namespace Database.MLDatabases;

public class DocumentEmbedding
{
    public Guid Id { get; set; }
    public string DocumentId { get; set; } = string.Empty;
    public float[] Vector { get; set; } = Array.Empty<float>();
    public string ModelName { get; set; } = string.Empty;
    public JsonDocument? Metadata { get; set; }
    public DateTime CreatedAt { get; set; }
}