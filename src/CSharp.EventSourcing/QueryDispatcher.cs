using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CSharp.EventSourcing;

/// <summary>
/// Query dispatcher implementation using dependency injection
/// </summary>
public class QueryDispatcher : IQueryDispatcher
{
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger? logger;

    public QueryDispatcher(IServiceProvider serviceProvider, ILogger<QueryDispatcher>? logger = null)
    {
        this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        this.logger = logger;
    }

    public async Task<TResult> DispatchAsync<TQuery, TResult>(TQuery query, CancellationToken token = default)
        where TQuery : IQuery<TResult>
    {
        var handler = serviceProvider.GetRequiredService<IQueryHandler<TQuery, TResult>>();
        
        logger?.LogTrace("Dispatching query {QueryType} with ID {QueryId}",
            typeof(TQuery).Name, query.QueryId);

        try
        {
            var result = await handler.HandleAsync(query, token).ConfigureAwait(false);
            logger?.LogTrace("Successfully handled query {QueryId}", query.QueryId);
            return result;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to handle query {QueryId}", query.QueryId);
            throw;
        }
    }
}