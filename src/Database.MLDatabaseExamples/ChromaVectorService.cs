namespace Database.MLDatabaseExamples;

public class ChromaVectorService
{
    private readonly Dictionary<string, List<ChromaDocument>> collections = new();
    
    public async Task<string> CreateCollectionAsync(
        string name, 
        Dictionary<string, object>? metadata = null)
    {
        await Task.Delay(10);
        
        if (!collections.ContainsKey(name))
        {
            collections[name] = new List<ChromaDocument>();
        }
        
        return name;
    }
    
    public async Task AddDocumentsAsync(
        string collectionName,
        List<ChromaDocument> documents)
    {
        await Task.Delay(10);
        
        if (!collections.ContainsKey(collectionName))
        {
            collections[collectionName] = new List<ChromaDocument>();
        }
        
        collections[collectionName].AddRange(documents);
    }
    
    public async Task<List<ChromaQueryResult>> QuerySimilarAsync(
        string collectionName,
        float[] queryEmbedding,
        int nResults = 10,
        Dictionary<string, object>? filter = null)
    {
        await Task.Delay(10);
        
        if (!collections.ContainsKey(collectionName))
        {
            return new List<ChromaQueryResult>();
        }
        
        var documents = collections[collectionName];
        
        // Apply filter if provided
        if (filter != null)
        {
            documents = documents.Where(d => 
                filter.All(f => d.Metadata.ContainsKey(f.Key) && 
                                d.Metadata[f.Key].Equals(f.Value))).ToList();
        }
        
        var results = documents
            .Select(d => new ChromaQueryResult
            {
                Id = d.Id,
                Document = d.Content,
                Distance = 1.0 - CalculateCosineSimilarity(queryEmbedding, d.Embedding),
                Metadata = d.Metadata
            })
            .OrderBy(r => r.Distance)
            .Take(nResults)
            .ToList();
        
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