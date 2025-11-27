using Microsoft.Extensions.Logging;

namespace CSharp.CacheInvalidation;

public class EventDrivenInvalidationService : IDisposable
{
    private readonly ICacheInvalidationService invalidationService;
    private readonly IEventSubscriber eventSubscriber;
    private readonly Dictionary<string, Func<object, Task>> eventHandlers;
    private readonly ILogger? logger;
    private bool disposed = false;

    public EventDrivenInvalidationService(
        ICacheInvalidationService invalidationService,
        IEventSubscriber eventSubscriber,
        ILogger<EventDrivenInvalidationService>? logger = null)
    {
        this.invalidationService = invalidationService ?? throw new ArgumentNullException(nameof(invalidationService));
        this.eventSubscriber = eventSubscriber ?? throw new ArgumentNullException(nameof(eventSubscriber));
        this.logger = logger;
        
        eventHandlers = new Dictionary<string, Func<object, Task>>();
        SetupEventHandlers();
    }

    public void RegisterEventHandler(string eventType, Func<object, Task> handler)
    {
        eventHandlers[eventType] = handler;
        logger?.LogInformation("Registered event handler for {EventType}", eventType);
    }

    private void SetupEventHandlers()
    {
        // User-related events
        RegisterEventHandler("user.updated", async eventData =>
        {
            if (eventData is UserUpdatedEvent userEvent)
            {
                await invalidationService.InvalidateByPatternAsync($"user:{userEvent.UserId}*")
                    .ConfigureAwait(false);
                await invalidationService.InvalidateByTagAsync("user-profiles")
                    .ConfigureAwait(false);
            }
        });

        // Product-related events  
        RegisterEventHandler("product.updated", async eventData =>
        {
            if (eventData is ProductUpdatedEvent productEvent)
            {
                await invalidationService.InvalidateAsync($"product:{productEvent.ProductId}")
                    .ConfigureAwait(false);
                await invalidationService.InvalidateByTagAsync($"category:{productEvent.CategoryId}")
                    .ConfigureAwait(false);
            }
        });

        // Order-related events
        RegisterEventHandler("order.created", async eventData =>
        {
            if (eventData is OrderCreatedEvent orderEvent)
            {
                await invalidationService.InvalidateByPatternAsync($"inventory:*")
                    .ConfigureAwait(false);
                await invalidationService.InvalidateAsync($"user:{orderEvent.UserId}:orders")
                    .ConfigureAwait(false);
            }
        });

        // Configuration changes
        RegisterEventHandler("config.changed", async eventData =>
        {
            if (eventData is ConfigChangedEvent configEvent)
            {
                await invalidationService.InvalidateByTagAsync("configuration")
                    .ConfigureAwait(false);
                await invalidationService.InvalidateByPatternAsync($"config:*")
                    .ConfigureAwait(false);
            }
        });
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await eventSubscriber.SubscribeAsync(HandleEventAsync, cancellationToken)
            .ConfigureAwait(false);
        
        logger?.LogInformation("Event-driven cache invalidation service started");
    }

    private async Task HandleEventAsync(EventMessage eventMessage)
    {
        try
        {
            if (eventHandlers.TryGetValue(eventMessage.EventType, out var handler))
            {
                await handler(eventMessage.Data).ConfigureAwait(false);
                logger?.LogTrace("Processed cache invalidation for event {EventType}", 
                    eventMessage.EventType);
            }
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error handling cache invalidation event {EventType}", 
                eventMessage.EventType);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await eventSubscriber.UnsubscribeAsync(cancellationToken).ConfigureAwait(false);
        logger?.LogInformation("Event-driven cache invalidation service stopped");
    }

    public void Dispose()
    {
        if (!disposed)
        {
            eventSubscriber?.Dispose();
            disposed = true;
        }
    }
}