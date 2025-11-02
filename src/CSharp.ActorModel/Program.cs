using CSharp.ActorModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CSharp.ActorModel;

/// <summary>
/// Demonstrates the Actor Model pattern implementation in C#.
/// 
/// The Actor Model is a concurrent computation model where "actors" are fundamental
/// units of computation that process messages sequentially, maintaining internal state
/// and communicating through asynchronous message passing.
/// 
/// Key Features Demonstrated:
/// - Message-based communication
/// - Actor lifecycle management
/// - Supervision strategies for fault tolerance
/// - Actor system coordination
/// - Mailbox queuing and processing
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("=== Actor Model Pattern Demo ===\n");

        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices(services =>
            {
                services.AddLogging(builder =>
                    builder.AddConsole().SetMinimumLevel(LogLevel.Information));
            })
            .Build();

        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        
        await DemoBasicActorOperations(logger);
        await DemoActorCommunication(logger);
        await DemoSupervisionStrategies(logger);
        await DemoActorSystemCoordination(logger);
        
        Console.WriteLine("\n=== Demo Complete ===");
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }

    /// <summary>
    /// Demonstrates basic actor creation, message sending, and state management
    /// </summary>
    private static async Task DemoBasicActorOperations(ILogger logger)
    {
        Console.WriteLine("1. Basic Actor Operations");
        Console.WriteLine("------------------------");

        // Create actor system
        var actorSystem = new ActorSystem("DemoSystem", logger);

        try
        {
            // Create counter actor
            var counterRef = actorSystem.ActorOf<CounterActor>("counter");

            // Send increment messages
            await counterRef.Tell(new IncrementMessage(5));
            await counterRef.Tell(new IncrementMessage(3));
            await counterRef.Tell(new IncrementMessage(2));

            // Get current count
            var response = await counterRef.Ask<CountResponseMessage>(new GetCountMessage());
            Console.WriteLine($"Counter value after increments: {response.Count}");

            // Reset and increment again
            await counterRef.Tell(new IncrementMessage(-10)); // Decrement
            var finalResponse = await counterRef.Ask<CountResponseMessage>(new GetCountMessage());
            Console.WriteLine($"Counter value after decrement: {finalResponse.Count}");
        }
        finally
        {
            await actorSystem.Shutdown();
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrates actor-to-actor communication patterns
    /// </summary>
    private static async Task DemoActorCommunication(ILogger logger)
    {
        Console.WriteLine("2. Actor Communication Patterns");
        Console.WriteLine("-------------------------------");

        var actorSystem = new ActorSystem("CommunicationSystem", logger);

        try
        {
            // Create multiple counter actors
            var counter1 = actorSystem.ActorOf<CounterActor>("counter1");
            var counter2 = actorSystem.ActorOf<CounterActor>("counter2");
            var counter3 = actorSystem.ActorOf<CounterActor>("counter3");

            // Demonstrate parallel message processing
            var tasks = new[]
            {
                SendMessages(counter1, "Counter1", 10),
                SendMessages(counter2, "Counter2", 15),
                SendMessages(counter3, "Counter3", 20)
            };

            await Task.WhenAll(tasks);

            // Get final counts
            var count1 = await counter1.Ask<CountResponseMessage>(new GetCountMessage());
            var count2 = await counter2.Ask<CountResponseMessage>(new GetCountMessage());
            var count3 = await counter3.Ask<CountResponseMessage>(new GetCountMessage());

            Console.WriteLine($"Final counts - Counter1: {count1.Count}, Counter2: {count2.Count}, Counter3: {count3.Count}");
        }
        finally
        {
            await actorSystem.Shutdown();
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrates supervision strategies for fault tolerance
    /// </summary>
    private static async Task DemoSupervisionStrategies(ILogger logger)
    {
        Console.WriteLine("3. Supervision Strategies");
        Console.WriteLine("-------------------------");

        var actorSystem = new ActorSystem("SupervisionSystem", logger);

        try
        {
            var faultyActor = actorSystem.ActorOf<FaultyActor>("faulty");

            // Send messages that will cause different types of failures
            await faultyActor.Tell(new CauseArgumentExceptionMessage());
            await Task.Delay(100); // Let supervision handle the error

            await faultyActor.Tell(new CauseInvalidOperationMessage());
            await Task.Delay(100); // Let supervision handle the error

            // Send normal message after errors
            await faultyActor.Tell(new NormalMessage("After errors"));

            Console.WriteLine("Faulty actor handled errors through supervision strategies");
        }
        finally
        {
            await actorSystem.Shutdown();
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrates actor system coordination and lifecycle management
    /// </summary>
    private static async Task DemoActorSystemCoordination(ILogger logger)
    {
        Console.WriteLine("4. Actor System Coordination");
        Console.WriteLine("----------------------------");

        var actorSystem = new ActorSystem("CoordinationSystem", logger);

        try
        {
            // Create a hierarchy of actors
            var supervisor = actorSystem.ActorOf<SupervisorActor>("supervisor");
            var worker1 = actorSystem.ActorOf<WorkerActor>("worker1");
            var worker2 = actorSystem.ActorOf<WorkerActor>("worker2");

            // Coordinate work through supervisor
            await supervisor.Tell(new CoordinateWorkMessage("Task1", "Task2", "Task3"));

            // Let workers process tasks
            await Task.Delay(500);

            Console.WriteLine("Actor system coordination completed");

            // Demonstrate graceful shutdown
            await actorSystem.Shutdown();
            Console.WriteLine("Actor system shut down gracefully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during actor system coordination");
        }

        Console.WriteLine();
    }

    private static async Task SendMessages(IActorRef actorRef, string actorName, int count)
    {
        for (int i = 1; i <= count; i++)
        {
            await actorRef.Tell(new IncrementMessage(1));
            if (i % 5 == 0)
            {
                await Task.Delay(10); // Simulate processing time
            }
        }
        Console.WriteLine($"{actorName} processed {count} messages");
    }
}