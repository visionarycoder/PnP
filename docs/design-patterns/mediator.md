# Mediator Pattern

**Description**: Defines how a set of objects interact with each other. The pattern promotes loose coupling by keeping objects from referring to each other explicitly, and letting you vary their interaction independently. A mediator object encapsulates the interaction logic between multiple objects.

**Language/Technology**: C#

**Code**:

## 1. Basic Mediator Structure

```csharp
// Mediator interface
public interface IMediator
{
    Task SendAsync(object message, IColleague sender);
    void RegisterColleague(IColleague colleague);
    void RemoveColleague(IColleague colleague);
}

// Colleague interface
public interface IColleague
{
    string Id { get; }
    IMediator? Mediator { get; set; }
    Task ReceiveAsync(object message, IColleague sender);
}

// Base colleague implementation
public abstract class BaseColleague : IColleague
{
    public string Id { get; }
    public IMediator? Mediator { get; set; }
    
    protected BaseColleague(string id)
    {
        Id = id;
    }
    
    public abstract Task ReceiveAsync(object message, IColleague sender);
    
    protected async Task SendAsync(object message)
    {
        if (Mediator != null)
        {
            await Mediator.SendAsync(message, this);
        }
    }
}
```

## 2. Chat Room Mediator Example

```csharp
// Message types
public record ChatMessage(string Content, string SenderId, DateTime Timestamp, string? TargetId = null);
public record UserJoinedMessage(string UserId, DateTime Timestamp);
public record UserLeftMessage(string UserId, DateTime Timestamp);
public record PrivateMessage(string Content, string SenderId, string TargetId, DateTime Timestamp);
public record SystemMessage(string Content, DateTime Timestamp);

// Chat room mediator
public class ChatRoomMediator : IMediator
{
    private readonly List<IColleague> _participants = new();
    private readonly List<ChatMessage> _messageHistory = new();
    private readonly Dictionary<string, UserProfile> _userProfiles = new();
    
    public IReadOnlyList<ChatMessage> MessageHistory => _messageHistory.AsReadOnly();
    public IReadOnlyList<IColleague> Participants => _participants.AsReadOnly();
    
    public void RegisterColleague(IColleague colleague)
    {
        _participants.Add(colleague);
        colleague.Mediator = this;
        
        // Notify all participants about new user
        var joinMessage = new UserJoinedMessage(colleague.Id, DateTime.UtcNow);
        _ = Task.Run(async () =>
        {
            foreach (var participant in _participants.Where(p => p.Id != colleague.Id))
            {
                await participant.ReceiveAsync(joinMessage, colleague);
            }
        });
        
        Console.WriteLine($"[ChatRoom] {colleague.Id} joined the chat room");
    }
    
    public void RemoveColleague(IColleague colleague)
    {
        _participants.Remove(colleague);
        colleague.Mediator = null;
        
        // Notify remaining participants
        var leaveMessage = new UserLeftMessage(colleague.Id, DateTime.UtcNow);
        _ = Task.Run(async () =>
        {
            foreach (var participant in _participants)
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
        _messageHistory.Add(message);
        
        Console.WriteLine($"[ChatRoom] Broadcasting message from {sender.Id}: {message.Content}");
        
        var tasks = _participants
            .Where(p => p.Id != sender.Id)
            .Select(p => p.ReceiveAsync(message, sender));
            
        await Task.WhenAll(tasks);
    }
    
    private async Task SendPrivateMessage(PrivateMessage message, IColleague sender)
    {
        var target = _participants.FirstOrDefault(p => p.Id == message.TargetId);
        
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
        
        var tasks = _participants.Select(p => p.ReceiveAsync(message, this as IColleague ?? _participants.First()));
        await Task.WhenAll(tasks);
    }
    
    public void SetUserProfile(string userId, UserProfile profile)
    {
        _userProfiles[userId] = profile;
    }
    
    public UserProfile? GetUserProfile(string userId)
    {
        return _userProfiles.TryGetValue(userId, out var profile) ? profile : null;
    }
}

public record UserProfile(string DisplayName, string Status, Dictionary<string, object> Metadata);

// Chat participants
public class ChatUser : BaseColleague
{
    public string DisplayName { get; }
    public bool IsOnline { get; private set; } = true;
    
    public ChatUser(string id, string displayName) : base(id)
    {
        DisplayName = displayName;
    }
    
    public async Task SendMessage(string content, string? targetId = null)
    {
        var message = targetId == null
            ? new ChatMessage(content, Id, DateTime.UtcNow)
            : new PrivateMessage(content, Id, targetId, DateTime.UtcNow);
            
        await SendAsync(message);
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

public class ChatBot : BaseColleague
{
    private readonly Dictionary<string, string> _responses = new()
    {
        ["help"] = "Available commands: help, time, weather, joke",
        ["time"] = $"Current time: {DateTime.Now:HH:mm:ss}",
        ["weather"] = "It's sunny with a chance of code! ☀️",
        ["joke"] = "Why do programmers prefer dark mode? Because light attracts bugs! 🐛"
    };
    
    public ChatBot() : base("ChatBot")
    {
    }
    
    public override async Task ReceiveAsync(object message, IColleague sender)
    {
        await Task.Delay(500); // Simulate processing time
        
        if (message is ChatMessage chatMsg && chatMsg.Content.StartsWith("/"))
        {
            var command = chatMsg.Content[1..].ToLower();
            
            if (_responses.TryGetValue(command, out var response))
            {
                var botResponse = new ChatMessage($"🤖 {response}", Id, DateTime.UtcNow);
                await SendAsync(botResponse);
            }
            else
            {
                var unknownResponse = new ChatMessage("🤖 Unknown command. Type '/help' for available commands.", Id, DateTime.UtcNow);
                await SendAsync(unknownResponse);
            }
        }
    }
}
```

## 3. UI Dialog Mediator Example

```csharp
// UI component messages
public record FieldChangedMessage(string FieldName, object? Value);
public record ValidationMessage(string FieldName, bool IsValid, string? ErrorMessage = null);
public record FormSubmitMessage(Dictionary<string, object?> FormData);
public record ButtonClickMessage(string ButtonName);

// Dialog mediator
public class DialogMediator : IMediator
{
    private readonly List<IColleague> _components = new();
    private readonly Dictionary<string, object?> _formData = new();
    private readonly Dictionary<string, string> _validationErrors = new();
    
    public void RegisterColleague(IColleague colleague)
    {
        _components.Add(colleague);
        colleague.Mediator = this;
        Console.WriteLine($"[Dialog] Registered component: {colleague.Id}");
    }
    
    public void RemoveColleague(IColleague colleague)
    {
        _components.Remove(colleague);
        colleague.Mediator = null;
        Console.WriteLine($"[Dialog] Removed component: {colleague.Id}");
    }
    
    public async Task SendAsync(object message, IColleague sender)
    {
        switch (message)
        {
            case FieldChangedMessage fieldMsg:
                await HandleFieldChanged(fieldMsg, sender);
                break;
                
            case ValidationMessage validationMsg:
                await HandleValidation(validationMsg, sender);
                break;
                
            case ButtonClickMessage buttonMsg:
                await HandleButtonClick(buttonMsg, sender);
                break;
                
            case FormSubmitMessage submitMsg:
                await HandleFormSubmit(submitMsg, sender);
                break;
        }
    }
    
    private async Task HandleFieldChanged(FieldChangedMessage message, IColleague sender)
    {
        _formData[message.FieldName] = message.Value;
        Console.WriteLine($"[Dialog] Field '{message.FieldName}' changed to: {message.Value}");
        
        // Trigger validation
        await ValidateField(message.FieldName, message.Value);
        
        // Notify other components that might depend on this field
        await NotifyDependentComponents(message, sender);
    }
    
    private async Task ValidateField(string fieldName, object? value)
    {
        var isValid = true;
        string? errorMessage = null;
        
        // Example validation logic
        switch (fieldName.ToLower())
        {
            case "email":
                var email = value?.ToString() ?? "";
                isValid = email.Contains("@") && email.Contains(".");
                errorMessage = isValid ? null : "Invalid email format";
                break;
                
            case "age":
                if (int.TryParse(value?.ToString(), out var age))
                {
                    isValid = age >= 18 && age <= 120;
                    errorMessage = isValid ? null : "Age must be between 18 and 120";
                }
                else
                {
                    isValid = false;
                    errorMessage = "Age must be a number";
                }
                break;
                
            case "password":
                var password = value?.ToString() ?? "";
                isValid = password.Length >= 8;
                errorMessage = isValid ? null : "Password must be at least 8 characters";
                break;
        }
        
        if (isValid)
        {
            _validationErrors.Remove(fieldName);
        }
        else
        {
            _validationErrors[fieldName] = errorMessage ?? "Invalid value";
        }
        
        var validationMessage = new ValidationMessage(fieldName, isValid, errorMessage);
        
        // Notify validation components
        var validationComponents = _components.OfType<ValidationDisplay>();
        foreach (var component in validationComponents)
        {
            await component.ReceiveAsync(validationMessage, this as IColleague ?? _components.First());
        }
        
        // Update submit button state
        await UpdateSubmitButtonState();
    }
    
    private async Task NotifyDependentComponents(FieldChangedMessage message, IColleague sender)
    {
        // Example: Update dependent dropdowns, enable/disable fields, etc.
        var dependentComponents = _components.Where(c => c.Id != sender.Id);
        
        foreach (var component in dependentComponents)
        {
            await component.ReceiveAsync(message, sender);
        }
    }
    
    private async Task HandleButtonClick(ButtonClickMessage message, IColleague sender)
    {
        Console.WriteLine($"[Dialog] Button '{message.ButtonName}' clicked");
        
        switch (message.ButtonName.ToLower())
        {
            case "submit":
                if (_validationErrors.Count == 0)
                {
                    var submitMessage = new FormSubmitMessage(new Dictionary<string, object?>(_formData));
                    await SendAsync(submitMessage, sender);
                }
                else
                {
                    Console.WriteLine("[Dialog] Cannot submit - validation errors exist");
                }
                break;
                
            case "reset":
                await ResetForm();
                break;
                
            case "cancel":
                await CancelDialog();
                break;
        }
    }
    
    private async Task HandleFormSubmit(FormSubmitMessage message, IColleague sender)
    {
        Console.WriteLine("[Dialog] Form submitted with data:");
        foreach (var kvp in message.FormData)
        {
            Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
        }
        
        // Notify all components about successful submission
        foreach (var component in _components)
        {
            await component.ReceiveAsync(message, sender);
        }
    }
    
    private async Task UpdateSubmitButtonState()
    {
        var hasErrors = _validationErrors.Count > 0;
        var submitButtons = _components.OfType<SubmitButton>();
        
        foreach (var button in submitButtons)
        {
            await button.ReceiveAsync(new ValidationMessage("form", !hasErrors), this as IColleague ?? _components.First());
        }
    }
    
    private async Task ResetForm()
    {
        _formData.Clear();
        _validationErrors.Clear();
        
        Console.WriteLine("[Dialog] Form reset");
        
        // Notify all components to reset
        foreach (var component in _components)
        {
            await component.ReceiveAsync("RESET", this as IColleague ?? _components.First());
        }
    }
    
    private async Task CancelDialog()
    {
        Console.WriteLine("[Dialog] Dialog cancelled");
        
        foreach (var component in _components)
        {
            await component.ReceiveAsync("CANCEL", this as IColleague ?? _components.First());
        }
    }
}

// UI Components
public class InputField : BaseColleague
{
    public string Label { get; }
    private object? _value;
    
    public InputField(string id, string label) : base(id)
    {
        Label = label;
    }
    
    public async Task SetValue(object? value)
    {
        _value = value;
        var message = new FieldChangedMessage(Id, value);
        await SendAsync(message);
    }
    
    public override async Task ReceiveAsync(object message, IColleague sender)
    {
        await Task.CompletedTask;
        
        switch (message)
        {
            case string str when str == "RESET":
                _value = null;
                Console.WriteLine($"[{Label} Field] Reset to empty");
                break;
                
            case FieldChangedMessage fieldMsg when fieldMsg.FieldName != Id:
                // React to other field changes if needed
                Console.WriteLine($"[{Label} Field] Other field changed: {fieldMsg.FieldName}");
                break;
        }
    }
    
    public object? GetValue() => _value;
}

public class ValidationDisplay : BaseColleague
{
    public ValidationDisplay() : base("ValidationDisplay")
    {
    }
    
    public override async Task ReceiveAsync(object message, IColleague sender)
    {
        await Task.CompletedTask;
        
        if (message is ValidationMessage validationMsg)
        {
            if (validationMsg.IsValid)
            {
                Console.WriteLine($"[Validation] ✓ {validationMsg.FieldName} is valid");
            }
            else
            {
                Console.WriteLine($"[Validation] ✗ {validationMsg.FieldName}: {validationMsg.ErrorMessage}");
            }
        }
    }
}

public class SubmitButton : BaseColleague
{
    private bool _isEnabled = false;
    
    public SubmitButton() : base("SubmitButton")
    {
    }
    
    public async Task Click()
    {
        if (_isEnabled)
        {
            var message = new ButtonClickMessage("submit");
            await SendAsync(message);
        }
        else
        {
            Console.WriteLine("[Submit Button] Button is disabled");
        }
    }
    
    public override async Task ReceiveAsync(object message, IColleague sender)
    {
        await Task.CompletedTask;
        
        switch (message)
        {
            case ValidationMessage validationMsg when validationMsg.FieldName == "form":
                _isEnabled = validationMsg.IsValid;
                Console.WriteLine($"[Submit Button] {'Enabled' : 'Disabled'}");
                break;
                
            case FormSubmitMessage:
                Console.WriteLine("[Submit Button] Form submitted successfully!");
                break;
                
            case string str when str == "RESET":
                _isEnabled = false;
                Console.WriteLine("[Submit Button] Reset to disabled");
                break;
        }
    }
}
```

## 4. Advanced Mediator Features

```csharp
// Generic typed mediator
public interface ITypedMediator
{
    Task PublishAsync<T>(T message) where T : class;
    void Subscribe<T>(IMessageHandler<T> handler) where T : class;
    void Unsubscribe<T>(IMessageHandler<T> handler) where T : class;
}

public interface IMessageHandler<T> where T : class
{
    Task HandleAsync(T message);
}

public class TypedMediator : ITypedMediator
{
    private readonly Dictionary<Type, List<object>> _handlers = new();
    
    public void Subscribe<T>(IMessageHandler<T> handler) where T : class
    {
        var messageType = typeof(T);
        
        if (!_handlers.ContainsKey(messageType))
        {
            _handlers[messageType] = new List<object>();
        }
        
        _handlers[messageType].Add(handler);
        Console.WriteLine($"[TypedMediator] Subscribed handler for {messageType.Name}");
    }
    
    public void Unsubscribe<T>(IMessageHandler<T> handler) where T : class
    {
        var messageType = typeof(T);
        
        if (_handlers.TryGetValue(messageType, out var handlers))
        {
            handlers.Remove(handler);
            Console.WriteLine($"[TypedMediator] Unsubscribed handler for {messageType.Name}");
        }
    }
    
    public async Task PublishAsync<T>(T message) where T : class
    {
        var messageType = typeof(T);
        
        if (_handlers.TryGetValue(messageType, out var handlers))
        {
            Console.WriteLine($"[TypedMediator] Publishing {messageType.Name} to {handlers.Count} handlers");
            
            var tasks = handlers
                .Cast<IMessageHandler<T>>()
                .Select(handler => handler.HandleAsync(message));
                
            await Task.WhenAll(tasks);
        }
        else
        {
            Console.WriteLine($"[TypedMediator] No handlers found for {messageType.Name}");
        }
    }
}

// Request-Response Mediator
public interface IRequestResponseMediator
{
    Task<TResponse> SendAsync<TRequest, TResponse>(TRequest request) 
        where TRequest : class 
        where TResponse : class;
    void RegisterHandler<TRequest, TResponse>(IRequestHandler<TRequest, TResponse> handler)
        where TRequest : class 
        where TResponse : class;
}

public interface IRequestHandler<TRequest, TResponse>
    where TRequest : class
    where TResponse : class
{
    Task<TResponse> HandleAsync(TRequest request);
}

public class RequestResponseMediator : IRequestResponseMediator
{
    private readonly Dictionary<Type, object> _handlers = new();
    
    public void RegisterHandler<TRequest, TResponse>(IRequestHandler<TRequest, TResponse> handler)
        where TRequest : class
        where TResponse : class
    {
        var requestType = typeof(TRequest);
        _handlers[requestType] = handler;
        Console.WriteLine($"[RequestResponseMediator] Registered handler for {requestType.Name}");
    }
    
    public async Task<TResponse> SendAsync<TRequest, TResponse>(TRequest request)
        where TRequest : class
        where TResponse : class
    {
        var requestType = typeof(TRequest);
        
        if (_handlers.TryGetValue(requestType, out var handlerObj) && 
            handlerObj is IRequestHandler<TRequest, TResponse> handler)
        {
            Console.WriteLine($"[RequestResponseMediator] Handling {requestType.Name}");
            return await handler.HandleAsync(request);
        }
        
        throw new InvalidOperationException($"No handler registered for {requestType.Name}");
    }
}

// Priority-based mediator
public class PriorityMessage
{
    public object Content { get; }
    public int Priority { get; }
    public DateTime Timestamp { get; }
    
    public PriorityMessage(object content, int priority = 0)
    {
        Content = content;
        Priority = priority;
        Timestamp = DateTime.UtcNow;
    }
}

public class PriorityMediator : IMediator
{
    private readonly PriorityQueue<PriorityMessage, int> _messageQueue = new();
    private readonly List<IColleague> _colleagues = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly Task _processingTask;
    
    public PriorityMediator()
    {
        _processingTask = Task.Run(ProcessMessages, _cancellationTokenSource.Token);
    }
    
    public void RegisterColleague(IColleague colleague)
    {
        _colleagues.Add(colleague);
        colleague.Mediator = this;
    }
    
    public void RemoveColleague(IColleague colleague)
    {
        _colleagues.Remove(colleague);
        colleague.Mediator = null;
    }
    
    public async Task SendAsync(object message, IColleague sender)
    {
        var priority = message is PriorityMessage pm ? pm.Priority : 0;
        var priorityMessage = message is PriorityMessage ? (PriorityMessage)message : new PriorityMessage(message, priority);
        
        _messageQueue.Enqueue(priorityMessage, -priorityMessage.Priority); // Negative for highest priority first
        await Task.CompletedTask;
    }
    
    private async Task ProcessMessages()
    {
        while (!_cancellationTokenSource.Token.IsCancellationRequested)
        {
            if (_messageQueue.TryDequeue(out var priorityMessage, out _))
            {
                Console.WriteLine($"[PriorityMediator] Processing priority {priorityMessage.Priority} message");
                
                var tasks = _colleagues.Select(c => c.ReceiveAsync(priorityMessage.Content, _colleagues.First()));
                await Task.WhenAll(tasks);
            }
            else
            {
                await Task.Delay(10, _cancellationTokenSource.Token);
            }
        }
    }
    
    public void Stop()
    {
        _cancellationTokenSource.Cancel();
        _processingTask.Wait(5000);
    }
}
```

**Usage**:

```csharp
// 1. Chat Room Example
var chatRoom = new ChatRoomMediator();

var alice = new ChatUser("alice", "Alice");
var bob = new ChatUser("bob", "Bob");
var charlie = new ChatUser("charlie", "Charlie");
var bot = new ChatBot();

// Register users
chatRoom.RegisterColleague(alice);
chatRoom.RegisterColleague(bob);
chatRoom.RegisterColleague(charlie);
chatRoom.RegisterColleague(bot);

// Send messages
await alice.SendMessage("Hello everyone!");
await bob.SendMessage("Hi Alice!", "alice"); // Private message
await charlie.SendMessage("/help"); // Bot command
await charlie.SendMessage("/time");

// User leaves
chatRoom.RemoveColleague(bob);

// 2. Dialog Form Example
var dialogMediator = new DialogMediator();

var emailField = new InputField("email", "Email");
var ageField = new InputField("age", "Age");
var passwordField = new InputField("password", "Password");
var validation = new ValidationDisplay();
var submitButton = new SubmitButton();

// Register components
dialogMediator.RegisterColleague(emailField);
dialogMediator.RegisterColleague(ageField);
dialogMediator.RegisterColleague(passwordField);
dialogMediator.RegisterColleague(validation);
dialogMediator.RegisterColleague(submitButton);

// Simulate user input
await emailField.SetValue("user@example.com");
await ageField.SetValue("25");
await passwordField.SetValue("password123");

// Try to submit
await submitButton.Click();

// 3. Typed Mediator Example
var typedMediator = new TypedMediator();

// Subscribe handlers
typedMediator.Subscribe<ChatMessage>(new ChatMessageHandler());
typedMediator.Subscribe<SystemMessage>(new SystemMessageHandler());

// Publish messages
await typedMediator.PublishAsync(new ChatMessage("Hello typed world!", "user1", DateTime.UtcNow));
await typedMediator.PublishAsync(new SystemMessage("System notification", DateTime.UtcNow));

// Expected output demonstrates:
// - Decoupled communication between chat participants
// - Form validation and submission workflow
// - UI component coordination through mediator
// - Message broadcasting and private messaging
// - Bot integration and command processing
// - Priority-based message processing
// - Type-safe message handling
```

**Notes**:

- **Decoupling**: Objects don't reference each other directly, only the mediator
- **Centralized Control**: All interaction logic is contained in the mediator
- **Reusability**: Colleagues can be reused with different mediators
- **Complexity**: Can become a "god object" if it handles too many responsibilities
- **Single Point of Failure**: The mediator becomes critical for system operation
- **Performance**: All communication goes through the mediator, which can create bottlenecks
- **Testing**: Easier to test individual components in isolation
- **Maintainability**: Changes to interaction logic are centralized in the mediator

**Prerequisites**:

- .NET 6.0 or later
- Understanding of async/await patterns
- Knowledge of interfaces and polymorphism
- Familiarity with event-driven programming concepts

**Related Patterns**:

- **Observer**: Mediator can use Observer pattern to notify participants
- **Command**: Messages sent through mediator can be Command objects
- **Facade**: Both provide simplified interfaces, but Mediator focuses on communication
- **Singleton**: Mediators are often implemented as singletons

