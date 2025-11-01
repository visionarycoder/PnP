namespace Snippets.DesignPatterns.Behavioral.Mediator;

public class ChatUser(string id, string displayName) : BaseColleague(id)
{
    public string DisplayName { get; } = displayName;
    public bool IsOnline { get; private set; } = true;

    public async Task SendMessage(string content, string? targetId = null)
    {
        if (targetId == null)
        {
            var message = new ChatMessage(content, Id, DateTime.UtcNow);
            await SendAsync(message);
        }
        else
        {
            var privateMessage = new PrivateMessage(content, Id, targetId, DateTime.UtcNow);
            await SendAsync(privateMessage);
        }
    }

    public override async Task ReceiveAsync(object message, IColleague sender)
    {
        await Task.CompletedTask;

        switch (message)
        {
            case ChatMessage chatMsg:
                Console.WriteLine($"[{DisplayName}] Received: {chatMsg.Content} (from {sender.Id})");
                break;

            case PrivateMessage privateMsg:
                Console.WriteLine($"[{DisplayName}] Private: {privateMsg.Content} (from {sender.Id})");
                break;

            case UserJoinedMessage joinMsg:
                Console.WriteLine($"[{DisplayName}] {joinMsg.UserId} joined the chat");
                break;

            case UserLeftMessage leftMsg:
                Console.WriteLine($"[{DisplayName}] {leftMsg.UserId} left the chat");
                break;

            case SystemMessage sysMsg:
                Console.WriteLine($"[{DisplayName}] SYSTEM: {sysMsg.Content}");
                break;
        }
    }

    public void SetOffline()
    {
        IsOnline = false;
    }

    public void SetOnline()
    {
        IsOnline = true;
    }
}