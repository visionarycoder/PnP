namespace Snippets.DesignPatterns.Behavioral.ChainOfResponsibility;

// Base handler interface

// Abstract base handler with chain management
public abstract class BaseHandler<TRequest, TResponse> : IRequestHandler<TRequest, TResponse>
{
    public IRequestHandler<TRequest, TResponse>? NextHandler { get; set; }

    public virtual async Task<TResponse?> HandleAsync(TRequest request)
    {
        var response = await ProcessRequestAsync(request);

        if (response != null)
        {
            return response;
        }

        // Pass to next handler if current handler can't process the request
        if (NextHandler != null)
        {
            return await NextHandler.HandleAsync(request);
        }

        return default(TResponse);
    }

    protected abstract Task<TResponse?> ProcessRequestAsync(TRequest request);

    public IRequestHandler<TRequest, TResponse> SetNext(IRequestHandler<TRequest, TResponse> handler)
    {
        NextHandler = handler;
        return handler;
    }
}

// Request and response models

// IP Whitelist Handler

// Rate Limiting Handler

// Basic Authentication Handler

// Token Authentication Handler

// Authorization Handler

// Logging Handler

// Chain Builder for easier setup

// Simple Example: Support Ticket System

// Support Handler Base

// Critical Issues Handler

// Technical Issues Handler

// Billing Issues Handler

// General Support Handler (catch-all)