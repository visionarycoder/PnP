namespace Snippets.DesignPatterns.Structural.Bridge;

public interface IMessageSender
{
    void SendMessage(string message, string recipient);
    string GetSenderType();
}