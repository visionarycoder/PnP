# Bridge Pattern

**Description**: Separates an abstraction from its implementation so that both can vary independently. Useful when you want to share implementation among multiple objects or when both abstraction and implementation need to be extended through subclassing.

**Language/Technology**: C# / .NET

**Code**:

```csharp
using System;
using System.Collections.Generic;

// Implementation interface - defines the interface for implementation classes
public interface IMessageSender
{
    void SendMessage(string message, string recipient);
    string GetSenderType();
}

// Concrete Implementations
public class EmailSender : IMessageSender
{
    private readonly string _smtpServer;
    private readonly int _port;
    
    public EmailSender(string smtpServer = "smtp.gmail.com", int port = 587)
    {
        _smtpServer = smtpServer;
        _port = port;
    }
    
    public void SendMessage(string message, string recipient)
    {
        Console.WriteLine($"📧 Email sent via {_smtpServer}:{_port}");
        Console.WriteLine($"   To: {recipient}");
        Console.WriteLine($"   Message: {message}");
        Console.WriteLine($"   Status: ✅ Delivered to inbox");
    }
    
    public string GetSenderType() => "Email";
}

public class SmsSender : IMessageSender
{
    private readonly string _apiKey;
    private readonly string _provider;
    
    public SmsSender(string provider = "Twilio", string apiKey = "API_KEY_123")
    {
        _provider = provider;
        _apiKey = apiKey;
    }
    
    public void SendMessage(string message, string recipient)
    {
        Console.WriteLine($"📱 SMS sent via {_provider}");
        Console.WriteLine($"   To: {recipient}");
        Console.WriteLine($"   Message: {message}");
        Console.WriteLine($"   Status: ✅ Delivered to mobile device");
    }
    
    public string GetSenderType() => "SMS";
}

public class SlackSender : IMessageSender
{
    private readonly string _workspace;
    private readonly string _botToken;
    
    public SlackSender(string workspace = "MyCompany", string botToken = "xoxb-token-123")
    {
        _workspace = workspace;
        _botToken = botToken;
    }
    
    public void SendMessage(string message, string recipient)
    {
        Console.WriteLine($"💬 Slack message sent to {_workspace}");
        Console.WriteLine($"   Channel/User: {recipient}");
        Console.WriteLine($"   Message: {message}");
        Console.WriteLine($"   Status: ✅ Posted to Slack");
    }
    
    public string GetSenderType() => "Slack";
}

public class PushNotificationSender : IMessageSender
{
    private readonly string _platform;
    
    public PushNotificationSender(string platform = "Firebase")
    {
        _platform = platform;
    }
    
    public void SendMessage(string message, string recipient)
    {
        Console.WriteLine($"🔔 Push notification sent via {_platform}");
        Console.WriteLine($"   Device ID: {recipient}");
        Console.WriteLine($"   Message: {message}");
        Console.WriteLine($"   Status: ✅ Pushed to device");
    }
    
    public string GetSenderType() => "Push Notification";
}

// Abstraction - defines the abstraction's interface and maintains a reference to implementor
public abstract class Message
{
    protected IMessageSender _messageSender;
    
    protected Message(IMessageSender messageSender)
    {
        _messageSender = messageSender ?? throw new ArgumentNullException(nameof(messageSender));
    }
    
    public abstract void Send(string recipient);
    
    public virtual void SetSender(IMessageSender messageSender)
    {
        _messageSender = messageSender ?? throw new ArgumentNullException(nameof(messageSender));
    }
    
    public string GetSenderType() => _messageSender.GetSenderType();
}

// Refined Abstractions
public class TextMessage : Message
{
    private readonly string _content;
    
    public TextMessage(string content, IMessageSender messageSender) : base(messageSender)
    {
        _content = content ?? throw new ArgumentNullException(nameof(content));
    }
    
    public override void Send(string recipient)
    {
        Console.WriteLine($"\n📝 Sending Text Message:");
        _messageSender.SendMessage(_content, recipient);
    }
}

public class AlertMessage : Message
{
    private readonly string _alertType;
    private readonly string _content;
    private readonly DateTime _timestamp;
    
    public AlertMessage(string alertType, string content, IMessageSender messageSender) : base(messageSender)
    {
        _alertType = alertType ?? throw new ArgumentNullException(nameof(alertType));
        _content = content ?? throw new ArgumentNullException(nameof(content));
        _timestamp = DateTime.Now;
    }
    
    public override void Send(string recipient)
    {
        Console.WriteLine($"\n🚨 Sending {_alertType} Alert:");
        var alertMessage = $"[{_alertType.ToUpper()}] {_content} (Sent: {_timestamp:yyyy-MM-dd HH:mm:ss})";
        _messageSender.SendMessage(alertMessage, recipient);
    }
}

public class RichMessage : Message
{
    private readonly string _title;
    private readonly string _content;
    private readonly string _imageUrl;
    private readonly Dictionary<string, string> _metadata;
    
    public RichMessage(string title, string content, IMessageSender messageSender, 
                      string imageUrl = null) : base(messageSender)
    {
        _title = title ?? throw new ArgumentNullException(nameof(title));
        _content = content ?? throw new ArgumentNullException(nameof(content));
        _imageUrl = imageUrl;
        _metadata = new Dictionary<string, string>();
    }
    
    public void AddMetadata(string key, string value)
    {
        _metadata[key] = value;
    }
    
    public override void Send(string recipient)
    {
        Console.WriteLine($"\n🎨 Sending Rich Message:");
        var richContent = $"Title: {_title}\nContent: {_content}";
        
        if (!string.IsNullOrEmpty(_imageUrl))
        {
            richContent += $"\nImage: {_imageUrl}";
        }
        
        if (_metadata.Count > 0)
        {
            richContent += "\nMetadata:";
            foreach (var kvp in _metadata)
            {
                richContent += $"\n  {kvp.Key}: {kvp.Value}";
            }
        }
        
        _messageSender.SendMessage(richContent, recipient);
    }
}

// Advanced Bridge Pattern with Multiple Abstractions
public abstract class NotificationChannel
{
    protected IMessageSender _sender;
    protected List<string> _recipients;
    
    protected NotificationChannel(IMessageSender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        _recipients = new List<string>();
    }
    
    public virtual void AddRecipient(string recipient)
    {
        if (!_recipients.Contains(recipient))
        {
            _recipients.Add(recipient);
        }
    }
    
    public virtual void RemoveRecipient(string recipient)
    {
        _recipients.Remove(recipient);
    }
    
    public abstract void Broadcast(string message);
    
    public void ChangeSender(IMessageSender newSender)
    {
        _sender = newSender ?? throw new ArgumentNullException(nameof(newSender));
        Console.WriteLine($"📡 Channel sender changed to: {_sender.GetSenderType()}");
    }
}

public class EmergencyNotificationChannel : NotificationChannel
{
    private readonly string _emergencyPrefix;
    
    public EmergencyNotificationChannel(IMessageSender sender, string emergencyPrefix = "[EMERGENCY]") 
        : base(sender)
    {
        _emergencyPrefix = emergencyPrefix;
    }
    
    public override void Broadcast(string message)
    {
        var emergencyMessage = $"{_emergencyPrefix} {message}";
        Console.WriteLine($"\n🆘 Emergency Broadcast to {_recipients.Count} recipients:");
        
        foreach (var recipient in _recipients)
        {
            _sender.SendMessage(emergencyMessage, recipient);
        }
    }
}

public class MarketingNotificationChannel : NotificationChannel
{
    private readonly string _campaignId;
    
    public MarketingNotificationChannel(IMessageSender sender, string campaignId) 
        : base(sender)
    {
        _campaignId = campaignId ?? "CAMPAIGN_001";
    }
    
    public override void Broadcast(string message)
    {
        var marketingMessage = $"{message}\n\nCampaign ID: {_campaignId}\nUnsubscribe: reply STOP";
        Console.WriteLine($"\n📢 Marketing Broadcast to {_recipients.Count} recipients:");
        
        foreach (var recipient in _recipients)
        {
            _sender.SendMessage(marketingMessage, recipient);
        }
    }
}

// Bridge Pattern Factory
public static class MessageBridgeFactory
{
    public static Message CreateMessage(string messageType, string content, string senderType, 
                                      Dictionary<string, string> parameters = null)
    {
        var sender = CreateSender(senderType, parameters);
        
        return messageType.ToLower() switch
        {
            "text" => new TextMessage(content, sender),
            "alert" => new AlertMessage(parameters?.GetValueOrDefault("alertType", "INFO") ?? "INFO", content, sender),
            "rich" => CreateRichMessage(content, sender, parameters),
            _ => throw new ArgumentException($"Unknown message type: {messageType}")
        };
    }
    
    private static IMessageSender CreateSender(string senderType, Dictionary<string, string> parameters)
    {
        return senderType.ToLower() switch
        {
            "email" => new EmailSender(
                parameters?.GetValueOrDefault("smtpServer", "smtp.gmail.com"),
                int.Parse(parameters?.GetValueOrDefault("port", "587") ?? "587")),
            "sms" => new SmsSender(
                parameters?.GetValueOrDefault("provider", "Twilio"),
                parameters?.GetValueOrDefault("apiKey", "API_KEY_123")),
            "slack" => new SlackSender(
                parameters?.GetValueOrDefault("workspace", "MyCompany"),
                parameters?.GetValueOrDefault("botToken", "xoxb-token-123")),
            "push" => new PushNotificationSender(
                parameters?.GetValueOrDefault("platform", "Firebase")),
            _ => throw new ArgumentException($"Unknown sender type: {senderType}")
        };
    }
    
    private static RichMessage CreateRichMessage(string content, IMessageSender sender, 
                                               Dictionary<string, string> parameters)
    {
        var title = parameters?.GetValueOrDefault("title", "Rich Message") ?? "Rich Message";
        var imageUrl = parameters?.GetValueOrDefault("imageUrl");
        var richMessage = new RichMessage(title, content, sender, imageUrl);
        
        if (parameters != null)
        {
            foreach (var kvp in parameters.Where(p => p.Key.StartsWith("meta_")))
            {
                richMessage.AddMetadata(kvp.Key.Substring(5), kvp.Value);
            }
        }
        
        return richMessage;
    }
}
```

**Usage**:

```csharp
using System;
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        Console.WriteLine("=== Basic Bridge Pattern ===");
        
        // Create different senders (implementations)
        var emailSender = new EmailSender();
        var smsSender = new SmsSender();
        var slackSender = new SlackSender();
        var pushSender = new PushNotificationSender();
        
        // Create messages (abstractions) with different senders
        var textMessage = new TextMessage("Hello, this is a simple text message!", emailSender);
        textMessage.Send("user@example.com");
        
        // Same message, different implementation
        textMessage.SetSender(smsSender);
        textMessage.Send("+1234567890");
        
        // Alert messages
        var alertMessage = new AlertMessage("CRITICAL", "Server is down!", slackSender);
        alertMessage.Send("#alerts");
        
        // Switch implementation at runtime
        alertMessage.SetSender(pushSender);
        alertMessage.Send("device_id_123");
        
        Console.WriteLine("\n=== Rich Message Bridge ===");
        
        var richMessage = new RichMessage("Product Launch", "Exciting news about our new product!", emailSender);
        richMessage.AddMetadata("Category", "Marketing");
        richMessage.AddMetadata("Priority", "High");
        richMessage.Send("marketing@company.com");
        
        Console.WriteLine("\n=== Notification Channels ===");
        
        // Emergency notification channel
        var emergencyChannel = new EmergencyNotificationChannel(emailSender);
        emergencyChannel.AddRecipient("admin@company.com");
        emergencyChannel.AddRecipient("security@company.com");
        emergencyChannel.AddRecipient("management@company.com");
        
        emergencyChannel.Broadcast("Security breach detected in system!");
        
        // Change the implementation at runtime
        emergencyChannel.ChangeSender(smsSender);
        emergencyChannel.AddRecipient("+1-911-ADMIN");
        emergencyChannel.AddRecipient("+1-911-SECURITY");
        emergencyChannel.Broadcast("Immediate action required!");
        
        // Marketing channel
        var marketingChannel = new MarketingNotificationChannel(emailSender, "SUMMER_SALE_2025");
        marketingChannel.AddRecipient("customer1@email.com");
        marketingChannel.AddRecipient("customer2@email.com");
        marketingChannel.AddRecipient("customer3@email.com");
        
        marketingChannel.Broadcast("Summer Sale: 50% off all products!");
        
        Console.WriteLine("\n=== Bridge Factory Pattern ===");
        
        var parameters = new Dictionary<string, string>
        {
            {"alertType", "WARNING"},
            {"smtpServer", "smtp.company.com"},
            {"port", "465"}
        };
        
        var factoryMessage = MessageBridgeFactory.CreateMessage("alert", 
            "Database connection timeout", "email", parameters);
        factoryMessage.Send("devops@company.com");
        
        var richParameters = new Dictionary<string, string>
        {
            {"title", "System Status Update"},
            {"imageUrl", "https://status.company.com/chart.png"},
            {"meta_source", "Monitoring System"},
            {"meta_severity", "Medium"}
        };
        
        var factoryRichMessage = MessageBridgeFactory.CreateMessage("rich", 
            "All systems are operating normally", "slack", richParameters);
        factoryRichMessage.Send("#system-status");
        
        Console.WriteLine("\n=== Multiple Implementations Demo ===");
        
        var implementations = new IMessageSender[] { emailSender, smsSender, slackSender, pushSender };
        var message = new TextMessage("Multi-platform notification test", emailSender);
        
        foreach (var implementation in implementations)
        {
            message.SetSender(implementation);
            var recipient = implementation.GetSenderType().ToLower() switch
            {
                "email" => "test@example.com",
                "sms" => "+1234567890",
                "slack" => "#general",
                "push notification" => "device_123",
                _ => "unknown"
            };
            
            Console.WriteLine($"\n--- Using {implementation.GetSenderType()} Implementation ---");
            message.Send(recipient);
        }
        
        Console.WriteLine("\n=== Bridge Pattern Benefits ===");
        Console.WriteLine("✅ Abstraction and implementation can vary independently");
        Console.WriteLine("✅ Runtime implementation switching");
        Console.WriteLine("✅ Easy to add new message types without changing senders");
        Console.WriteLine("✅ Easy to add new senders without changing message types");
        Console.WriteLine("✅ Hides implementation details from clients");
    }
}
```

**Notes**:

- Separates abstraction (Message types) from implementation (Sender types)
- Both sides can be extended independently without affecting each other
- Runtime switching of implementations provides flexibility
- Reduces coupling between abstraction and implementation
- Factory pattern can be combined for easier object creation
- Useful for cross-platform applications (different implementations per platform)
- Different from Adapter pattern - Bridge is designed upfront, Adapter fixes incompatibility
- Can be used with multiple levels of abstraction and implementation
- Related patterns: [Adapter](adapter.md), [Strategy](strategy.md), [Abstract Factory](abstract-factory.md)

**Prerequisites**:

- .NET Framework 2.0+ or .NET Core
- Understanding of interfaces and inheritance
- Knowledge of composition over inheritance principle

**References**:

- Gang of Four Design Patterns book
- [Microsoft Docs: Bridge Pattern](https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/bridge)

---

*Created: 2025-10-31*  
*Last Updated: 2025-10-31*  
*Tags: #bridge #structural #abstraction #implementation*
