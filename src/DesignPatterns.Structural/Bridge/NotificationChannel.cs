namespace Snippets.DesignPatterns.Structural.Bridge;

public abstract class NotificationChannel(IMessageSender sender)
{
    protected IMessageSender Sender = sender ?? throw new ArgumentNullException(nameof(sender));
    protected List<string> Recipients = [];

    public virtual void AddRecipient(string recipient)
    {
        if (!Recipients.Contains(recipient))
        {
            Recipients.Add(recipient);
        }
    }

    public virtual void RemoveRecipient(string recipient)
    {
        Recipients.Remove(recipient);
    }

    public abstract void Broadcast(string message);

    public void ChangeSender(IMessageSender newSender)
    {
        Sender = newSender ?? throw new ArgumentNullException(nameof(newSender));
        Console.WriteLine($"📡 Channel sender changed to: {Sender.GetSenderType()}");
    }
}