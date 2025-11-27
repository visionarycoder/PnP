using System.Text.Json;

namespace Database.MLDatabases;

public class ChromaVectorProvider
{
    private readonly Dictionary<string, List<ChromaDocument>> collections = new();
    
    public async Task<string> CreateCollectionAsync(string name, Dictionary<string, object>? metadata = null)
    {
        await Task.Delay(10);
        
        if (!collections.ContainsKey(name))
        {
            collections[name] = new List<ChromaDocument>();
        }
        
        Console.WriteLine($"   🔗 Created Chroma collection: {name}");
        return name; // Collection ID
    }
    
    public async Task AddEmbeddingsAsync(string collectionId, IEnumerable<DocumentEmbedding> embeddings)
    {
        await Task.Delay(10);
        
        if (!collections.ContainsKey(collectionId))
        {
            collections[collectionId] = new List<ChromaDocument>();
        }
        
        var documents = embeddings.Select(e => new ChromaDocument
        {
            Id = e.Id.ToString(),
            Content = e.DocumentId,
            Embedding = e.Vector,
            Metadata = e.Metadata != null ? JsonSerializer.Deserialize<Dictionary<string, object>>(e.Metadata) ?? new() : new()
        }).ToList();
        
        collections[collectionId].AddRange(documents);
        Console.WriteLine($"   📚 Added {documents.Count} embeddings to collection {collectionId}");
    }
    
    public async Task<VectorSearchResult[]> QuerySimilarAsync(
        string collectionId,
        float[] queryEmbedding,
        int topK = 10,
        Dictionary<string, object>? filter = null)
    {
        await Task.Delay(10);
        
        if (!collections.ContainsKey(collectionId))
        {
            return Array.Empty<VectorSearchResult>();
        }
        
        var documents = collections[collectionId];
        
        // Apply filter if provided
        if (filter != null)
        {
            documents = documents.Where(d => 
                filter.All(f => d.Metadata.ContainsKey(f.Key) && 
                                d.Metadata[f.Key].ToString() == f.Value.ToString())).ToList();
        }
        
        var results = documents
            .Select(d => new VectorSearchResult
            {
                Id = d.Id,
                Document = d.Content,
                Distance = 1.0 - CalculateCosineSimilarity(queryEmbedding, d.Embedding),
                Metadata = d.Metadata
            })
            .OrderBy(r => r.Distance)
            .Take(topK)
            .ToArray();
        
        Console.WriteLine($"   🔍 Found {results.Length} similar documents in {collectionId}");
        return results;
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