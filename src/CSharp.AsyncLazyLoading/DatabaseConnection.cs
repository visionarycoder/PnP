namespace CSharp.AsyncLazyLoading;

public class DatabaseConnection(string connectionString) : IDbConnection
{
    public string ConnectionString { get; } = connectionString;

    public async Task<bool> TestConnectionAsync()
    {
        await Task.Delay(100);
        return true;
    }
}