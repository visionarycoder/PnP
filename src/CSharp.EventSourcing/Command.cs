using System;
using System.Threading;
using System.Threading.Tasks;

namespace CSharp.EventSourcing;

// CQRS (Command Query Responsibility Segregation) Interfaces

/// <summary>
/// Base abstract class for commands
/// </summary>
public abstract class Command : ICommand
{
    protected Command()
    {
        CommandId = Guid.NewGuid();
        Timestamp = DateTime.UtcNow;
    }

    public Guid CommandId { get; private set; }
    public DateTime Timestamp { get; private set; }
}