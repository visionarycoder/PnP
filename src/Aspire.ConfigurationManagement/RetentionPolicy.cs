namespace Aspire.ConfigurationManagement;

public class RetentionPolicy
{
    public int DocumentRetentionDays { get; set; } = 30;
    public int ProcessingLogRetentionDays { get; set; } = 7;
    public bool EnableAutoCleanup { get; set; } = true;
}