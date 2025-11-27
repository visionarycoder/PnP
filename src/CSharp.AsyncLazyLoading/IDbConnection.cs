namespace CSharp.AsyncLazyLoading;

public interface IDbConnection
{
    string ConnectionString { get; }
    Task<bool> TestConnectionAsync();
}