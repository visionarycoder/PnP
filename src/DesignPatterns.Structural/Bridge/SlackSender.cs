namespace Snippets.DesignPatterns.Structural.Bridge;

public class SlackSender(string workspace = "MyCompany", string botToken = "xoxb-token-123")
    : IMessageSender
{
    private readonly string botToken = botToken;

    public void SendMessage(string message, string recipient)
    {
        Console.WriteLine($"💬 Slack message sent to {workspace}");
        Console.WriteLine($"   Channel/User: {recipient}");
        Console.WriteLine($"   Message: {message}");
        Console.WriteLine($"   Status: ✅ Posted to Slack");
    }

    public string GetSenderType() => "Slack";
}