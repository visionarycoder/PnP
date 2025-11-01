namespace Snippets.DesignPatterns.Structural.Bridge;

public static class MessageBridgeFactory
{
    public static Message CreateMessage(string messageType, string content, string senderType,
        Dictionary<string, string>? parameters = null)
    {
        var sender = CreateSender(senderType, parameters);

        return messageType.ToLower() switch
        {
            "text" => new TextMessage(content, sender),
            "alert" => new AlertMessage(parameters?.GetValueOrDefault("alertType", "INFO") ?? "INFO", content, sender),
            "rich" => CreateRichMessage(content, sender, parameters),
            _ => throw new ArgumentException($"Unknown message type: {messageType}")
        };
    }

    private static IMessageSender CreateSender(string senderType, Dictionary<string, string>? parameters = null)
    {
        return senderType.ToLower() switch
        {
            "email" => new EmailSender(
                parameters?.GetValueOrDefault("smtpServer", "smtp.gmail.com") ?? "smtp.gmail.com",
                int.Parse(parameters?.GetValueOrDefault("port", "587") ?? "587")),
            "sms" => new SmsSender(
                parameters?.GetValueOrDefault("provider", "Twilio") ?? "Twilio",
                parameters?.GetValueOrDefault("apiKey", "API_KEY_123") ?? "API_KEY_123"),
            "slack" => new SlackSender(
                parameters?.GetValueOrDefault("workspace", "MyCompany") ?? "MyCompany",
                parameters?.GetValueOrDefault("botToken", "xoxb-token-123") ?? "xoxb-token-123"),
            "push" => new PushNotificationSender(
                parameters?.GetValueOrDefault("platform", "Firebase") ?? "Firebase"),
            _ => throw new ArgumentException($"Unknown sender type: {senderType}")
        };
    }

    private static RichMessage CreateRichMessage(string content, IMessageSender sender,
        Dictionary<string, string>? parameters)
    {
        var title = parameters?.GetValueOrDefault("title", "Rich Message") ?? "Rich Message";
        var imageUrl = parameters?.GetValueOrDefault("imageUrl");

        var richMessage = new RichMessage(title, content, sender, imageUrl);

        // Add any additional metadata
        if (parameters != null)
        {
            foreach (var kvp in parameters.Where(p => !new[] { "title", "imageUrl" }.Contains(p.Key)))
            {
                richMessage.AddMetadata(kvp.Key, kvp.Value);
            }
        }

        return richMessage;
    }
}