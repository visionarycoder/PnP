using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Database.MLDatabaseExamples;

[Table("document_embeddings", Schema = "ml_schema")]
public class DocumentEmbedding
{
    public Guid Id { get; set; }
    
    [Column("document_id")]
    public string DocumentId { get; set; } = string.Empty;
    
    [Column("content_hash")]
    public string ContentHash { get; set; } = string.Empty;
    
    public float[] Embedding { get; set; } = Array.Empty<float>();
    
    [Column("modelName")]
    public string ModelName { get; set; } = string.Empty;
    
    [Column("chunk_index")]
    public int ChunkIndex { get; set; }
    
    public JsonDocument? Metadata { get; set; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}