namespace CSharp.CacheInvalidation;

public class MockEventSubscriber : IEventSubscriber
{
    private Func<EventMessage, Task>? handler;

    public Task SubscribeAsync(Func<EventMessage, Task> handler, CancellationToken token = default)
    {
        this.handler = handler;
        return Task.CompletedTask;
    }

    public async Task PublishEventAsync(EventMessage eventMessage)
    {
        if (handler != null)
        {
            await handler(eventMessage);
        }
    }

    public Task UnsubscribeAsync(CancellationToken token = default)
    {
        handler = null;
        return Task.CompletedTask;
    }

    public void Dispose() { }
}