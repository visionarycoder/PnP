namespace Snippets.DesignPatterns.Structural.Bridge;

public class TextMessage(string content, IMessageSender messageSender) : Message(messageSender)
{
    private readonly string content = content ?? throw new ArgumentNullException(nameof(content));

    public override void Send(string recipient)
    {
        Console.WriteLine($"\n📝 Sending Text Message:");
        MessageSender.SendMessage(content, recipient);
    }
}