# Facade Pattern

**Description**: Provides a unified interface to a set of interfaces in a subsystem. Facade defines a higher-level interface that makes the subsystem easier to use by hiding its complexity. Useful for simplifying complex APIs, integrating multiple services, or providing a clean interface to legacy systems.

**Language/Technology**: C# / .NET

**Code**:

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text;

// Complex subsystem classes - these would typically be in separate assemblies or services

// Email Service Subsystem
public class SmtpClient
{
    private readonly string server;
    private readonly int port;
    private readonly bool useSsl;
    
    public SmtpClient(string server, int port = 587, bool useSsl = true)
    {server = server;port = port;useSsl = useSsl;
    }
    
    public void Connect()
    {
        Console.WriteLine($"📡 Connecting to SMTP server {server}:{port} (SSL: {useSsl})");
        // Simulate connection logic
        System.Threading.Thread.Sleep(100);
        Console.WriteLine("✅ SMTP connection established");
    }
    
    public void Authenticate(string username, string password)
    {
        Console.WriteLine($"🔐 Authenticating user: {username}");
        // Simulate authentication
        System.Threading.Thread.Sleep(50);
        Console.WriteLine("✅ Authentication successful");
    }
    
    public void SendMessage(string from, string to, string subject, string body, bool isHtml = false)
    {
        Console.WriteLine($"📤 Sending email from {from} to {to}");
        Console.WriteLine($"   Subject: {subject}");
        Console.WriteLine($"   Format: {(isHtml ? "HTML" : "Text")}");
        // Simulate sending
        System.Threading.Thread.Sleep(200);
        Console.WriteLine("✅ Email sent successfully");
    }
    
    public void Disconnect()
    {
        Console.WriteLine("📡 Disconnecting from SMTP server");
        Console.WriteLine("✅ Disconnected");
    }
}

public class EmailTemplate
{
    private readonly Dictionary<string, string> templates;
    
    public EmailTemplate()
    {templates = new Dictionary<string, string>
        {
            ["welcome"] = "<h1>Welcome {name}!</h1><p>Thank you for joining us.</p>",
            ["password-reset"] = "<h2>Password Reset</h2><p>Click <a href='{link}'>here</a> to reset your password.</p>",
            ["notification"] = "<h3>Notification</h3><p>{message}</p>",
            ["invoice"] = "<h2>Invoice #{invoice_id}</h2><p>Amount: ${amount}</p><p>Due: {due_date}</p>"
        };
    }
    
    public string GetTemplate(string templateName)
    {
        return templates.TryGetValue(templateName, out var template) ? template : 
               "<p>{message}</p>";
    }
    
    public string ProcessTemplate(string templateName, Dictionary<string, string> variables)
    {
        var template = GetTemplate(templateName);
        
        foreach (var variable in variables)
        {
            template = template.Replace($"{{{variable.Key}}}", variable.Value);
        }
        
        return template;
    }
}

public class EmailValidator
{
    public bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;
        
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
    
    public bool IsValidSubject(string subject)
    {
        return !string.IsNullOrWhiteSpace(subject) && subject.Length <= 200;
    }
    
    public void ValidateEmailRequest(string from, string to, string subject, string body)
    {
        if (!IsValidEmail(from))
            throw new ArgumentException($"Invalid sender email: {from}");
        
        if (!IsValidEmail(to))
            throw new ArgumentException($"Invalid recipient email: {to}");
        
        if (!IsValidSubject(subject))
            throw new ArgumentException("Invalid subject");
        
        if (string.IsNullOrWhiteSpace(body))
            throw new ArgumentException("Email body cannot be empty");
    }
}

// Database Service Subsystem
public class DatabaseConnection
{
    private readonly string connectionString;
    private bool isConnected;
    
    public DatabaseConnection(string connectionString)
    {connectionString = connectionString;
    }
    
    public void Connect()
    {
        Console.WriteLine($"🗄️ Connecting to database: {connectionString}");
        System.Threading.Thread.Sleep(150);isConnected = true;
        Console.WriteLine("✅ Database connected");
    }
    
    public void ExecuteQuery(string sql, Dictionary<string, object> parameters = null)
    {
        if (!isConnected)
            throw new InvalidOperationException("Not connected to database");
        
        Console.WriteLine($"🔍 Executing SQL: {sql}");
        if (parameters != null && parameters.Count > 0)
        {
            Console.WriteLine($"   Parameters: {string.Join(", ", parameters)}");
        }
        System.Threading.Thread.Sleep(100);
        Console.WriteLine("✅ Query executed");
    }
    
    public T QuerySingle<T>(string sql, Dictionary<string, object> parameters = null)
    {
        ExecuteQuery(sql, parameters);
        return default(T); // Simulate return
    }
    
    public void Disconnect()
    {
        if (isConnected)
        {
            Console.WriteLine("🗄️ Disconnecting from database");isConnected = false;
            Console.WriteLine("✅ Database disconnected");
        }
    }
}

public class UserRepository
{
    private readonly DatabaseConnection db;
    
    public UserRepository(DatabaseConnection db)
    {db = db;
    }
    
    public void CreateUser(string email, string name, string hashedPassword)
    {
        var parameters = new Dictionary<string, object>
        {
            ["email"] = email,
            ["name"] = name,
            ["password"] = hashedPassword,
            ["created"] = DateTime.Now
        };db.ExecuteQuery("INSERT INTO Users (Email, Name, Password, Created) VALUES (@email, @name, @password, @created)", parameters);
    }
    
    public bool UserExists(string email)
    {
        var parameters = new Dictionary<string, object> { ["email"] = email };
        var count =db.QuerySingle<int>("SELECT COUNT(*) FROM Users WHERE Email = @email", parameters);
        return count > 0;
    }
    
    public void UpdateLastLogin(string email)
    {
        var parameters = new Dictionary<string, object>
        {
            ["email"] = email,
            ["lastLogin"] = DateTime.Now
        };db.ExecuteQuery("UPDATE Users SET LastLogin = @lastLogin WHERE Email = @email", parameters);
    }
}

// Logging Subsystem
public class FileLogger
{
    private readonly string logPath;
    
    public FileLogger(string logPath = "app.log")
    {logPath = logPath;
    }
    
    public void Log(string level, string message)
    {
        var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";
        Console.WriteLine($"📝 Writing to log: {logEntry}");
        
        // Simulate file writing
        System.Threading.Thread.Sleep(10);
    }
    
    public void Info(string message) => Log("INFO", message);
    public void Warning(string message) => Log("WARN", message);
    public void Error(string message) => Log("ERROR", message);
    public void Debug(string message) => Log("DEBUG", message);
}

public class EventLogger
{
    private readonly List<string> events = new List<string>();
    
    public void LogEvent(string eventType, string description, object data = null)
    {
        var eventEntry = $"{DateTime.Now:HH:mm:ss} - {eventType}: {description}";
        if (data != null)
        {
            eventEntry += $" | Data: {JsonSerializer.Serialize(data)}";
        }events.Add(eventEntry);
        Console.WriteLine($"📊 Event logged: {eventEntry}");
    }
    
    public IReadOnlyList<string> GetEvents() => events.AsReadOnly();
}

// Security Subsystem
public class PasswordHasher
{
    public string HashPassword(string password)
    {
        Console.WriteLine("🔒 Hashing password");
        // Simulate complex hashing (BCrypt, Argon2, etc.)
        System.Threading.Thread.Sleep(200);
        var hash = Convert.ToBase64String(Encoding.UTF8.GetBytes($"hashed_{password}_{DateTime.Now.Ticks}"));
        Console.WriteLine("✅ Password hashed");
        return hash;
    }
    
    public bool VerifyPassword(string password, string hash)
    {
        Console.WriteLine("🔍 Verifying password");
        System.Threading.Thread.Sleep(200);
        // Simulate verification
        return hash.Contains("hashed_");
    }
}

public class TokenGenerator
{
    public string GenerateVerificationToken()
    {
        Console.WriteLine("🎟️ Generating verification token");
        var token = Guid.NewGuid().ToString("N")[..16];
        Console.WriteLine($"✅ Token generated: {token}");
        return token;
    }
    
    public string GeneratePasswordResetToken(string email)
    {
        Console.WriteLine($"🔑 Generating password reset token for {email}");
        var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{email}_{DateTime.Now.Ticks}"));
        Console.WriteLine($"✅ Reset token generated");
        return token;
    }
}

// FACADE - Provides simple interface to complex subsystem
public class UserManagementFacade
{
    private readonly SmtpClient smtpClient;
    private readonly EmailTemplate emailTemplate;
    private readonly EmailValidator emailValidator;
    private readonly DatabaseConnection database;
    private readonly UserRepository userRepository;
    private readonly FileLogger fileLogger;
    private readonly EventLogger eventLogger;
    private readonly PasswordHasher passwordHasher;
    private readonly TokenGenerator tokenGenerator;
    
    public UserManagementFacade(string smtpServer, string dbConnectionString)
    {
        // Initialize all subsystem componentssmtpClient = new SmtpClient(smtpServer);emailTemplate = new EmailTemplate();emailValidator = new EmailValidator();database = new DatabaseConnection(dbConnectionString);userRepository = new UserRepository(database);fileLogger = new FileLogger("user_management.log");eventLogger = new EventLogger();passwordHasher = new PasswordHasher();tokenGenerator = new TokenGenerator();
    }
    
    // Simple interface method that coordinates complex subsystem operations
    public async Task<bool> RegisterUserAsync(string email, string name, string password)
    {
        try
        {
            Console.WriteLine($"\n🚀 Starting user registration for: {email}");
            
            // Step 1: Validate inputemailValidator.ValidateEmailRequest("noreply@company.com", email, "Welcome", "Welcome message");
            
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name is required");
            
            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
                throw new ArgumentException("Password must be at least 6 characters");
            
            // Step 2: Check if user already existsdatabase.Connect();
            
            if (userRepository.UserExists(email))
            {fileLogger.Warning($"Registration attempt for existing user: {email}");
                return false;
            }
            
            // Step 3: Hash password
            var hashedPassword =passwordHasher.HashPassword(password);
            
            // Step 4: Create user in databaseuserRepository.CreateUser(email, name, hashedPassword);
            
            // Step 5: Generate verification token
            var verificationToken =tokenGenerator.GenerateVerificationToken();
            
            // Step 6: Send welcome email
            var emailVariables = new Dictionary<string, string>
            {
                ["name"] = name,
                ["verification_link"] = $"https://company.com/verify?token={verificationToken}"
            };
            
            var emailBody =emailTemplate.ProcessTemplate("welcome", emailVariables);smtpClient.Connect();smtpClient.Authenticate("noreply@company.com", "smtp_password");smtpClient.SendMessage("noreply@company.com", email, "Welcome to Our Platform!", emailBody, true);smtpClient.Disconnect();
            
            // Step 7: Log successfileLogger.Info($"User registered successfully: {email}");eventLogger.LogEvent("USER_REGISTERED", $"New user registration", new { email, name });
            
            // Step 8: Cleanupdatabase.Disconnect();
            
            Console.WriteLine("✅ User registration completed successfully!");
            return true;
        }
        catch (Exception ex)
        {fileLogger.Error($"Registration failed for {email}: {ex.Message}");eventLogger.LogEvent("REGISTRATION_FAILED", ex.Message, new { email });database.Disconnect();
            throw;
        }
    }
    
    public async Task<bool> SendPasswordResetAsync(string email)
    {
        try
        {
            Console.WriteLine($"\n🔑 Starting password reset for: {email}");
            
            // Validate email
            if (!emailValidator.IsValidEmail(email))
                throw new ArgumentException("Invalid email address");
            
            // Check if user existsdatabase.Connect();
            
            if (!userRepository.UserExists(email))
            {fileLogger.Warning($"Password reset attempt for non-existent user: {email}");
                return false;
            }
            
            // Generate reset token
            var resetToken =tokenGenerator.GeneratePasswordResetToken(email);
            
            // Send reset email
            var emailVariables = new Dictionary<string, string>
            {
                ["link"] = $"https://company.com/reset-password?token={resetToken}"
            };
            
            var emailBody =emailTemplate.ProcessTemplate("password-reset", emailVariables);smtpClient.Connect();smtpClient.Authenticate("noreply@company.com", "smtp_password");smtpClient.SendMessage("noreply@company.com", email, "Password Reset Request", emailBody, true);smtpClient.Disconnect();
            
            // Log activityfileLogger.Info($"Password reset email sent to: {email}");eventLogger.LogEvent("PASSWORD_RESET_SENT", "Password reset email sent", new { email });database.Disconnect();
            
            Console.WriteLine("✅ Password reset email sent successfully!");
            return true;
        }
        catch (Exception ex)
        {fileLogger.Error($"Password reset failed for {email}: {ex.Message}");eventLogger.LogEvent("PASSWORD_RESET_FAILED", ex.Message, new { email });database.Disconnect();
            throw;
        }
    }
    
    public async Task<bool> SendNotificationAsync(string email, string message)
    {
        try
        {
            Console.WriteLine($"\n📢 Sending notification to: {email}");
            
            if (!emailValidator.IsValidEmail(email))
                throw new ArgumentException("Invalid email address");
            
            var emailVariables = new Dictionary<string, string>
            {
                ["message"] = message
            };
            
            var emailBody =emailTemplate.ProcessTemplate("notification", emailVariables);smtpClient.Connect();smtpClient.Authenticate("noreply@company.com", "smtp_password");smtpClient.SendMessage("noreply@company.com", email, "Notification", emailBody, true);smtpClient.Disconnect();fileLogger.Info($"Notification sent to: {email}");eventLogger.LogEvent("NOTIFICATION_SENT", "Notification email sent", new { email, message });
            
            Console.WriteLine("✅ Notification sent successfully!");
            return true;
        }
        catch (Exception ex)
        {fileLogger.Error($"Notification failed for {email}: {ex.Message}");eventLogger.LogEvent("NOTIFICATION_FAILED", ex.Message, new { email, message });
            throw;
        }
    }
    
    public List<string> GetRecentActivity()
    {
        return eventLogger.GetEvents().ToList();
    }
}

// Alternative Facade for E-commerce Operations
public class EcommerceFacade
{
    private readonly DatabaseConnection database;
    private readonly SmtpClient emailClient;
    private readonly EmailTemplate emailTemplate;
    private readonly FileLogger logger;
    private readonly EventLogger eventLogger;
    
    public EcommerceFacade(string dbConnection, string smtpServer)
    {database = new DatabaseConnection(dbConnection);emailClient = new SmtpClient(smtpServer);emailTemplate = new EmailTemplate();logger = new FileLogger("ecommerce.log");eventLogger = new EventLogger();
    }
    
    public async Task<string> ProcessOrderAsync(string customerEmail, List<(string product, decimal price)> items)
    {
        try
        {
            Console.WriteLine($"\n🛒 Processing order for: {customerEmail}");
            
            var orderId = Guid.NewGuid().ToString("N")[..8].ToUpper();
            var totalAmount = items.Sum(item => item.price);
            var itemsDescription = string.Join(", ", items.Select(i => $"{i.product} (${i.price})"));
            
            // Save order to databasedatabase.Connect();
            
            var orderParams = new Dictionary<string, object>
            {
                ["orderId"] = orderId,
                ["customerEmail"] = customerEmail,
                ["items"] = itemsDescription,
                ["totalAmount"] = totalAmount,
                ["orderDate"] = DateTime.Now,
                ["status"] = "CONFIRMED"
            };database.ExecuteQuery("INSERT INTO Orders (OrderId, CustomerEmail, Items, TotalAmount, OrderDate, Status) VALUES (@orderId, @customerEmail, @items, @totalAmount, @orderDate, @status)", orderParams);
            
            // Send confirmation email
            var emailVariables = new Dictionary<string, string>
            {
                ["invoice_id"] = orderId,
                ["amount"] = totalAmount.ToString("F2"),
                ["due_date"] = DateTime.Now.AddDays(30).ToString("yyyy-MM-dd")
            };
            
            var emailBody =emailTemplate.ProcessTemplate("invoice", emailVariables);emailClient.Connect();emailClient.Authenticate("orders@company.com", "smtp_password");emailClient.SendMessage("orders@company.com", customerEmail, $"Order Confirmation #{orderId}", emailBody, true);emailClient.Disconnect();
            
            // Log successlogger.Info($"Order {orderId} processed successfully for {customerEmail}. Amount: ${totalAmount}");eventLogger.LogEvent("ORDER_PROCESSED", "Order successfully processed", new { orderId, customerEmail, totalAmount });database.Disconnect();
            
            Console.WriteLine($"✅ Order {orderId} processed successfully!");
            return orderId;
        }
        catch (Exception ex)
        {logger.Error($"Order processing failed for {customerEmail}: {ex.Message}");eventLogger.LogEvent("ORDER_FAILED", ex.Message, new { customerEmail });database.Disconnect();
            throw;
        }
    }
}

// Facade Factory for different configurations
public static class FacadeFactory
{
    public static UserManagementFacade CreateDevelopmentUserManagement()
    {
        return new UserManagementFacade("localhost", "Server=localhost;Database=DevDB;");
    }
    
    public static UserManagementFacade CreateProductionUserManagement()
    {
        return new UserManagementFacade("smtp.company.com", "Server=prod-db;Database=ProdDB;");
    }
    
    public static EcommerceFacade CreateEcommerceFacade(string environment = "development")
    {
        return environment.ToLower() switch
        {
            "production" => new EcommerceFacade("Server=prod-db;Database=EcommerceDB;", "smtp.company.com"),
            "staging" => new EcommerceFacade("Server=staging-db;Database=EcommerceDB;", "smtp-staging.company.com"),
            _ => new EcommerceFacade("Server=localhost;Database=EcommerceDB;", "localhost")
        };
    }
}
```

**Usage**:

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        Console.WriteLine("=== User Management Facade ===");
        
        // Create facade - hides complexity of multiple subsystems
        var userManagement = FacadeFactory.CreateDevelopmentUserManagement();
        
        // Simple interface to complex operations
        try
        {
            // Register new user - coordinates email, database, security, and logging
            await userManagement.RegisterUserAsync("john.doe@email.com", "John Doe", "SecurePassword123");
            
            Console.WriteLine("\n" + new string('=', 50));
            
            // Send password reset - coordinates validation, database, and email
            await userManagement.SendPasswordResetAsync("john.doe@email.com");
            
            Console.WriteLine("\n" + new string('=', 50));
            
            // Send notification - simplified email sending
            await userManagement.SendNotificationAsync("john.doe@email.com", "Your account has been updated successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Operation failed: {ex.Message}");
        }
        
        Console.WriteLine("\n=== Recent Activity Log ===");
        var activities = userManagement.GetRecentActivity();
        foreach (var activity in activities)
        {
            Console.WriteLine($"📋 {activity}");
        }
        
        Console.WriteLine("\n=== E-commerce Facade ===");
        
        // E-commerce operations made simple
        var ecommerce = FacadeFactory.CreateEcommerceFacade("development");
        
        try
        {
            var orderItems = new List<(string product, decimal price)>
            {
                ("Laptop Computer", 999.99m),
                ("Wireless Mouse", 29.99m),
                ("USB Cable", 12.99m)
            };
            
            var orderId = await ecommerce.ProcessOrderAsync("customer@email.com", orderItems);
            Console.WriteLine($"\n🎉 Order completed! Order ID: {orderId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Order failed: {ex.Message}");
        }
        
        Console.WriteLine("\n=== Facade vs Direct Subsystem Usage ===");
        
        // Without Facade (complex - client must know all subsystems)
        Console.WriteLine("\n--- Complex Direct Usage (Without Facade) ---");
        Console.WriteLine("❌ Client must:");
        Console.WriteLine("  1. Know about SmtpClient, EmailTemplate, EmailValidator");
        Console.WriteLine("  2. Understand DatabaseConnection, UserRepository");
        Console.WriteLine("  3. Handle PasswordHasher, TokenGenerator");
        Console.WriteLine("  4. Manage FileLogger, EventLogger");
        Console.WriteLine("  5. Coordinate all these subsystems manually");
        Console.WriteLine("  6. Handle proper cleanup and error handling");
        
        // With Facade (simple - client only needs facade)
        Console.WriteLine("\n--- Simple Facade Usage ---");
        Console.WriteLine("✅ Client only needs:");
        Console.WriteLine("  1. Create facade instance");
        Console.WriteLine("  2. Call simple methods like RegisterUserAsync()");
        Console.WriteLine("  3. Facade handles all complexity internally");
        
        Console.WriteLine("\n=== Facade Pattern Benefits ===");
        Console.WriteLine("✅ Simplifies complex subsystem interactions");
        Console.WriteLine("✅ Reduces coupling between client and subsystem");
        Console.WriteLine("✅ Provides a single point of entry");
        Console.WriteLine("✅ Hides subsystem complexity from clients");
        Console.WriteLine("✅ Makes subsystems easier to use and understand");
        Console.WriteLine("✅ Allows subsystems to evolve independently");
        
        Console.WriteLine("\n=== Real-world Applications ===");
        Console.WriteLine("• API Gateway (microservices)");
        Console.WriteLine("• Payment processing (multiple providers)");
        Console.WriteLine("• File upload (validation, storage, thumbnails)");
        Console.WriteLine("• User authentication (multiple steps)");
        Console.WriteLine("• Order processing (inventory, payment, shipping)");
        Console.WriteLine("• Report generation (data, formatting, delivery)");
        
        Console.WriteLine("\n=== When to Use Facade ===");
        Console.WriteLine("• Complex subsystem with many interdependent classes");
        Console.WriteLine("• Need to provide simple interface to complex functionality");
        Console.WriteLine("• Want to decouple client from subsystem implementation");
        Console.WriteLine("• Multiple subsystems need to work together");
        Console.WriteLine("• Legacy system integration");
        Console.WriteLine("• API simplification for external consumers");
        
        // Demonstrate error handling
        Console.WriteLine("\n=== Error Handling Example ===");
        try
        {
            await userManagement.RegisterUserAsync("invalid-email", "Test User", "pass");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"✅ Facade properly handled validation error: {ex.Message}");
        }
        
        Console.WriteLine("\n=== Facade Variations ===");
        Console.WriteLine("• Minimum Interface Facade: Provides only essential operations");
        Console.WriteLine("• Convenience Facade: Adds helpful utility methods");
        Console.WriteLine("• Layered Facade: Multiple facade layers for different abstraction levels");
        Console.WriteLine("• Configurable Facade: Allows customization of underlying subsystems");
    }
}
```

**Notes**:

- Provides a simple interface to a complex subsystem of classes
- Hides the complexity of subsystem interactions from clients
- Reduces coupling between clients and subsystem implementations
- Does not add new functionality - only simplifies access to existing functionality
- Can be used alongside direct subsystem access when more control is needed
- Makes subsystems easier to use and more maintainable
- Clients are not prevented from using subsystem classes directly if needed
- Different from Adapter pattern - Facade simplifies, Adapter changes interface
- Related patterns: [Mediator](mediator.md), [Abstract Factory](abstract-factory.md), [Singleton](singleton.md)

**Prerequisites**:

- .NET Framework 2.0+ or .NET Core
- Understanding of complex system integration
- Knowledge of service coordination patterns
- Familiarity with dependency management

**References**:

- Gang of Four Design Patterns book
- [Microsoft Docs: Facade Pattern](https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/facade)

---

*Created: 2025-10-31*  
*Last Updated: 2025-10-31*  
*Tags: #facade #structural #simplification #subsystem*
