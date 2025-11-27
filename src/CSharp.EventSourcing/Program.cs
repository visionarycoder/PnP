using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CSharp.EventSourcing;

// Sample domain events

// Sample aggregate

// Sample commands

// Sample queries

// Command handlers

// Query handlers

// Sample projection

// Main program demonstrating event sourcing patterns
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== Event Sourcing Patterns Demo ===\n");

        // Setup dependency injection
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        
        // Register event sourcing components
        services.AddSingleton<IEventSerializer, JsonEventSerializer>();
        services.AddSingleton<IEventStore, InMemoryEventStore>();
        services.AddSingleton<ISnapshotStrategy>(new SimpleSnapshotStrategy(3));
        services.AddSingleton<IEventSourcedRepository<User>, EventSourcedRepository<User>>();
        services.AddSingleton<IProjectionManager, ProjectionManager>();
        services.AddSingleton<UserProjection>();
        services.AddSingleton<ICommandDispatcher, CommandDispatcher>();
        services.AddSingleton<IQueryDispatcher, QueryDispatcher>();
        services.AddSingleton<IEventReplayService, EventReplayService>();
        
        // Register handlers
        services.AddTransient<ICommandHandler<CreateUserCommand>, CreateUserCommandHandler>();
        services.AddTransient<ICommandHandler<ChangeUserEmailCommand>, ChangeUserEmailCommandHandler>();
        services.AddTransient<IQueryHandler<GetUserQuery, User?>, GetUserQueryHandler>();

        var serviceProvider = services.BuildServiceProvider();
        
        try
        {
            await RunEventSourcingDemo(serviceProvider);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    static async Task RunEventSourcingDemo(ServiceProvider serviceProvider)
    {
        var commandDispatcher = serviceProvider.GetRequiredService<ICommandDispatcher>();
        var queryDispatcher = serviceProvider.GetRequiredService<IQueryDispatcher>();
        var projectionManager = serviceProvider.GetRequiredService<IProjectionManager>();
        var userProjection = serviceProvider.GetRequiredService<UserProjection>();
        var eventStore = serviceProvider.GetRequiredService<IEventStore>();
        
        // Register projection
        projectionManager.RegisterProjection(userProjection);

        Console.WriteLine("--- Creating Users ---");
        
        // Create users via direct repository (for demo purposes with known IDs)
        var userRepository = serviceProvider.GetRequiredService<IEventSourcedRepository<User>>();
        
        var user1 = new User("John Doe", "john@example.com");
        var userId1 = user1.Id;
        
        // Project events before saving
        foreach (var evt in user1.UncommittedEvents)
        {
            await projectionManager.ProjectEventAsync(evt);
        }
        await userRepository.SaveAsync(user1);
        
        var user2 = new User("Jane Smith", "jane@example.com");
        var userId2 = user2.Id;
        
        // Project events before saving
        foreach (var evt in user2.UncommittedEvents)
        {
            await projectionManager.ProjectEventAsync(evt);
        }
        await userRepository.SaveAsync(user2);
        
        Console.WriteLine("Created 2 users");

        // Change email
        Console.WriteLine("\n--- Changing User Email ---");
        var userToUpdate = await userRepository.GetByIdAsync(userId1);
        if (userToUpdate != null)
        {
            Console.WriteLine($"Loaded user version: {userToUpdate.Version}");
        var currentVersionInStore = await eventStore.GetCurrentVersionAsync(userId1);
        Console.WriteLine($"Current version in event store: {currentVersionInStore}");
            userToUpdate.ChangeEmail("john.doe@newcompany.com");
            Console.WriteLine($"After email change, user version: {userToUpdate.Version}");
        Console.WriteLine($"Uncommitted events count: {userToUpdate.UncommittedEvents.Count()}");
            
            // Project the email change event before saving
            foreach (var evt in userToUpdate.UncommittedEvents)
            {
                await projectionManager.ProjectEventAsync(evt);
            }
            await userRepository.SaveAsync(userToUpdate);
            Console.WriteLine("Changed John's email");
        }

        // Query users
        Console.WriteLine("\n--- Querying Users ---");
        var user = await userRepository.GetByIdAsync(userId1);
        
        if (user != null)
        {
            Console.WriteLine($"User: {user.Name} ({user.Email}) - Version: {user.Version}");
        }

        // Show projection data
        Console.WriteLine("\n--- Projection Data ---");
        var allUsers = userProjection.GetAllUsers();
        foreach (var readModel in allUsers)
        {
            Console.WriteLine($"ReadModel: {readModel.Name} ({readModel.Email}) - Active: {readModel.IsActive}");
        }

        // Show event replay
        Console.WriteLine("\n--- Event Replay Demo ---");
        var replayService = serviceProvider.GetRequiredService<IEventReplayService>();
        
        // Reset projection and rebuild
        await userProjection.ResetAsync();
        Console.WriteLine("Reset projection");
        
        await replayService.ReplayEventsFromPositionAsync(0, 100);
        Console.WriteLine("Replayed all events");
        
        // Verify projection is rebuilt
        var rebuiltUsers = userProjection.GetAllUsers();
        Console.WriteLine($"Rebuilt projection has {rebuiltUsers.Count()} users");

        // Show event streaming
        Console.WriteLine("\n--- Event Streaming Demo ---");
        var eventStream = await eventStore.GetEventStreamAsync(userId1);
        
        Console.WriteLine($"Event stream for user {userId1}:");
        await foreach (var evt in eventStream)
        {
            Console.WriteLine($"  Event: {evt.EventType} at {evt.Timestamp:HH:mm:ss}");
        }

        // Show snapshot creation (after 3 events)
        Console.WriteLine("\n--- Snapshot Demo ---");
        var repository = serviceProvider.GetRequiredService<IEventSourcedRepository<User>>();
        var userForSnapshot = await repository.GetByIdAsync(userId1);
        
        if (userForSnapshot != null)
        {
            // Create more events to trigger snapshot
            userForSnapshot.ChangeEmail("john.doe.final@example.com");
            await repository.SaveAsync(userForSnapshot);
            
            userForSnapshot.ChangeEmail("john.doe.latest@example.com");
            await repository.SaveAsync(userForSnapshot);
            
            Console.WriteLine($"User now at version {userForSnapshot.Version} - snapshot should be created");
        }

        // Show aggregate recreation from events
        Console.WriteLine("\n--- Aggregate Reconstruction Demo ---");
        var reconstructedUser = await repository.GetByIdAsync(userId1);
        if (reconstructedUser != null)
        {
            Console.WriteLine($"Reconstructed user: {reconstructedUser.Name} ({reconstructedUser.Email}) - Version: {reconstructedUser.Version}");
            Console.WriteLine($"User has {reconstructedUser.UncommittedEvents.Count()} uncommitted events");
        }

        Console.WriteLine("\n=== Demo completed successfully! ===");
        Console.WriteLine("\nKey concepts demonstrated:");
        Console.WriteLine("- Event sourcing with aggregate roots");
        Console.WriteLine("- CQRS with commands and queries");
        Console.WriteLine("- Event projections and read models");
        Console.WriteLine("- Event replay and projection rebuilding");
        Console.WriteLine("- Optimistic concurrency control");
        Console.WriteLine("- Snapshot creation and restoration");
        Console.WriteLine("- Event streaming and serialization");
    }
}