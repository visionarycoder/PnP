namespace Snippets.DesignPatterns.Behavioral.Mediator;

public class InputField(string fieldName) : BaseColleague(fieldName)
{
    private object? value;

    public async Task SetValue(object? value)
    {
        this.value = value;
        var message = new FieldChangedMessage(Id, value);
        await SendAsync(message);
    }

    public object? GetValue() => value;

    public override async Task ReceiveAsync(object message, IColleague sender)
    {
        await Task.CompletedTask;

        switch (message)
        {
            case ValidationMessage validationMsg when validationMsg.FieldName == Id:
                if (validationMsg.IsValid)
                {
                    Console.WriteLine($"[{Id}] ✅ Field is valid");
                }
                else
                {
                    Console.WriteLine($"[{Id}] ❌ {validationMsg.ErrorMessage}");
                }

                break;

            case SystemMessage sysMsg:
                if (sysMsg.Content == "Form reset")
                {
                    value = null;
                    Console.WriteLine($"[{Id}] Field cleared");
                }
                else
                {
                    Console.WriteLine($"[{Id}] {sysMsg.Content}");
                }

                break;
        }
    }
}