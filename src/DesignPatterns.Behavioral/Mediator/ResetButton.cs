namespace Snippets.DesignPatterns.Behavioral.Mediator;

public class ResetButton() : BaseColleague("ResetButton")
{
    public async Task Click()
    {
        var resetMessage = new ButtonClickMessage("Reset");
        await SendAsync(resetMessage);
    }

    public override async Task ReceiveAsync(object message, IColleague sender)
    {
        await Task.CompletedTask;

        switch (message)
        {
            case SystemMessage sysMsg:
                Console.WriteLine($"[ResetButton] {sysMsg.Content}");
                break;
        }
    }
}