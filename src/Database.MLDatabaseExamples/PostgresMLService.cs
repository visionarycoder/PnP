using System.Text.Json;

namespace Database.MLDatabaseExamples;

public class PostgresMLService
{
    private readonly List<MLExperiment> experiments = new();
    private readonly List<DocumentEmbedding> embeddings = new();
    
    public async Task<Guid> CreateExperimentAsync(
        string name, 
        string modelType, 
        Dictionary<string, object> parameters,
        string? description = null)
    {
        await Task.Delay(10); // Simulate async operation
        
        var experiment = new MLExperiment
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            ModelType = modelType,
            Parameters = JsonDocument.Parse(JsonSerializer.Serialize(parameters)),
            Status = "created",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        experiments.Add(experiment);
        return experiment.Id;
    }
    
    public async Task<List<DocumentEmbedding>> FindSimilarDocumentsAsync(
        float[] queryEmbedding, 
        string modelName, 
        int limit = 10)
    {
        await Task.Delay(10);
        
        return embeddings
            .Where(e => e.ModelName == modelName)
            .OrderBy(e => CalculateCosineSimilarity(queryEmbedding, e.Embedding))
            .Take(limit)
            .ToList();
    }
    
    public async Task UpdateExperimentStatusAsync(Guid experimentId, string status, Dictionary<string, object>? metrics = null)
    {
        await Task.Delay(10);
        
        var experiment = experiments.FirstOrDefault(e => e.Id == experimentId);
        if (experiment != null)
        {
            experiment.Status = status;
            experiment.UpdatedAt = DateTime.UtcNow;
            if (status == "completed")
            {
                experiment.CompletedAt = DateTime.UtcNow;
            }
            if (metrics != null)
            {
                experiment.Metrics = JsonDocument.Parse(JsonSerializer.Serialize(metrics));
            }
        }
    }
    
    public Task<List<MLExperiment>> GetExperimentsAsync(string? modelType = null)
    {
        var result = modelType == null 
            ? experiments.ToList() 
            : experiments.Where(e => e.ModelType == modelType).ToList();
        return Task.FromResult(result);
    }
    
    private static double CalculateCosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length) return 0;
        
        double dotProduct = 0;
        double normA = 0;
        double normB = 0;
        
        for (int i = 0; i < a.Length; i++)
        {
            dotProduct += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }
        
        return dotProduct / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }
}