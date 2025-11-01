namespace Snippets.DesignPatterns.Behavioral.Mediator;

public class ChatBot() : BaseColleague("ChatBot")
{
    private readonly Dictionary<string, string> responses = new()
    {
        ["help"] = "Available commands: help, time, weather, joke",
        ["time"] = $"Current time: {DateTime.Now:HH:mm:ss}",
        ["weather"] = "It's sunny with a chance of code! ☀️",
        ["joke"] = "Why do programmers prefer dark mode? Because light attracts bugs! 🐛"
    };

    public override async Task ReceiveAsync(object message, IColleague sender)
    {
        await Task.Delay(100); // Simulate processing time

        if (message is ChatMessage chatMsg && chatMsg.Content.StartsWith("/"))
        {
            var command = chatMsg.Content[1..].ToLower();

            if (responses.TryGetValue(command, out var response))
            {
                var botResponse = new ChatMessage($"🤖 {response}", Id, DateTime.UtcNow);
                await SendAsync(botResponse);
            }
            else
            {
                var unknownResponse = new ChatMessage("🤖 Unknown command. Type '/help' for available commands.", Id,
                    DateTime.UtcNow);
                await SendAsync(unknownResponse);
            }
        }
    }
}