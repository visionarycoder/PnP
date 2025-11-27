namespace Aspire.ConfigurationManagement;

public class DocumentProcessingOptions
{
    public const string SectionName = "DocumentProcessing";
    
    public string DefaultLanguage { get; set; } = "en";
    public int MaxDocumentSize { get; set; } = 10 * 1024 * 1024; // 10MB
    public int ProcessingTimeout { get; set; } = 300; // 5 minutes
    public bool EnableParallelProcessing { get; set; } = true;
    public int MaxConcurrentDocuments { get; set; } = Environment.ProcessorCount * 2;
    public StorageConfiguration Storage { get; set; } = new();
    public MLConfiguration ML { get; set; } = new();
}