namespace CSharp.EventSourcing;

/// <summary>
/// Base interface for all commands in the system
/// </summary>
public interface ICommand
{
    /// <summary>
    /// Unique identifier for the command
    /// </summary>
    Guid CommandId { get; }
    
    /// <summary>
    /// Timestamp when the command was created
    /// </summary>
    DateTime Timestamp { get; }
}