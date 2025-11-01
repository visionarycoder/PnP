# Chain of Responsibility Pattern

**Description**: Creates a chain of handler objects for a request. The pattern decouples the sender of a request from its receivers by giving more than one object a chance to handle the request. Each handler either processes the request or passes it to the next handler in the chain.

**Language/Technology**: C#

**Code**:

## 1. Basic Chain Structure

```csharp
// Base handler interface
public interface IRequestHandler<TRequest, TResponse>
{
    IRequestHandler<TRequest, TResponse>? NextHandler { get; set; }
    Task<TResponse?> HandleAsync(TRequest request);
}

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
```

## 2. Authentication and Authorization Chain

```csharp
// Request and response models
public class AuthRequest
{
    public string Username { get; init; } = "";
    public string Password { get; init; } = "";
    public string Token { get; init; } = "";
    public string IpAddress { get; init; } = "";
    public string UserAgent { get; init; } = "";
    public List<string> RequiredRoles { get; init; } = new();
    public string Resource { get; init; } = "";
    public Dictionary<string, object> Context { get; init; } = new();
}

public class AuthResponse
{
    public bool IsSuccessful { get; init; }
    public string Message { get; init; } = "";
    public string UserId { get; init; } = "";
    public List<string> UserRoles { get; init; } = new();
    public Dictionary<string, object> Claims { get; init; } = new();
    public string FailureReason { get; init; } = "";
    public string HandlerName { get; init; } = "";
    
    public static AuthResponse Success(string userId, List<string> roles, string handlerName, string message = "Authentication successful")
    {
        return new AuthResponse
        {
            IsSuccessful = true,
            Message = message,
            UserId = userId,
            UserRoles = roles,
            HandlerName = handlerName
        };
    }
    
    public static AuthResponse Failure(string reason, string handlerName)
    {
        return new AuthResponse
        {
            IsSuccessful = false,
            FailureReason = reason,
            HandlerName = handlerName,
            Message = $"Authentication failed: {reason}"
        };
    }
}

// IP Whitelist Handler
public class IpWhitelistHandler : BaseHandler<AuthRequest, AuthResponse>
{
    private readonly HashSet<string> _allowedIps = new()
    {
        "127.0.0.1", "192.168.1.0", "10.0.0.0"
    };
    
    protected override async Task<AuthResponse?> ProcessRequestAsync(AuthRequest request)
    {
        await Task.Delay(10); // Simulate async processing
        
        Console.WriteLine($"[IP Whitelist] Checking IP: {request.IpAddress}");
        
        if (!_allowedIps.Contains(request.IpAddress))
        {
            Console.WriteLine($"[IP Whitelist] IP {request.IpAddress} not in whitelist");
            return AuthResponse.Failure($"IP address {request.IpAddress} not allowed", nameof(IpWhitelistHandler));
        }
        
        Console.WriteLine($"[IP Whitelist] IP {request.IpAddress} is whitelisted - continuing chain");
        return null; // Continue to next handler
    }
}

// Rate Limiting Handler
public class RateLimitHandler : BaseHandler<AuthRequest, AuthResponse>
{
    private readonly Dictionary<string, List<DateTime>> _requestHistory = new();
    private readonly TimeSpan _timeWindow = TimeSpan.FromMinutes(1);
    private readonly int _maxRequests = 5;
    
    protected override async Task<AuthResponse?> ProcessRequestAsync(AuthRequest request)
    {
        await Task.Delay(5);
        
        var key = $"{request.IpAddress}:{request.Username}";
        var now = DateTime.UtcNow;
        
        Console.WriteLine($"[Rate Limit] Checking rate limit for: {key}");
        
        if (!_requestHistory.ContainsKey(key))
        {
            _requestHistory[key] = new List<DateTime>();
        }
        
        var requests = _requestHistory[key];
        requests.RemoveAll(r => now - r > _timeWindow);
        
        if (requests.Count >= _maxRequests)
        {
            Console.WriteLine($"[Rate Limit] Rate limit exceeded for {key}");
            return AuthResponse.Failure($"Rate limit exceeded. Try again later.", nameof(RateLimitHandler));
        }
        
        requests.Add(now);
        Console.WriteLine($"[Rate Limit] Rate limit check passed for {key} - continuing chain");
        return null;
    }
}

// Basic Authentication Handler
public class BasicAuthHandler : BaseHandler<AuthRequest, AuthResponse>
{
    private readonly Dictionary<string, (string Password, string UserId, List<string> Roles)> _users = new()
    {
        ["admin"] = ("admin123", "usr_001", new List<string> { "admin", "user" }),
        ["user1"] = ("password", "usr_002", new List<string> { "user" }),
        ["guest"] = ("guest123", "usr_003", new List<string> { "guest" })
    };
    
    protected override async Task<AuthResponse?> ProcessRequestAsync(AuthRequest request)
    {
        await Task.Delay(50); // Simulate database lookup
        
        Console.WriteLine($"[Basic Auth] Validating credentials for: {request.Username}");
        
        if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
        {
            Console.WriteLine("[Basic Auth] Missing username or password - passing to next handler");
            return null; // Let another handler try (e.g., token auth)
        }
        
        if (!_users.TryGetValue(request.Username, out var userData))
        {
            Console.WriteLine($"[Basic Auth] User {request.Username} not found");
            return AuthResponse.Failure($"Invalid username", nameof(BasicAuthHandler));
        }
        
        if (userData.Password != request.Password)
        {
            Console.WriteLine($"[Basic Auth] Invalid password for {request.Username}");
            return AuthResponse.Failure($"Invalid password", nameof(BasicAuthHandler));
        }
        
        Console.WriteLine($"[Basic Auth] Authentication successful for {request.Username}");
        return AuthResponse.Success(userData.UserId, userData.Roles, nameof(BasicAuthHandler), "Basic authentication successful");
    }
}

// Token Authentication Handler
public class TokenAuthHandler : BaseHandler<AuthRequest, AuthResponse>
{
    private readonly Dictionary<string, (string UserId, List<string> Roles, DateTime Expiry)> _tokens = new()
    {
        ["token_admin_123"] = ("usr_001", new List<string> { "admin", "user" }, DateTime.UtcNow.AddHours(1)),
        ["token_user_456"] = ("usr_002", new List<string> { "user" }, DateTime.UtcNow.AddHours(1)),
        ["token_guest_789"] = ("usr_003", new List<string> { "guest" }, DateTime.UtcNow.AddMinutes(30))
    };
    
    protected override async Task<AuthResponse?> ProcessRequestAsync(AuthRequest request)
    {
        await Task.Delay(30);
        
        Console.WriteLine($"[Token Auth] Validating token: {request.Token}");
        
        if (string.IsNullOrEmpty(request.Token))
        {
            Console.WriteLine("[Token Auth] No token provided - passing to next handler");
            return null;
        }
        
        if (!_tokens.TryGetValue(request.Token, out var tokenData))
        {
            Console.WriteLine($"[Token Auth] Invalid token");
            return AuthResponse.Failure("Invalid token", nameof(TokenAuthHandler));
        }
        
        if (DateTime.UtcNow > tokenData.Expiry)
        {
            Console.WriteLine($"[Token Auth] Token expired");
            return AuthResponse.Failure("Token expired", nameof(TokenAuthHandler));
        }
        
        Console.WriteLine($"[Token Auth] Token authentication successful");
        return AuthResponse.Success(tokenData.UserId, tokenData.Roles, nameof(TokenAuthHandler), "Token authentication successful");
    }
}

// Authorization Handler
public class AuthorizationHandler : BaseHandler<AuthRequest, AuthResponse>
{
    protected override async Task<AuthResponse?> ProcessRequestAsync(AuthRequest request)
    {
        await Task.Delay(20);
        
        Console.WriteLine($"[Authorization] Checking permissions for resource: {request.Resource}");
        
        // This handler expects the previous handler to have set user roles in context
        if (!request.Context.ContainsKey("UserRoles"))
        {
            Console.WriteLine("[Authorization] No user roles found in context - passing to next handler");
            return null;
        }
        
        var userRoles = (List<string>)request.Context["UserRoles"];
        
        if (request.RequiredRoles.Any() && !request.RequiredRoles.Any(required => userRoles.Contains(required)))
        {
            Console.WriteLine($"[Authorization] Access denied - missing required roles");
            return AuthResponse.Failure($"Insufficient permissions. Required roles: {string.Join(", ", request.RequiredRoles)}", nameof(AuthorizationHandler));
        }
        
        Console.WriteLine($"[Authorization] Authorization successful");
        return null; // Authorization passed, continue chain
    }
}
```

## 3. HTTP Request Processing Chain

```csharp
public class HttpRequest
{
    public string Method { get; init; } = "";
    public string Url { get; init; } = "";
    public Dictionary<string, string> Headers { get; init; } = new();
    public string Body { get; init; } = "";
    public Dictionary<string, object> Properties { get; init; } = new();
}

public class HttpResponse
{
    public int StatusCode { get; init; }
    public string Content { get; init; } = "";
    public Dictionary<string, string> Headers { get; init; } = new();
    public bool IsHandled { get; init; }
    public string HandlerName { get; init; } = "";
    
    public static HttpResponse Ok(string content, string handlerName)
    {
        return new HttpResponse
        {
            StatusCode = 200,
            Content = content,
            IsHandled = true,
            HandlerName = handlerName
        };
    }
    
    public static HttpResponse NotFound(string handlerName)
    {
        return new HttpResponse
        {
            StatusCode = 404,
            Content = "Not Found",
            IsHandled = true,
            HandlerName = handlerName
        };
    }
    
    public static HttpResponse BadRequest(string message, string handlerName)
    {
        return new HttpResponse
        {
            StatusCode = 400,
            Content = message,
            IsHandled = true,
            HandlerName = handlerName
        };
    }
}

// CORS Handler
public class CorsHandler : BaseHandler<HttpRequest, HttpResponse>
{
    protected override async Task<HttpResponse?> ProcessRequestAsync(HttpRequest request)
    {
        await Task.CompletedTask;
        
        Console.WriteLine($"[CORS] Processing {request.Method} request to {request.Url}");
        
        if (request.Method == "OPTIONS")
        {
            Console.WriteLine("[CORS] Handling preflight request");
            return new HttpResponse
            {
                StatusCode = 200,
                Headers = new Dictionary<string, string>
                {
                    ["Access-Control-Allow-Origin"] = "*",
                    ["Access-Control-Allow-Methods"] = "GET, POST, PUT, DELETE, OPTIONS",
                    ["Access-Control-Allow-Headers"] = "Content-Type, Authorization"
                },
                IsHandled = true,
                HandlerName = nameof(CorsHandler)
            };
        }
        
        // Add CORS headers to response (would be done in middleware)
        request.Properties["CorsHeaders"] = new Dictionary<string, string>
        {
            ["Access-Control-Allow-Origin"] = "*"
        };
        
        Console.WriteLine("[CORS] CORS headers added - continuing chain");
        return null;
    }
}

// Static File Handler
public class StaticFileHandler : BaseHandler<HttpRequest, HttpResponse>
{
    private readonly Dictionary<string, string> _staticFiles = new()
    {
        ["/index.html"] = "<html><body><h1>Welcome</h1></body></html>",
        ["/about.html"] = "<html><body><h1>About Us</h1></body></html>",
        ["/style.css"] = "body { font-family: Arial, sans-serif; }",
        ["/app.js"] = "console.log('Application loaded');"
    };
    
    protected override async Task<HttpResponse?> ProcessRequestAsync(HttpRequest request)
    {
        await Task.Delay(10);
        
        Console.WriteLine($"[Static File] Checking for static file: {request.Url}");
        
        if (request.Method != "GET")
        {
            Console.WriteLine("[Static File] Not a GET request - passing to next handler");
            return null;
        }
        
        if (_staticFiles.TryGetValue(request.Url, out var content))
        {
            Console.WriteLine($"[Static File] Serving static file: {request.Url}");
            return HttpResponse.Ok(content, nameof(StaticFileHandler));
        }
        
        Console.WriteLine($"[Static File] File not found: {request.Url} - passing to next handler");
        return null;
    }
}

// API Handler
public class ApiHandler : BaseHandler<HttpRequest, HttpResponse>
{
    private readonly Dictionary<string, Func<HttpRequest, Task<HttpResponse>>> _endpoints = new();
    
    public ApiHandler()
    {
        _endpoints["/api/users"] = HandleGetUsers;
        _endpoints["/api/user"] = HandleCreateUser;
        _endpoints["/api/health"] = HandleHealthCheck;
    }
    
    protected override async Task<HttpResponse?> ProcessRequestAsync(HttpRequest request)
    {
        Console.WriteLine($"[API] Checking API endpoint: {request.Url}");
        
        if (!request.Url.StartsWith("/api/"))
        {
            Console.WriteLine("[API] Not an API request - passing to next handler");
            return null;
        }
        
        var endpoint = request.Url.Split('?')[0]; // Remove query parameters
        
        if (_endpoints.TryGetValue(endpoint, out var handler))
        {
            Console.WriteLine($"[API] Handling endpoint: {endpoint}");
            return await handler(request);
        }
        
        Console.WriteLine($"[API] Endpoint not found: {endpoint}");
        return HttpResponse.NotFound(nameof(ApiHandler));
    }
    
    private async Task<HttpResponse> HandleGetUsers(HttpRequest request)
    {
        await Task.Delay(50); // Simulate database query
        
        if (request.Method != "GET")
        {
            return HttpResponse.BadRequest("Method not allowed", nameof(ApiHandler));
        }
        
        var users = new[]
        {
            new { Id = 1, Name = "John Doe", Email = "john@example.com" },
            new { Id = 2, Name = "Jane Smith", Email = "jane@example.com" }
        };
        
        return HttpResponse.Ok(System.Text.Json.JsonSerializer.Serialize(users), nameof(ApiHandler));
    }
    
    private async Task<HttpResponse> HandleCreateUser(HttpRequest request)
    {
        await Task.Delay(100); // Simulate database insert
        
        if (request.Method != "POST")
        {
            return HttpResponse.BadRequest("Method not allowed", nameof(ApiHandler));
        }
        
        // Simulate user creation
        var newUser = new { Id = 3, Name = "New User", Email = "newuser@example.com" };
        return HttpResponse.Ok(System.Text.Json.JsonSerializer.Serialize(newUser), nameof(ApiHandler));
    }
    
    private async Task<HttpResponse> HandleHealthCheck(HttpRequest request)
    {
        await Task.CompletedTask;
        
        var health = new { Status = "Healthy", Timestamp = DateTime.UtcNow };
        return HttpResponse.Ok(System.Text.Json.JsonSerializer.Serialize(health), nameof(ApiHandler));
    }
}

// Fallback Handler
public class FallbackHandler : BaseHandler<HttpRequest, HttpResponse>
{
    protected override async Task<HttpResponse?> ProcessRequestAsync(HttpRequest request)
    {
        await Task.CompletedTask;
        
        Console.WriteLine($"[Fallback] No handler found for: {request.Method} {request.Url}");
        
        return new HttpResponse
        {
            StatusCode = 404,
            Content = $"<html><body><h1>404 - Page Not Found</h1><p>The requested resource {request.Url} was not found.</p></body></html>",
            IsHandled = true,
            HandlerName = nameof(FallbackHandler)
        };
    }
}
```

## 4. Chain Builder and Management

```csharp
public class ChainBuilder<TRequest, TResponse>
{
    private readonly List<IRequestHandler<TRequest, TResponse>> _handlers = new();
    
    public ChainBuilder<TRequest, TResponse> AddHandler(IRequestHandler<TRequest, TResponse> handler)
    {
        _handlers.Add(handler);
        return this;
    }
    
    public ChainBuilder<TRequest, TResponse> AddHandler<THandler>() 
        where THandler : IRequestHandler<TRequest, TResponse>, new()
    {
        _handlers.Add(new THandler());
        return this;
    }
    
    public IRequestHandler<TRequest, TResponse> Build()
    {
        if (_handlers.Count == 0)
        {
            throw new InvalidOperationException("At least one handler must be added to the chain");
        }
        
        // Link handlers together
        for (int i = 0; i < _handlers.Count - 1; i++)
        {
            _handlers[i].SetNext(_handlers[i + 1]);
        }
        
        return _handlers[0];
    }
    
    public async Task<TResponse?> ProcessAsync(TRequest request)
    {
        var chain = Build();
        return await chain.HandleAsync(request);
    }
}

// Chain with conditional handlers
public class ConditionalChain<TRequest, TResponse>
{
    private readonly List<(Func<TRequest, bool> Condition, IRequestHandler<TRequest, TResponse> Handler)> _conditionalHandlers = new();
    
    public ConditionalChain<TRequest, TResponse> AddConditionalHandler(
        Func<TRequest, bool> condition, 
        IRequestHandler<TRequest, TResponse> handler)
    {
        _conditionalHandlers.Add((condition, handler));
        return this;
    }
    
    public async Task<TResponse?> ProcessAsync(TRequest request)
    {
        foreach (var (condition, handler) in _conditionalHandlers)
        {
            if (condition(request))
            {
                var result = await handler.HandleAsync(request);
                if (result != null)
                {
                    return result;
                }
            }
        }
        
        return default(TResponse);
    }
}

// Parallel Chain (runs handlers in parallel and returns first successful result)
public class ParallelChain<TRequest, TResponse>
{
    private readonly List<IRequestHandler<TRequest, TResponse>> _handlers = new();
    
    public ParallelChain<TRequest, TResponse> AddHandler(IRequestHandler<TRequest, TResponse> handler)
    {
        _handlers.Add(handler);
        return this;
    }
    
    public async Task<TResponse?> ProcessAsync(TRequest request)
    {
        if (_handlers.Count == 0)
        {
            return default(TResponse);
        }
        
        var tasks = _handlers.Select(h => h.HandleAsync(request));
        
        while (tasks.Any())
        {
            var completedTask = await Task.WhenAny(tasks);
            var result = await completedTask;
            
            if (result != null)
            {
                return result;
            }
            
            tasks = tasks.Where(t => t != completedTask);
        }
        
        return default(TResponse);
    }
}

// Chain Statistics and Monitoring
public class MonitoringChain<TRequest, TResponse>
{
    private readonly IRequestHandler<TRequest, TResponse> _chain;
    private readonly Dictionary<string, ChainStatistics> _statistics = new();
    
    public MonitoringChain(IRequestHandler<TRequest, TResponse> chain)
    {
        _chain = chain;
    }
    
    public async Task<TResponse?> ProcessAsync(TRequest request)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var requestId = Guid.NewGuid().ToString();
        
        Console.WriteLine($"[Monitor] Starting request {requestId}");
        
        try
        {
            var result = await _chain.HandleAsync(request);
            stopwatch.Stop();
            
            RecordStatistics("Success", stopwatch.ElapsedMilliseconds);
            Console.WriteLine($"[Monitor] Request {requestId} completed successfully in {stopwatch.ElapsedMilliseconds}ms");
            
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            RecordStatistics("Error", stopwatch.ElapsedMilliseconds);
            Console.WriteLine($"[Monitor] Request {requestId} failed after {stopwatch.ElapsedMilliseconds}ms: {ex.Message}");
            throw;
        }
    }
    
    private void RecordStatistics(string outcome, long duration)
    {
        if (!_statistics.ContainsKey(outcome))
        {
            _statistics[outcome] = new ChainStatistics();
        }
        
        var stats = _statistics[outcome];
        stats.RequestCount++;
        stats.TotalDuration += duration;
        stats.AverageDuration = stats.TotalDuration / stats.RequestCount;
        
        if (duration > stats.MaxDuration)
        {
            stats.MaxDuration = duration;
        }
        
        if (stats.MinDuration == 0 || duration < stats.MinDuration)
        {
            stats.MinDuration = duration;
        }
    }
    
    public Dictionary<string, ChainStatistics> GetStatistics() => _statistics.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
}

public class ChainStatistics
{
    public long RequestCount { get; set; }
    public long TotalDuration { get; set; }
    public long AverageDuration { get; set; }
    public long MaxDuration { get; set; }
    public long MinDuration { get; set; }
}
```

**Usage**:

```csharp
// 1. Authentication Chain Example
var authChain = new ChainBuilder<AuthRequest, AuthResponse>()
    .AddHandler(new IpWhitelistHandler())
    .AddHandler(new RateLimitHandler())
    .AddHandler(new BasicAuthHandler())
    .AddHandler(new TokenAuthHandler())
    .Build();

// Test basic authentication
var basicAuthRequest = new AuthRequest
{
    Username = "admin",
    Password = "admin123",
    IpAddress = "127.0.0.1",
    RequiredRoles = new List<string> { "admin" }
};

var authResponse = await authChain.HandleAsync(basicAuthRequest);
Console.WriteLine($"Auth Result: {authResponse?.Message} (Handler: {authResponse?.HandlerName})");

// Test token authentication
var tokenAuthRequest = new AuthRequest
{
    Token = "token_admin_123",
    IpAddress = "127.0.0.1"
};

var tokenResponse = await authChain.HandleAsync(tokenAuthRequest);
Console.WriteLine($"Token Auth: {tokenResponse?.Message}");

// 2. HTTP Request Processing Chain
var httpChain = new ChainBuilder<HttpRequest, HttpResponse>()
    .AddHandler(new CorsHandler())
    .AddHandler(new StaticFileHandler())
    .AddHandler(new ApiHandler())
    .AddHandler(new FallbackHandler())
    .Build();

// Test static file request
var staticRequest = new HttpRequest
{
    Method = "GET",
    Url = "/index.html"
};

var staticResponse = await httpChain.HandleAsync(staticRequest);
Console.WriteLine($"Static Response: {staticResponse?.StatusCode} - {staticResponse?.HandlerName}");

// Test API request
var apiRequest = new HttpRequest
{
    Method = "GET",
    Url = "/api/users"
};

var apiResponse = await httpChain.HandleAsync(apiRequest);
Console.WriteLine($"API Response: {apiResponse?.StatusCode} - Content Length: {apiResponse?.Content.Length}");

// Test 404 case
var notFoundRequest = new HttpRequest
{
    Method = "GET",
    Url = "/nonexistent"
};

var notFoundResponse = await httpChain.HandleAsync(notFoundRequest);
Console.WriteLine($"404 Response: {notFoundResponse?.StatusCode} - {notFoundResponse?.HandlerName}");

// 3. Conditional Chain Example
var conditionalChain = new ConditionalChain<HttpRequest, HttpResponse>()
    .AddConditionalHandler(
        req => req.Url.StartsWith("/admin"),
        new ApiHandler())
    .AddConditionalHandler(
        req => req.Method == "GET",
        new StaticFileHandler());

// 4. Monitoring Chain Example
var monitoredChain = new MonitoringChain<HttpRequest, HttpResponse>(httpChain);

for (int i = 0; i < 5; i++)
{
    await monitoredChain.ProcessAsync(new HttpRequest
    {
        Method = "GET",
        Url = i % 2 == 0 ? "/api/health" : "/index.html"
    });
}

var statistics = monitoredChain.GetStatistics();
foreach (var stat in statistics)
{
    Console.WriteLine($"{stat.Key}: {stat.Value.RequestCount} requests, Avg: {stat.Value.AverageDuration}ms");
}

// Expected output demonstrates:
// - Sequential processing through handler chain
// - Early termination when handler processes request
// - Fallback behavior when no handler can process request
// - Authentication and authorization workflow
// - HTTP request routing and processing
// - Performance monitoring and statistics
```

**Notes**:

- **Sequential Processing**: Handlers are invoked one by one until one handles the request
- **Loose Coupling**: Sender doesn't know which handler will process the request
- **Dynamic Configuration**: Chain can be built and modified at runtime
- **Flexible Routing**: Different types of requests can follow different paths through the chain
- **Fail-Safe**: If no handler processes the request, a default response can be provided
- **Performance**: Each handler adds latency, so chain length should be considered
- **Error Handling**: Individual handlers can fail without breaking the entire chain
- **Conditional Processing**: Handlers can decide whether to process or pass based on request content
- **Composite Behavior**: Multiple chains can be combined for complex scenarios

**Prerequisites**:

- .NET 6.0 or later
- Understanding of async/await patterns
- Knowledge of interfaces and inheritance
- Familiarity with the Strategy pattern for handler implementations

**Related Patterns**:

- **Command**: Chain handlers often implement Command pattern for request processing
- **Composite**: Chain structure is similar to Composite for building handler trees
- **Strategy**: Individual handlers implement different strategies for processing requests
- **Decorator**: Similar structure but different purpose (enhancement vs. routing)
