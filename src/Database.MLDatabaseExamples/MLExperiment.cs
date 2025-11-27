using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Database.MLDatabaseExamples;

[Table("experiments", Schema = "ml_schema")]
public class MLExperiment
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    [Column("model_type")]
    public string ModelType { get; set; } = string.Empty;
    
    public JsonDocument? Parameters { get; set; }
    public JsonDocument? Metrics { get; set; }
    public string Status { get; set; } = "created";
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
    
    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }
}