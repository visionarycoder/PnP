namespace Snippets.DesignPatterns.Behavioral.Mediator;

public class ChatRoomMediator : IMediator
{
    private readonly List<IColleague> participants = [];
    private readonly List<ChatMessage> messageHistory = [];
    private readonly Dictionary<string, UserProfile> userProfiles = new();

    public IReadOnlyList<ChatMessage> MessageHistory => messageHistory.AsReadOnly();
    public IReadOnlyList<IColleague> Participants => participants.AsReadOnly();

    public void RegisterColleague(IColleague colleague)
    {
        participants.Add(colleague);
        colleague.Mediator = this;

        // Notify all participants about new user
        var joinMessage = new UserJoinedMessage(colleague.Id, DateTime.UtcNow);
        _ = Task.Run(async () =>
        {
            foreach (var participant in participants.Where(p => p.Id != colleague.Id))
            {
                await participant.ReceiveAsync(joinMessage, colleague);
            }
        });

        Console.WriteLine($"[ChatRoom] {colleague.Id} joined the chat room");
    }

    public void RemoveColleague(IColleague colleague)
    {
        participants.Remove(colleague);
        colleague.Mediator = null;

        // Notify remaining participants
        var leaveMessage = new UserLeftMessage(colleague.Id, DateTime.UtcNow);
        _ = Task.Run(async () =>
        {
            foreach (var participant in participants)
            {
                await participant.ReceiveAsync(leaveMessage, colleague);
            }
        });

        Console.WriteLine($"[ChatRoom] {colleague.Id} left the chat room");
    }

    public async Task SendAsync(object message, IColleague sender)
    {
        switch (message)
        {
            case ChatMessage chatMsg when string.IsNullOrEmpty(chatMsg.TargetId):
                await BroadcastMessage(chatMsg, sender);
                break;

            case PrivateMessage privateMsg:
                await SendPrivateMessage(privateMsg, sender);
                break;

            case SystemMessage sysMsg:
                await BroadcastSystemMessage(sysMsg);
                break;

            default:
                Console.WriteLine($"[ChatRoom] Unknown message type: {message.GetType().Name}");
                break;
        }
    }

    private async Task BroadcastMessage(ChatMessage message, IColleague sender)
    {
        messageHistory.Add(message);

        Console.WriteLine($"[ChatRoom] Broadcasting message from {sender.Id}: {message.Content}");

        var tasks = participants
            .Where(p => p.Id != sender.Id)
            .Select(p => p.ReceiveAsync(message, sender));

        await Task.WhenAll(tasks);
    }

    private async Task SendPrivateMessage(PrivateMessage message, IColleague sender)
    {
        var target = participants.FirstOrDefault(p => p.Id == message.TargetId);

        if (target != null)
        {
            Console.WriteLine($"[ChatRoom] Private message from {sender.Id} to {message.TargetId}");
            await target.ReceiveAsync(message, sender);
        }
        else
        {
            var errorMsg = new SystemMessage($"User {message.TargetId} not found", DateTime.UtcNow);
            await sender.ReceiveAsync(errorMsg, sender);
        }
    }

    private async Task BroadcastSystemMessage(SystemMessage message)
    {
        Console.WriteLine($"[ChatRoom] System message: {message.Content}");

        var tasks = participants.Select(p =>
            p.ReceiveAsync(message, this as IColleague ?? participants.FirstOrDefault()!));
        await Task.WhenAll(tasks);
    }

    public void SetUserProfile(string userId, UserProfile profile)
    {
        userProfiles[userId] = profile;
    }

    public UserProfile? GetUserProfile(string userId)
    {
        return userProfiles.TryGetValue(userId, out var profile) ? profile : null;
    }
}