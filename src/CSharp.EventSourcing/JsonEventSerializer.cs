using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CSharp.EventSourcing;

/// <summary>
/// JSON-based event serializer implementation
/// </summary>
public class JsonEventSerializer : IEventSerializer
{
    private readonly JsonSerializerOptions options;
    private readonly Dictionary<string, Type> eventTypes;

    public JsonEventSerializer()
    {
        options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        eventTypes = new Dictionary<string, Type>();
        RegisterKnownEventTypes();
    }

    public string Serialize(object obj)
    {
        return JsonSerializer.Serialize(obj, options);
    }

    public IEvent Deserialize(string data, string eventType)
    {
        if (!eventTypes.TryGetValue(eventType, out var type))
        {
            throw new InvalidOperationException($"Unknown event type: {eventType}");
        }

        var result = JsonSerializer.Deserialize(data, type, options);
        return (IEvent)result!;
    }

    public T Deserialize<T>(string data)
    {
        var result = JsonSerializer.Deserialize<T>(data, options);
        return result!;
    }

    public void RegisterEventType<T>() where T : IEvent
    {
        eventTypes[typeof(T).Name] = typeof(T);
    }

    private void RegisterKnownEventTypes()
    {
        // Register common event types from the executing assembly
        var eventTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => typeof(IEvent).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface);

        foreach (var type in eventTypes)
        {
            this.eventTypes[type.Name] = type;
        }
    }
}