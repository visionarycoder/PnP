namespace Snippets.DesignPatterns.Structural.Bridge;

public abstract class Message(IMessageSender messageSender)
{
    protected IMessageSender MessageSender = messageSender ?? throw new ArgumentNullException(nameof(messageSender));

    public abstract void Send(string recipient);

    public virtual void SetSender(IMessageSender messageSender)
    {
        MessageSender = messageSender ?? throw new ArgumentNullException(nameof(messageSender));
    }

    public string GetSenderType() => MessageSender.GetSenderType();
}