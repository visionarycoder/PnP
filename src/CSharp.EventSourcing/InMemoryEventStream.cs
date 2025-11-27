using Microsoft.Extensions.Logging;

namespace CSharp.EventSourcing;

/// <summary>
/// In-memory implementation of event stream
/// </summary>
public class InMemoryEventStream : IEventStream
{
    private readonly Guid streamId;
    private readonly InMemoryEventStore eventStore;
    private readonly IEventSerializer eventSerializer;
    private readonly ILogger? logger;
    private int currentPosition = 0;

    public InMemoryEventStream(
        Guid streamId,
        InMemoryEventStore eventStore,
        IEventSerializer eventSerializer,
        ILogger? logger)
    {
        this.streamId = streamId;
        this.eventStore = eventStore;
        this.eventSerializer = eventSerializer;
        this.logger = logger;
    }

    public Guid StreamId => streamId;
    public int CurrentVersion => currentPosition;

    public async Task<bool> HasMoreEventsAsync(CancellationToken token = default)
    {
        var currentVersion = await eventStore.GetCurrentVersionAsync(streamId, token).ConfigureAwait(false);
        return currentPosition < currentVersion;
    }

    public async IAsyncEnumerator<IEvent> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        while (await HasMoreEventsAsync(cancellationToken).ConfigureAwait(false))
        {
            var events = await eventStore.GetEventsAsync(streamId, currentPosition, cancellationToken)
                .ConfigureAwait(false);
            
            foreach (var domainEvent in events)
            {
                currentPosition++;
                yield return domainEvent;
            }
        }
    }
}