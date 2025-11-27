namespace Database.MLDatabaseExamples;

public class ChromaQueryResult
{
    public string Id { get; set; } = string.Empty;
    public string Document { get; set; } = string.Empty;
    public double Distance { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}