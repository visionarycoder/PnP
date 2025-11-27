namespace CSharp.EventSourcing;

/// <summary>
/// Interface for event serialization
/// </summary>
public interface IEventSerializer
{
    /// <summary>
    /// Serialize an object to string
    /// </summary>
    string Serialize(object obj);
    
    /// <summary>
    /// Deserialize an event from string data
    /// </summary>
    IEvent Deserialize(string data, string eventType);
    
    /// <summary>
    /// Deserialize a typed object from string data
    /// </summary>
    T Deserialize<T>(string data);
}