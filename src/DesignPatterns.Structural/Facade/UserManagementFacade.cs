namespace Snippets.DesignPatterns.Structural.Facade;

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
        // Initialize all subsystem components
        smtpClient = new SmtpClient(smtpServer);
        emailTemplate = new EmailTemplate();
        emailValidator = new EmailValidator();
        database = new DatabaseConnection(dbConnectionString);
        userRepository = new UserRepository(database);
        fileLogger = new FileLogger("user_management.log");
        eventLogger = new EventLogger();
        passwordHasher = new PasswordHasher();
        tokenGenerator = new TokenGenerator();
    }

    // Simple interface method that coordinates complex subsystem operations
    public async Task<bool> RegisterUserAsync(string email, string name, string password)
    {
        try
        {
            Console.WriteLine($"\n🚀 Starting user registration for: {email}");

            // Step 1: Validate input
            emailValidator.ValidateEmailRequest("noreply@company.com", email, "Welcome", "Welcome message");
            fileLogger.Info($"Starting registration for {email}");

            // Step 2: Connect to database
            database.Connect();

            // Step 3: Check if user already exists
            if (userRepository.UserExists(email))
            {
                fileLogger.Warning($"Registration failed - user already exists: {email}");
                throw new InvalidOperationException("User already exists");
            }

            // Step 4: Hash password
            var hashedPassword = passwordHasher.HashPassword(password);

            // Step 5: Create user in database
            userRepository.CreateUser(email, name, hashedPassword);
            eventLogger.LogEvent("USER_CREATED", "New user registered", new { Email = email, Name = name });

            // Step 6: Send welcome email
            await SendWelcomeEmailAsync(email, name);

            // Step 7: Cleanup
            database.Disconnect();

            fileLogger.Info($"User registration completed successfully: {email}");
            return true;
        }
        catch (Exception ex)
        {
            fileLogger.Error($"Registration failed for {email}: {ex.Message}");
            database.Disconnect();
            return false;
        }
    }

    public async Task<bool> LoginUserAsync(string email, string password)
    {
        try
        {
            Console.WriteLine($"\n🔐 User login attempt: {email}");

            fileLogger.Info($"Login attempt for {email}");
            database.Connect();

            // Simulate password verification (in real app, you'd fetch hash from DB)
            var isValidPassword = passwordHasher.VerifyPassword(password, "hashed_password");

            if (isValidPassword)
            {
                userRepository.UpdateLastLogin(email);
                eventLogger.LogEvent("USER_LOGIN", "Successful login", new { Email = email });
                fileLogger.Info($"Login successful: {email}");

                database.Disconnect();
                return true;
            }
            else
            {
                fileLogger.Warning($"Login failed - invalid password: {email}");
                database.Disconnect();
                return false;
            }
        }
        catch (Exception ex)
        {
            fileLogger.Error($"Login error for {email}: {ex.Message}");
            database.Disconnect();
            return false;
        }
    }

    public async Task<bool> RequestPasswordResetAsync(string email)
    {
        try
        {
            Console.WriteLine($"\n🔄 Password reset request: {email}");

            fileLogger.Info($"Password reset requested for {email}");
            database.Connect();

            if (!userRepository.UserExists(email))
            {
                fileLogger.Warning($"Password reset failed - user not found: {email}");
                database.Disconnect();
                return false;
            }

            // Generate reset token
            var resetToken = tokenGenerator.GeneratePasswordResetToken(email);

            // Send reset email
            smtpClient.Connect();
            smtpClient.Authenticate("noreply@company.com", "smtp_password");

            var variables = new Dictionary<string, string>
            {
                ["link"] = $"https://company.com/reset?token={resetToken}"
            };

            var emailBody = emailTemplate.ProcessTemplate("password-reset", variables);
            smtpClient.SendMessage("noreply@company.com", email, "Password Reset Request", emailBody, true);
            smtpClient.Disconnect();

            eventLogger.LogEvent("PASSWORD_RESET_REQUESTED", "Password reset email sent", new { Email = email });

            database.Disconnect();
            fileLogger.Info($"Password reset email sent to: {email}");
            return true;
        }
        catch (Exception ex)
        {
            fileLogger.Error($"Password reset failed for {email}: {ex.Message}");
            database.Disconnect();
            return false;
        }
    }

    private async Task SendWelcomeEmailAsync(string email, string name)
    {
        try
        {
            smtpClient.Connect();
            smtpClient.Authenticate("noreply@company.com", "smtp_password");

            var variables = new Dictionary<string, string>
            {
                ["name"] = name
            };

            var emailBody = emailTemplate.ProcessTemplate("welcome", variables);
            smtpClient.SendMessage("noreply@company.com", email, "Welcome to Our Service!", emailBody, true);
            smtpClient.Disconnect();

            eventLogger.LogEvent("WELCOME_EMAIL_SENT", "Welcome email sent to new user", new { Email = email });
        }
        catch (Exception ex)
        {
            fileLogger.Error($"Failed to send welcome email to {email}: {ex.Message}");
            throw; // Re-throw to handle in calling method
        }
    }

    public IReadOnlyList<string> GetEventHistory()
    {
        return eventLogger.GetEvents();
    }
}