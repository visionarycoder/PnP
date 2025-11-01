namespace Snippets.DesignPatterns.Behavioral.ChainOfResponsibility;

public class AuthorizationHandler : BaseHandler<AuthRequest, AuthResponse>
{
    protected override async Task<AuthResponse?> ProcessRequestAsync(AuthRequest request)
    {
        await Task.Delay(20);

        Console.WriteLine($"[Authorization] Checking permissions for resource: {request.Resource}");

        // This handler only processes requests that have been authenticated
        if (string.IsNullOrEmpty(request.Context.GetValueOrDefault("AuthenticatedUserId")?.ToString()))
        {
            Console.WriteLine("[Authorization] No authenticated user - passing to next handler");
            return null;
        }

        var userRoles = (List<string>?)request.Context.GetValueOrDefault("UserRoles") ?? [];

        if (request.RequiredRoles.Count == 0)
        {
            Console.WriteLine("[Authorization] No specific roles required - access granted");
            return null; // Continue chain
        }

        var hasRequiredRole = request.RequiredRoles.Any(role => userRoles.Contains(role));

        if (!hasRequiredRole)
        {
            Console.WriteLine(
                $"[Authorization] Access denied - user roles: [{string.Join(", ", userRoles)}], required: [{string.Join(", ", request.RequiredRoles)}]");
            return AuthResponse.Failure(
                $"Insufficient permissions. Required roles: {string.Join(", ", request.RequiredRoles)}",
                nameof(AuthorizationHandler));
        }

        Console.WriteLine($"[Authorization] Access granted for resource: {request.Resource}");
        return null; // Continue to next handler or complete successfully
    }
}