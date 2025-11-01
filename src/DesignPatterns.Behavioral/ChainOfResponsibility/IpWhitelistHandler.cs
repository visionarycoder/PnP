namespace Snippets.DesignPatterns.Behavioral.ChainOfResponsibility;

public class IpWhitelistHandler : BaseHandler<AuthRequest, AuthResponse>
{
    private readonly HashSet<string> allowedIps = ["127.0.0.1", "192.168.1.0", "10.0.0.0"];

    protected override async Task<AuthResponse?> ProcessRequestAsync(AuthRequest request)
    {
        await Task.Delay(10); // Simulate async processing

        Console.WriteLine($"[IP Whitelist] Checking IP: {request.IpAddress}");

        if (!allowedIps.Contains(request.IpAddress))
        {
            Console.WriteLine($"[IP Whitelist] IP {request.IpAddress} not in whitelist");
            return AuthResponse.Failure($"IP address {request.IpAddress} not allowed", nameof(IpWhitelistHandler));
        }

        Console.WriteLine($"[IP Whitelist] IP {request.IpAddress} is whitelisted - continuing chain");
        return null; // Continue to next handler
    }
}