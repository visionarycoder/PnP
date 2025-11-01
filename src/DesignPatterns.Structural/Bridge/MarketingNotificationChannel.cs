namespace Snippets.DesignPatterns.Structural.Bridge;

public class MarketingNotificationChannel(IMessageSender sender, string campaignId) : NotificationChannel(sender)
{
    private readonly string campaignId = campaignId ?? "CAMPAIGN_001";

    public override void Broadcast(string message)
    {
        var marketingMessage = $"{message}\n\nCampaign ID: {campaignId}\nUnsubscribe: reply STOP";
        Console.WriteLine($"\n📢 Marketing Broadcast to {Recipients.Count} recipients:");

        foreach (var recipient in Recipients)
        {
            Sender.SendMessage(marketingMessage, recipient);
        }
    }
}