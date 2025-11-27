using System.Text.Json;

namespace Database.MLDatabases;

public class SQLiteMLProvider
{
    private readonly List<MLExperiment> experiments = new();
    private readonly List<ModelArtifact> artifacts = new();
    private readonly List<TrainingDataReference> trainingData = new();
    
    public async Task<string> CreateExperimentAsync(MLExperimentRequest request)
    {
        await Task.Delay(5);
        
        var experimentId = Guid.NewGuid().ToString();
        var experiment = new MLExperiment
        {
            Id = experimentId,
            Name = request.Name,
            ModelType = request.ModelType,
            Parameters = JsonDocument.Parse(JsonSerializer.Serialize(request.Parameters)),
            Status = ExperimentStatus.Created,
            CreatedAt = DateTime.UtcNow
        };
        
        experiments.Add(experiment);
        Console.WriteLine($"   🗄️ SQLite: Created experiment {experiment.Name} (ID: {experimentId})");
        return experimentId;
    }
    
    public async Task AddModelArtifactAsync(string experimentId, string artifactType, string filePath, Dictionary<string, object>? metadata = null)
    {
        await Task.Delay(5);
        
        var artifact = new ModelArtifact
        {
            Id = Guid.NewGuid().ToString(),
            ExperimentId = experimentId,
            ArtifactType = artifactType,
            FilePath = filePath,
            Metadata = metadata != null ? JsonDocument.Parse(JsonSerializer.Serialize(metadata)) : null,
            CreatedAt = DateTime.UtcNow
        };
        
        artifacts.Add(artifact);
        Console.WriteLine($"   📦 Added model artifact: {artifactType} at {filePath}");
    }
    
    public async Task AddTrainingDataReferenceAsync(string experimentId, string datasetName, string filePath, int rowCount, int featureCount)
    {
        await Task.Delay(5);
        
        var reference = new TrainingDataReference
        {
            Id = Guid.NewGuid().ToString(),
            ExperimentId = experimentId,
            DatasetName = datasetName,
            FilePath = filePath,
            RowCount = rowCount,
            FeatureCount = featureCount,
            CreatedAt = DateTime.UtcNow
        };
        
        trainingData.Add(reference);
        Console.WriteLine($"   📊 Added training data reference: {datasetName} ({rowCount} rows, {featureCount} features)");
    }
    
    public async Task<List<ModelArtifact>> GetExperimentArtifactsAsync(string experimentId)
    {
        await Task.Delay(5);
        return artifacts.Where(a => a.ExperimentId == experimentId).ToList();
    }
}