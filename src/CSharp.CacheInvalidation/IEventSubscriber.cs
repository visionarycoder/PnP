namespace CSharp.CacheInvalidation;

public interface IEventSubscriber : IDisposable
{
    Task SubscribeAsync(Func<EventMessage, Task> handler, CancellationToken token = default);
    Task UnsubscribeAsync(CancellationToken token = default);
}