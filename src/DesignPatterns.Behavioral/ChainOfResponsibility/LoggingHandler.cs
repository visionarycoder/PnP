namespace Snippets.DesignPatterns.Behavioral.ChainOfResponsibility;

public class LoggingHandler : BaseHandler<AuthRequest, AuthResponse>
{
    protected override async Task<AuthResponse?> ProcessRequestAsync(AuthRequest request)
    {
        await Task.Delay(5);

        Console.WriteLine(
            $"[Audit Log] Request: User={request.Username}, IP={request.IpAddress}, Resource={request.Resource}, UserAgent={request.UserAgent}");

        // Logging handler never blocks the chain, always continues
        return null;
    }
}