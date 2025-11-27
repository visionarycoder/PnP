using System.Text.Json;

namespace Database.MLDatabases;

public class PostgresMLProvider
{
    private readonly List<MLExperiment> experiments = new();
    private readonly List<DocumentEmbedding> embeddings = new();
    
    public async Task<string> CreateExperimentAsync(MLExperimentRequest request)
    {
        await Task.Delay(10); // Simulate async operation
        
        var experiment = new MLExperiment
        {
            Id = Guid.NewGuid().ToString(),
            Name = request.Name,
            ModelType = request.ModelType,
            Parameters = JsonDocument.Parse(JsonSerializer.Serialize(request.Parameters)),
            Status = ExperimentStatus.Created,
            CreatedAt = DateTime.UtcNow
        };
        
        experiments.Add(experiment);
        Console.WriteLine($"   📝 Created experiment: {experiment.Name} (ID: {experiment.Id})");
        return experiment.Id;
    }
    
    public async Task UpdateExperimentStatusAsync(string experimentId, ExperimentStatus status, Dictionary<string, object>? metrics = null)
    {
        await Task.Delay(10);
        
        var experiment = experiments.FirstOrDefault(e => e.Id == experimentId);
        if (experiment != null)
        {
            experiment.Status = status;
            if (status == ExperimentStatus.Completed)
            {
                experiment.CompletedAt = DateTime.UtcNow;
            }
            if (metrics != null)
            {
                experiment.Metrics = JsonDocument.Parse(JsonSerializer.Serialize(metrics));
            }
            Console.WriteLine($"   ✅ Updated experiment {experimentId} status to {status}");
        }
    }
    
    public async Task<List<MLExperiment>> GetExperimentsAsync(string? modelType = null)
    {
        await Task.Delay(5);
        
        var result = modelType == null 
            ? experiments.ToList() 
            : experiments.Where(e => e.ModelType == modelType).ToList();
        return result;
    }
    
    public async Task AddDocumentEmbeddingAsync(DocumentEmbedding embedding)
    {
        await Task.Delay(5);
        embeddings.Add(embedding);
        Console.WriteLine($"   📄 Added document embedding: {embedding.DocumentId} ({embedding.ModelName})");
    }
    
    public async Task<List<DocumentEmbedding>> FindSimilarDocumentsAsync(float[] queryEmbedding, string modelName, int limit = 10)
    {
        await Task.Delay(10);
        
        return embeddings
            .Where(e => e.ModelName == modelName)
            .OrderBy(e => CalculateCosineSimilarity(queryEmbedding, e.Vector))
            .Take(limit)
            .ToList();
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