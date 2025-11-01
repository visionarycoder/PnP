namespace Snippets.DesignPatterns.Behavioral.Mediator;

public class SubmitButton() : BaseColleague("SubmitButton")
{
    private bool isEnabled = false;

    public async Task Click()
    {
        if (isEnabled)
        {
            var formData = new Dictionary<string, object?>();
            var submitMessage = new FormSubmitMessage(formData);
            await SendAsync(submitMessage);
        }
        else
        {
            Console.WriteLine("[SubmitButton] Button is disabled - cannot submit");
        }
    }

    public override async Task ReceiveAsync(object message, IColleague sender)
    {
        await Task.CompletedTask;

        switch (message)
        {
            case ButtonClickMessage buttonMsg:
                if (buttonMsg.ButtonName == "Enable")
                {
                    isEnabled = true;
                    Console.WriteLine("[SubmitButton] Button enabled ✅");
                }
                else if (buttonMsg.ButtonName == "Disable")
                {
                    isEnabled = false;
                    Console.WriteLine("[SubmitButton] Button disabled ❌");
                }

                break;

            case SystemMessage sysMsg:
                Console.WriteLine($"[SubmitButton] {sysMsg.Content}");
                break;
        }
    }
}