namespace Database.MLDatabases;

public class MLExperimentRequest
{
    public string Name { get; set; } = string.Empty;
    public string ModelType { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
}