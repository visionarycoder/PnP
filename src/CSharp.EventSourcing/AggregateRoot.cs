using System.Reflection;

namespace CSharp.EventSourcing;

/// <summary>
/// Base abstract class for aggregate roots with event sourcing capabilities
/// </summary>
public abstract class AggregateRoot : IAggregateRoot
{
    private readonly List<IEvent> uncommittedEvents = new();
    private readonly Dictionary<Type, Action<IEvent>> eventHandlers = new();

    protected AggregateRoot()
    {
        Id = Guid.NewGuid();
        RegisterEventHandlers();
    }

    protected AggregateRoot(Guid id)
    {
        Id = id;
        RegisterEventHandlers();
    }

    public Guid Id { get; protected set; }
    public int Version { get; protected set; }
    public IEnumerable<IEvent> UncommittedEvents => uncommittedEvents.AsReadOnly();

    public void MarkEventsAsCommitted()
    {
        uncommittedEvents.Clear();
    }

    public void LoadFromHistory(IEnumerable<IEvent> events)
    {
        foreach (var domainEvent in events.OrderBy(e => e.Version))
        {
            ApplyEvent(domainEvent, isNew: false);
            Version = domainEvent.Version;
        }
    }

    public void SetId(Guid id)
    {
        Id = id;
    }

    /// <summary>
    /// Raise a new event and apply it to the aggregate
    /// </summary>
    protected void RaiseEvent(IEvent domainEvent)
    {
        if (domainEvent is IDomainEvent de)
        {
            de.AggregateId = Id;
            de.AggregateType = GetType().Name;
        }

        domainEvent.Version = Version + 1;
        ApplyEvent(domainEvent, isNew: true);
        
        if (domainEvent is IDomainEvent)
        {
            uncommittedEvents.Add(domainEvent);
        }
        
        Version = domainEvent.Version;
    }

    /// <summary>
    /// Apply an event to the aggregate state
    /// </summary>
    private void ApplyEvent(IEvent domainEvent, bool isNew)
    {
        var eventType = domainEvent.GetType();
        if (eventHandlers.TryGetValue(eventType, out var handler))
        {
            handler(domainEvent);
        }
        else
        {
            // Try to find handler method by convention (Apply + EventName)
            var methodName = $"Apply{eventType.Name}";
            var method = GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (method != null)
            {
                method.Invoke(this, new object[] { domainEvent });
            }
        }
    }

    /// <summary>
    /// Register an event handler for a specific event type
    /// </summary>
    protected void RegisterEventHandler<TEvent>(Action<TEvent> handler) where TEvent : IEvent
    {
        eventHandlers[typeof(TEvent)] = evt => handler((TEvent)evt);
    }

    /// <summary>
    /// Override to register event handlers in derived classes
    /// </summary>
    protected virtual void RegisterEventHandlers()
    {
        // Override in derived classes to register event handlers
    }

    /// <summary>
    /// Create a snapshot of the current aggregate state
    /// </summary>
    internal virtual ISnapshot CreateSnapshot()
    {
        return new AggregateSnapshot
        {
            AggregateId = Id,
            Version = Version,
            CreatedAt = DateTime.UtcNow,
            Data = System.Text.Json.JsonSerializer.Serialize(this),
            AggregateType = GetType().Name
        };
    }

    /// <summary>
    /// Restore aggregate state from a snapshot
    /// </summary>
    internal virtual void RestoreFromSnapshot(ISnapshot snapshot)
    {
        Version = snapshot.Version;
        // Override in derived classes to restore state
    }
}