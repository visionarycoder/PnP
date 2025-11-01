namespace Snippets.DesignPatterns.Behavioral.Mediator;

public class DialogMediator : IMediator
{
    private readonly List<IColleague> components = [];
    private readonly Dictionary<string, object?> formData = new();
    private readonly Dictionary<string, string> validationErrors = new();

    public IReadOnlyDictionary<string, object?> FormData => formData.AsReadOnly();
    public IReadOnlyDictionary<string, string> ValidationErrors => validationErrors.AsReadOnly();
    public bool IsFormValid => validationErrors.Count == 0;

    public void RegisterColleague(IColleague colleague)
    {
        components.Add(colleague);
        colleague.Mediator = this;
        Console.WriteLine($"[Dialog] Registered component: {colleague.Id}");
    }

    public void RemoveColleague(IColleague colleague)
    {
        components.Remove(colleague);
        colleague.Mediator = null;
    }

    public async Task SendAsync(object message, IColleague sender)
    {
        switch (message)
        {
            case FieldChangedMessage fieldMsg:
                await HandleFieldChanged(fieldMsg, sender);
                break;

            case ValidationMessage validationMsg:
                await HandleValidation(validationMsg);
                break;

            case FormSubmitMessage submitMsg:
                await HandleFormSubmit(submitMsg, sender);
                break;

            case ButtonClickMessage buttonMsg:
                await HandleButtonClick(buttonMsg, sender);
                break;
        }
    }

    private async Task HandleFieldChanged(FieldChangedMessage message, IColleague sender)
    {
        formData[message.FieldName] = message.Value;
        Console.WriteLine($"[Dialog] Field '{message.FieldName}' changed to: {message.Value}");

        // Notify other components about the change
        var tasks = components
            .Where(c => c.Id != sender.Id)
            .Select(c => c.ReceiveAsync(message, sender));

        await Task.WhenAll(tasks);

        // Trigger validation
        await ValidateField(message.FieldName, message.Value);
    }

    private async Task ValidateField(string fieldName, object? value)
    {
        bool isValid = true;
        string? errorMessage = null;

        // Simple validation rules
        switch (fieldName.ToLower())
        {
            case "email":
                var email = value?.ToString() ?? "";
                isValid = !string.IsNullOrEmpty(email) && email.Contains('@');
                errorMessage = isValid ? null : "Please enter a valid email address";
                break;

            case "password":
                var password = value?.ToString() ?? "";
                isValid = !string.IsNullOrEmpty(password) && password.Length >= 8;
                errorMessage = isValid ? null : "Password must be at least 8 characters";
                break;

            case "age":
                if (value != null && int.TryParse(value.ToString(), out int age))
                {
                    isValid = age >= 18 && age <= 120;
                    errorMessage = isValid ? null : "Age must be between 18 and 120";
                }
                else
                {
                    isValid = false;
                    errorMessage = "Please enter a valid age";
                }

                break;
        }

        var validationMessage = new ValidationMessage(fieldName, isValid, errorMessage);

        if (isValid)
        {
            validationErrors.Remove(fieldName);
        }
        else
        {
            validationErrors[fieldName] = errorMessage!;
        }

        // Notify all components about validation result
        var tasks =
            components.Select(c => c.ReceiveAsync(validationMessage, this as IColleague ?? components.First()));
        await Task.WhenAll(tasks);
    }

    private async Task HandleValidation(ValidationMessage message)
    {
        Console.WriteLine(
            $"[Dialog] Validation for '{message.FieldName}': {(message.IsValid ? "VALID" : $"INVALID - {message.ErrorMessage}")}");

        // Update submit button state based on overall form validity
        var submitButton = components.FirstOrDefault(c => c.Id == "SubmitButton");
        if (submitButton != null)
        {
            var buttonStateMessage = new ButtonClickMessage(IsFormValid ? "Enable" : "Disable");
            await submitButton.ReceiveAsync(buttonStateMessage, this as IColleague ?? components.First());
        }
    }

    private async Task HandleFormSubmit(FormSubmitMessage message, IColleague sender)
    {
        if (IsFormValid)
        {
            Console.WriteLine("[Dialog] Form submitted successfully!");
            Console.WriteLine(
                $"[Dialog] Form data: {string.Join(", ", formData.Select(kvp => $"{kvp.Key}={kvp.Value}"))}");

            // Notify components about successful submission
            var successMsg = new SystemMessage("Form submitted successfully", DateTime.UtcNow);
            var tasks = components.Select(c => c.ReceiveAsync(successMsg, sender));
            await Task.WhenAll(tasks);
        }
        else
        {
            Console.WriteLine("[Dialog] Form submission failed - validation errors exist");
            var errorMsg =
                new SystemMessage($"Please fix validation errors: {string.Join(", ", validationErrors.Keys)}",
                    DateTime.UtcNow);
            await sender.ReceiveAsync(errorMsg, sender);
        }
    }

    private async Task HandleButtonClick(ButtonClickMessage message, IColleague sender)
    {
        Console.WriteLine($"[Dialog] Button '{message.ButtonName}' clicked by {sender.Id}");

        if (message.ButtonName == "Reset")
        {
            formData.Clear();
            validationErrors.Clear();

            // Notify all components about reset
            var resetMsg = new SystemMessage("Form reset", DateTime.UtcNow);
            var tasks = components.Select(c => c.ReceiveAsync(resetMsg, sender));
            await Task.WhenAll(tasks);
        }
    }
}