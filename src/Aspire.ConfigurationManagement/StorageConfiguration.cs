namespace Aspire.ConfigurationManagement;

public class StorageConfiguration
{
    public string ConnectionString { get; set; } = string.Empty;
    public string ContainerName { get; set; } = "documents";
    public bool UseLocalStorage { get; set; } = false;
    public string LocalStoragePath { get; set; } = "./storage";
    public RetentionPolicy Retention { get; set; } = new();
}