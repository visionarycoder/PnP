namespace Database.MLDatabaseExamples;

public class ChromaDocument
{
    public string Id { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public float[] Embedding { get; set; } = Array.Empty<float>();
    public Dictionary<string, object> Metadata { get; set; } = new();
}