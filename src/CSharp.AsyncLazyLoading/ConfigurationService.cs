namespace CSharp.AsyncLazyLoading;

public class ConfigurationService(string configSource)
{
    private readonly string configSource = configSource;
    private readonly AsyncLazyWithExpiration<AppConfig> configLazy = new(
        () => LoadConfigurationAsync(configSource),
        TimeSpan.FromMinutes(5)); // Refresh config every 5 minutes

    public Task<AppConfig> GetConfigurationAsync() => configLazy.GetValueAsync();

    private static async Task<AppConfig> LoadConfigurationAsync(string source)
    {
        Console.WriteLine($"Loading configuration from {source}...");
        
        // Simulate expensive config loading
        await Task.Delay(500);
        
        return new AppConfig
        {
            DatabaseConnectionString = "Server=localhost;Database=MyApp",
            ApiKey = "secret-api-key",
            MaxConcurrentUsers = 1000,
            EnableFeatureX = true
        };
    }
}