namespace Snippets.DesignPatterns.Structural.Bridge;

// Implementation interface - defines the interface for implementation classes

// Concrete Implementations
public class EmailSender(string smtpServer = "smtp.gmail.com", int port = 587) : IMessageSender
{
    public void SendMessage(string message, string recipient)
    {
        Console.WriteLine($"📧 Email sent via {smtpServer}:{port}");
        Console.WriteLine($"   To: {recipient}");
        Console.WriteLine($"   Message: {message}");
        Console.WriteLine($"   Status: ✅ Delivered to inbox");
    }

    public string GetSenderType() => "Email";
}

// Abstraction - defines the abstraction's interface and maintains a reference to implementor

// Refined Abstractions

// Advanced Bridge Pattern with Multiple Abstractions

// Bridge Pattern Factory