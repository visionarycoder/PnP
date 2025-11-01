namespace Snippets.DesignPatterns.Behavioral.ChainOfResponsibility;

public class BasicAuthHandler : BaseHandler<AuthRequest, AuthResponse>
{
    private readonly Dictionary<string, (string Password, string UserId, List<string> Roles)> users = new()
    {
        ["admin"] = ("admin123", "usr_001", ["admin", "user"]),
        ["user1"] = ("password", "usr_002", ["user"]),
        ["guest"] = ("guest123", "usr_003", ["guest"])
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

        if (!users.TryGetValue(request.Username, out var userData))
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
        return AuthResponse.Success(userData.UserId, userData.Roles, nameof(BasicAuthHandler),
            "Basic authentication successful");
    }
}