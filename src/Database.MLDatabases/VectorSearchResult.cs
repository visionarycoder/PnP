namespace Database.MLDatabases;

public class VectorSearchResult
{
    public string Id { get; set; } = string.Empty;
    public string Document { get; set; } = string.Empty;
    public double Distance { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}