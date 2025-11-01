namespace Snippets.DesignPatterns.Structural.Bridge;

public class SmsSender(string provider = "Twilio", string apiKey = "API_KEY_123") : IMessageSender
{
    private readonly string apiKey = apiKey;

    public void SendMessage(string message, string recipient)
    {
        Console.WriteLine($"📱 SMS sent via {provider}");
        Console.WriteLine($"   To: {recipient}");
        Console.WriteLine($"   Message: {message}");
        Console.WriteLine($"   Status: ✅ Delivered to mobile device");
    }

    public string GetSenderType() => "SMS";
}