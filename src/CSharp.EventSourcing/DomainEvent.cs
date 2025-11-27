using System;
using System.Collections.Generic;

namespace CSharp.EventSourcing;

/// <summary>
/// Base implementation for domain events
/// </summary>
public abstract class DomainEvent : IDomainEvent
{
    protected DomainEvent()
    {
        EventId = Guid.NewGuid();
        Timestamp = DateTime.UtcNow;
        EventType = GetType().Name;
        Metadata = new Dictionary<string, object>();
    }

    public Guid EventId { get; private set; }
    public DateTime Timestamp { get; private set; }
    public int Version { get; set; }
    public string EventType { get; private set; }
    public IDictionary<string, object> Metadata { get; private set; }
    public Guid AggregateId { get; set; }
    public string AggregateType { get; set; } = string.Empty;
}