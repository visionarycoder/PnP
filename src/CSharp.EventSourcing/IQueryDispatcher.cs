namespace CSharp.EventSourcing;

/// <summary>
/// Dispatcher interface for queries
/// </summary>
public interface IQueryDispatcher
{
    /// <summary>
    /// Dispatch a query to its appropriate handler and return the result
    /// </summary>
    Task<TResult> DispatchAsync<TQuery, TResult>(TQuery query, CancellationToken token = default)
        where TQuery : IQuery<TResult>;
}