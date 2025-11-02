using System;
using System.Threading;
using System.Threading.Tasks;

namespace CSharp.EventSourcing;

// CQRS (Command Query Responsibility Segregation) Interfaces

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

/// <summary>
/// Handler interface for processing commands
/// </summary>
public interface ICommandHandler<in TCommand> where TCommand : ICommand
{
    /// <summary>
    /// Handle the specified command
    /// </summary>
    Task HandleAsync(TCommand command, CancellationToken token = default);
}

/// <summary>
/// Base interface for all queries in the system
/// </summary>
public interface IQuery<out TResult>
{
    /// <summary>
    /// Unique identifier for the query
    /// </summary>
    Guid QueryId { get; }
    
    /// <summary>
    /// Timestamp when the query was created
    /// </summary>
    DateTime Timestamp { get; }
}

/// <summary>
/// Handler interface for processing queries
/// </summary>
public interface IQueryHandler<in TQuery, TResult> where TQuery : IQuery<TResult>
{
    /// <summary>
    /// Handle the specified query and return the result
    /// </summary>
    Task<TResult> HandleAsync(TQuery query, CancellationToken token = default);
}

/// <summary>
/// Dispatcher interface for commands
/// </summary>
public interface ICommandDispatcher
{
    /// <summary>
    /// Dispatch a command to its appropriate handler
    /// </summary>
    Task DispatchAsync<TCommand>(TCommand command, CancellationToken token = default) where TCommand : ICommand;
}

/// <summary>
/// Dispatcher interface for queries
/// </summary>
public interface IQueryDispatcher
{
    /// <summary>
    /// Dispatch a query to its appropriate handler and return the result
    /// </summary>
    Task<TResult> DispatchAsync<TQuery, TResult>(TQuery query, CancellationToken token = default)
        where TQuery : IQuery<TResult>;
}

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

/// <summary>
/// Base abstract class for queries
/// </summary>
public abstract class Query<TResult> : IQuery<TResult>
{
    protected Query()
    {
        QueryId = Guid.NewGuid();
        Timestamp = DateTime.UtcNow;
    }

    public Guid QueryId { get; private set; }
    public DateTime Timestamp { get; private set; }
}