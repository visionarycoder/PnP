namespace Snippets.DesignPatterns.Behavioral.ChainOfResponsibility;

public class RateLimitHandler : BaseHandler<AuthRequest, AuthResponse>
{
    private readonly Dictionary<string, List<DateTime>> requestHistory = new();
    private readonly TimeSpan timeWindow = TimeSpan.FromMinutes(1);
    private readonly int maxRequests = 5;

    protected override async Task<AuthResponse?> ProcessRequestAsync(AuthRequest request)
    {
        await Task.Delay(5);

        var key = $"{request.IpAddress}:{request.Username}";
        var now = DateTime.UtcNow;

        Console.WriteLine($"[Rate Limit] Checking rate limit for: {key}");

        if (!requestHistory.ContainsKey(key))
        {
            requestHistory[key] = [];
        }

        var requests = requestHistory[key];
        requests.RemoveAll(r => now - r > timeWindow);

        if (requests.Count >= maxRequests)
        {
            Console.WriteLine($"[Rate Limit] Rate limit exceeded for {key}");
            return AuthResponse.Failure($"Rate limit exceeded. Try again later.", nameof(RateLimitHandler));
        }

        requests.Add(now);
        Console.WriteLine($"[Rate Limit] Rate limit check passed for {key} - continuing chain");
        return null;
    }
}