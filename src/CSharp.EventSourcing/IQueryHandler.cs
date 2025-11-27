namespace CSharp.EventSourcing;

/// <summary>
/// Handler interface for processing queries
/// </summary>
public interface IQueryHandler<in TQuery, TResult> where TQuery : IQuery<TResult>
{
    /// <summary>
    /// Handle the specified query and return the result
    /// </summary>
    Task<TResult> HandleAsync(TQuery query, CancellationToken token = default);
}