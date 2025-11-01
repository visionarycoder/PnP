namespace Snippets.DesignPatterns.Structural.Bridge;

public class RichMessage(
    string title,
    string content,
    IMessageSender messageSender,
    string? imageUrl = null)
    : Message(messageSender)
{
    private readonly string title = title ?? throw new ArgumentNullException(nameof(title));
    private readonly string content = content ?? throw new ArgumentNullException(nameof(content));
    private readonly Dictionary<string, string> metadata = new();

    public void AddMetadata(string key, string value)
    {
        metadata[key] = value;
    }

    public override void Send(string recipient)
    {
        Console.WriteLine($"\n🎨 Sending Rich Message:");
        var richContent = $"Title: {title}\nContent: {content}";

        if (!string.IsNullOrEmpty(imageUrl))
        {
            richContent += $"\nImage: {imageUrl}";
        }

        if (metadata.Count > 0)
        {
            richContent += "\nMetadata:";
            foreach (var kvp in metadata)
            {
                richContent += $"\n  {kvp.Key}: {kvp.Value}";
            }
        }

        MessageSender.SendMessage(richContent, recipient);
    }
}