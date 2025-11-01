# Password Security and Hashing

**Description**: Comprehensive password security implementation with secure hashing (Argon2id), password validation, breach checking, and secure password generation for .NET applications.

**Language/Technology**: C#, .NET 8+

**Code**:

```csharp
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Konscious.Security.Cryptography;

// Password configuration options
public class PasswordOptions
{
    public int MinLength { get; set; } = 8;
    public int MaxLength { get; set; } = 100;
    public bool RequireUppercase { get; set; } = true;
    public bool RequireLowercase { get; set; } = true;
    public bool RequireDigit { get; set; } = true;
    public bool RequireSpecialChar { get; set; } = true;
    public int MaxConsecutiveChars { get; set; } = 2;
    public int MinUniqueChars { get; set; } = 5;
    public string[] CommonPasswords { get; set; } = Array.Empty<string>();
    public int PasswordHistoryLimit { get; set; } = 12;
    public int MaxFailedAttempts { get; set; } = 5;
    public TimeSpan LockoutDuration { get; set; } = TimeSpan.FromMinutes(15);
}

// Argon2 hashing configuration
public class Argon2Options
{
    public int MemorySize { get; set; } = 65536; // 64 MB
    public int Iterations { get; set; } = 3;
    public int DegreeOfParallelism { get; set; } = 1;
    public int SaltLength { get; set; } = 32;
    public int HashLength { get; set; } = 64;
}

// Password validation result
public class PasswordValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public int Score { get; set; }
    public PasswordStrength Strength { get; set; }
}

public enum PasswordStrength
{
    VeryWeak = 0,
    Weak = 1,
    Fair = 2,
    Good = 3,
    Strong = 4,
    VeryStrong = 5
}

// Main password service interface
public interface IPasswordService
{
    Task<string> HashPasswordAsync(string password);
    Task<bool> VerifyPasswordAsync(string password, string hashedPassword);
    Task<PasswordValidationResult> ValidatePasswordAsync(string password, string? userId = null);
    Task<bool> IsPasswordCompromisedAsync(string password);
    string GenerateSecurePassword(int length = 16);
    Task<bool> IsPasswordInHistoryAsync(string userId, string password);
    Task SavePasswordToHistoryAsync(string userId, string hashedPassword);
}

// Password service implementation
public class PasswordService : IPasswordService
{
    private readonly PasswordOptions passwordOptions;
    private readonly Argon2Options argon2Options;
    private readonly IPasswordHistoryRepository passwordHistoryRepository;
    private readonly IHaveIBeenPwnedService breachService;
    private readonly ILogger<PasswordService> logger;

    public PasswordService(
        IOptions<PasswordOptions> passwordOptions,
        IOptions<Argon2Options> argon2Options,
        IPasswordHistoryRepository passwordHistoryRepository,
        IHaveIBeenPwnedService breachService,
        ILogger<PasswordService> logger)
    {
        passwordOptions = passwordOptions.Value;
        argon2Options = argon2Options.Value;
        this.passwordHistoryRepository = passwordHistoryRepository;
        this.breachService = breachService;
        this.logger = logger;
    }

    public async Task<string> HashPasswordAsync(string password)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password cannot be null or empty", nameof(password));

        // Generate salt
        var salt = GenerateSalt();

        // Create Argon2id hasher
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            MemorySize = argon2Options.MemorySize,
            Iterations = argon2Options.Iterations,
            DegreeOfParallelism = argon2Options.DegreeOfParallelism
        };

        // Generate hash
        var hash = await argon2.GetBytesAsync(argon2Options.HashLength);

        // Combine salt and hash
        var result = new byte[salt.Length + hash.Length];
        Buffer.BlockCopy(salt, 0, result, 0, salt.Length);
        Buffer.BlockCopy(hash, 0, result, salt.Length, hash.Length);

        return Convert.ToBase64String(result);
    }

    public async Task<bool> VerifyPasswordAsync(string password, string hashedPassword)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hashedPassword))
            return false;

        try
        {
            var combined = Convert.FromBase64String(hashedPassword);
            var salt = new byte[argon2Options.SaltLength];
            var hash = new byte[argon2Options.HashLength];

            Buffer.BlockCopy(combined, 0, salt, 0, salt.Length);
            Buffer.BlockCopy(combined, salt.Length, hash, 0, hash.Length);

            using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
            {
                Salt = salt,
                MemorySize = argon2Options.MemorySize,
                Iterations = argon2Options.Iterations,
                DegreeOfParallelism = argon2Options.DegreeOfParallelism
            };

            var computedHash = await argon2.GetBytesAsync(argon2Options.HashLength);
            return CryptographicOperations.FixedTimeEquals(hash, computedHash);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error verifying password hash");
            return false;
        }
    }

    public async Task<PasswordValidationResult> ValidatePasswordAsync(string password, string? userId = null)
    {
        var result = new PasswordValidationResult { IsValid = true };

        if (string.IsNullOrEmpty(password))
        {
            result.IsValid = false;
            result.Errors.Add("Password cannot be empty");
            return result;
        }

        // Length validation
        if (password.Length < passwordOptions.MinLength)
        {
            result.IsValid = false;
            result.Errors.Add($"Password must be at least {passwordOptions.MinLength} characters long");
        }

        if (password.Length > passwordOptions.MaxLength)
        {
            result.IsValid = false;
            result.Errors.Add($"Password cannot exceed {passwordOptions.MaxLength} characters");
        }

        // Character requirements
        if (passwordOptions.RequireUppercase && !password.Any(char.IsUpper))
        {
            result.IsValid = false;
            result.Errors.Add("Password must contain at least one uppercase letter");
        }

        if (passwordOptions.RequireLowercase && !password.Any(char.IsLower))
        {
            result.IsValid = false;
            result.Errors.Add("Password must contain at least one lowercase letter");
        }

        if (passwordOptions.RequireDigit && !password.Any(char.IsDigit))
        {
            result.IsValid = false;
            result.Errors.Add("Password must contain at least one digit");
        }

        if (passwordOptions.RequireSpecialChar && !password.Any(IsSpecialCharacter))
        {
            result.IsValid = false;
            result.Errors.Add("Password must contain at least one special character");
        }

        // Consecutive character check
        if (HasConsecutiveCharacters(password, passwordOptions.MaxConsecutiveChars))
        {
            result.IsValid = false;
            result.Errors.Add($"Password cannot have more than {passwordOptions.MaxConsecutiveChars} consecutive identical characters");
        }

        // Unique character check
        var uniqueChars = password.Distinct().Count();
        if (uniqueChars < passwordOptions.MinUniqueChars)
        {
            result.IsValid = false;
            result.Errors.Add($"Password must contain at least {passwordOptions.MinUniqueChars} unique characters");
        }

        // Common password check
        if (passwordOptions.CommonPasswords.Contains(password.ToLowerInvariant()))
        {
            result.IsValid = false;
            result.Errors.Add("Password is too common, please choose a different one");
        }

        // Breach check
        if (await IsPasswordCompromisedAsync(password))
        {
            result.IsValid = false;
            result.Errors.Add("Password has been found in data breaches, please choose a different one");
        }

        // Password history check
        if (!string.IsNullOrEmpty(userId) && await IsPasswordInHistoryAsync(userId, password))
        {
            result.IsValid = false;
            result.Errors.Add("Password has been used recently, please choose a different one");
        }

        // Calculate strength score
        result.Score = CalculatePasswordScore(password);
        result.Strength = GetPasswordStrength(result.Score);

        return result;
    }

    public async Task<bool> IsPasswordCompromisedAsync(string password)
    {
        try
        {
            return await breachService.IsPasswordBreachedAsync(password);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to check password breach status");
            return false; // Fail open to not block users if service is down
        }
    }

    public string GenerateSecurePassword(int length = 16)
    {
        if (length < 8) length = 8;
        if (length > 128) length = 128;

        const string lowercase = "abcdefghijklmnopqrstuvwxyz";
        const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string digits = "0123456789";
        const string specialChars = "!@#$%^&*()_+-=[]{}|;:,.<>?";

        var allChars = lowercase + uppercase + digits + specialChars;
        var password = new StringBuilder();

        using var rng = RandomNumberGenerator.Create();

        // Ensure at least one character from each required set
        if (passwordOptions.RequireLowercase)
            password.Append(GetRandomChar(lowercase, rng));
        if (passwordOptions.RequireUppercase)
            password.Append(GetRandomChar(uppercase, rng));
        if (passwordOptions.RequireDigit)
            password.Append(GetRandomChar(digits, rng));
        if (passwordOptions.RequireSpecialChar)
            password.Append(GetRandomChar(specialChars, rng));

        // Fill remaining positions
        while (password.Length < length)
        {
            password.Append(GetRandomChar(allChars, rng));
        }

        // Shuffle the password
        return ShuffleString(password.ToString(), rng);
    }

    public async Task<bool> IsPasswordInHistoryAsync(string userId, string password)
    {
        var passwordHistory = await passwordHistoryRepository.GetPasswordHistoryAsync(
            userId, passwordOptions.PasswordHistoryLimit);

        foreach (var historicalHash in passwordHistory)
        {
            if (await VerifyPasswordAsync(password, historicalHash))
                return true;
        }

        return false;
    }

    public async Task SavePasswordToHistoryAsync(string userId, string hashedPassword)
    {
        await passwordHistoryRepository.AddPasswordToHistoryAsync(userId, hashedPassword);
        await passwordHistoryRepository.CleanupOldPasswordsAsync(userId, passwordOptions.PasswordHistoryLimit);
    }

    private byte[] GenerateSalt()
    {
        var salt = new byte[argon2Options.SaltLength];
        RandomNumberGenerator.Fill(salt);
        return salt;
    }

    private static bool IsSpecialCharacter(char c)
    {
        return !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c);
    }

    private static bool HasConsecutiveCharacters(string password, int maxConsecutive)
    {
        if (maxConsecutive <= 0) return false;

        for (int i = 0; i <= password.Length - maxConsecutive - 1; i++)
        {
            var consecutiveCount = 1;
            for (int j = i + 1; j < password.Length && password[j] == password[i]; j++)
            {
                consecutiveCount++;
                if (consecutiveCount > maxConsecutive)
                    return true;
            }
        }

        return false;
    }

    private static int CalculatePasswordScore(string password)
    {
        int score = 0;

        // Length bonus
        score += Math.Min(password.Length * 2, 20);

        // Character variety bonus
        if (password.Any(char.IsLower)) score += 5;
        if (password.Any(char.IsUpper)) score += 5;
        if (password.Any(char.IsDigit)) score += 5;
        if (password.Any(IsSpecialCharacter)) score += 10;

        // Unique character bonus
        var uniqueChars = password.Distinct().Count();
        score += Math.Min(uniqueChars * 2, 20);

        // Pattern penalties
        if (Regex.IsMatch(password, @"(.)\1{2,}")) score -= 10; // Repeated characters
        if (Regex.IsMatch(password, @"(012|123|234|345|456|567|678|789|890)")) score -= 5; // Sequential numbers
        if (Regex.IsMatch(password, @"(abc|bcd|cde|def|efg|fgh|ghi|hij|ijk|jkl|klm|lmn|mno|nop|opq|pqr|qrs|rst|stu|tuv|uvw|vwx|wxy|xyz)", RegexOptions.IgnoreCase)) score -= 5; // Sequential letters

        return Math.Max(0, Math.Min(100, score));
    }

    private static PasswordStrength GetPasswordStrength(int score)
    {
        return score switch
        {
            < 20 => PasswordStrength.VeryWeak,
            < 40 => PasswordStrength.Weak,
            < 60 => PasswordStrength.Fair,
            < 80 => PasswordStrength.Good,
            < 95 => PasswordStrength.Strong,
            _ => PasswordStrength.VeryStrong
        };
    }

    private static char GetRandomChar(string chars, RandomNumberGenerator rng)
    {
        var bytes = new byte[4];
        rng.GetBytes(bytes);
        var index = Math.Abs(BitConverter.ToInt32(bytes)) % chars.Length;
        return chars[index];
    }

    private static string ShuffleString(string input, RandomNumberGenerator rng)
    {
        var chars = input.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            var bytes = new byte[4];
            rng.GetBytes(bytes);
            var randomIndex = Math.Abs(BitConverter.ToInt32(bytes)) % chars.Length;
            (chars[i], chars[randomIndex]) = (chars[randomIndex], chars[i]);
        }
        return new string(chars);
    }
}

// Have I Been Pwned service for breach checking
public interface IHaveIBeenPwnedService
{
    Task<bool> IsPasswordBreachedAsync(string password);
}

public class HaveIBeenPwnedService : IHaveIBeenPwnedService
{
    private readonly HttpClient httpClient;
    private readonly ILogger<HaveIBeenPwnedService> logger;

    public HaveIBeenPwnedService(HttpClient httpClient, ILogger<HaveIBeenPwnedService> logger)
    {
        this.httpClient = httpClient;
        this.logger = logger;
        httpClient.DefaultRequestHeaders.Add("User-Agent", "YourAppName");
    }

    public async Task<bool> IsPasswordBreachedAsync(string password)
    {
        try
        {
            // Hash the password using SHA-1
            using var sha1 = SHA1.Create();
            var hashBytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(password));
            var hashString = Convert.ToHexString(hashBytes);

            // Use k-anonymity: send only first 5 characters
            var prefix = hashString[..5];
            var suffix = hashString[5..];

            var response = await httpClient.GetAsync($"https://api.pwnedpasswords.com/range/{prefix}");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return content.Contains(suffix, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking password breach status");
            return false;
        }
    }
}

// Account lockout service
public interface IAccountLockoutService
{
    Task<bool> IsAccountLockedAsync(string userId);
    Task RecordFailedLoginAttemptAsync(string userId);
    Task ResetFailedLoginAttemptsAsync(string userId);
    Task<TimeSpan?> GetLockoutTimeRemainingAsync(string userId);
}

public class AccountLockoutService : IAccountLockoutService
{
    private readonly PasswordOptions options;
    private readonly IAccountLockoutRepository repository;
    private readonly ILogger<AccountLockoutService> logger;

    public AccountLockoutService(
        IOptions<PasswordOptions> options,
        IAccountLockoutRepository repository,
        ILogger<AccountLockoutService> logger)
    {
        options = options.Value;
        this.repository = repository;
        this.logger = logger;
    }

    public async Task<bool> IsAccountLockedAsync(string userId)
    {
        var lockoutInfo = await repository.GetLockoutInfoAsync(userId);
        if (lockoutInfo == null) return false;

        if (lockoutInfo.LockedUntil.HasValue && lockoutInfo.LockedUntil > DateTime.UtcNow)
        {
            return true;
        }

        // Reset if lockout has expired
        if (lockoutInfo.LockedUntil.HasValue && lockoutInfo.LockedUntil <= DateTime.UtcNow)
        {
            await ResetFailedLoginAttemptsAsync(userId);
        }

        return false;
    }

    public async Task RecordFailedLoginAttemptAsync(string userId)
    {
        var lockoutInfo = await repository.GetLockoutInfoAsync(userId) ?? new AccountLockoutInfo { UserId = userId };
        
        lockoutInfo.FailedAttempts++;
        lockoutInfo.LastFailedAttempt = DateTime.UtcNow;

        if (lockoutInfo.FailedAttempts >= options.MaxFailedAttempts)
        {
            lockoutInfo.LockedUntil = DateTime.UtcNow.Add(options.LockoutDuration);
            logger.LogWarning("Account locked for user {UserId} due to {FailedAttempts} failed attempts", 
                userId, lockoutInfo.FailedAttempts);
        }

        await repository.UpdateLockoutInfoAsync(lockoutInfo);
    }

    public async Task ResetFailedLoginAttemptsAsync(string userId)
    {
        await repository.ResetLockoutInfoAsync(userId);
    }

    public async Task<TimeSpan?> GetLockoutTimeRemainingAsync(string userId)
    {
        var lockoutInfo = await repository.GetLockoutInfoAsync(userId);
        if (lockoutInfo?.LockedUntil == null) return null;

        var remaining = lockoutInfo.LockedUntil.Value - DateTime.UtcNow;
        return remaining > TimeSpan.Zero ? remaining : null;
    }
}

// Models
public class AccountLockoutInfo
{
    public required string UserId { get; set; }
    public int FailedAttempts { get; set; }
    public DateTime? LastFailedAttempt { get; set; }
    public DateTime? LockedUntil { get; set; }
}

public class PasswordHistoryEntry
{
    public int Id { get; set; }
    public required string UserId { get; set; }
    public required string HashedPassword { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

**Usage**:

```csharp
// Program.cs - Service registration
var builder = WebApplication.CreateBuilder(args);

// Configure password options
builder.Services.Configure<PasswordOptions>(
    builder.Configuration.GetSection("PasswordOptions"));

builder.Services.Configure<Argon2Options>(
    builder.Configuration.GetSection("Argon2Options"));

// Register services
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IAccountLockoutService, AccountLockoutService>();
builder.Services.AddHttpClient<IHaveIBeenPwnedService, HaveIBeenPwnedService>();

var app = builder.Build();

// Usage in user service
public class UserService
{
    private readonly IPasswordService passwordService;
    private readonly IAccountLockoutService lockoutService;

    public async Task<bool> AuthenticateUserAsync(string email, string password)
    {
        var user = await userRepository.GetByEmailAsync(email);
        if (user == null) return false;

        // Check if account is locked
        if (await lockoutService.IsAccountLockedAsync(user.Id))
        {
            throw new AccountLockedException("Account is temporarily locked due to too many failed attempts");
        }

        // Verify password
        var isValid = await passwordService.VerifyPasswordAsync(password, user.HashedPassword);
        
        if (isValid)
        {
            await lockoutService.ResetFailedLoginAttemptsAsync(user.Id);
            return true;
        }
        else
        {
            await lockoutService.RecordFailedLoginAttemptAsync(user.Id);
            return false;
        }
    }

    public async Task<bool> ChangePasswordAsync(string userId, string currentPassword, string newPassword)
    {
        var user = await userRepository.GetByIdAsync(userId);
        if (user == null) return false;

        // Verify current password
        if (!await passwordService.VerifyPasswordAsync(currentPassword, user.HashedPassword))
            return false;

        // Validate new password
        var validation = await passwordService.ValidatePasswordAsync(newPassword, userId);
        if (!validation.IsValid)
        {
            throw new PasswordValidationException(validation.Errors);
        }

        // Hash and save new password
        var hashedPassword = await passwordService.HashPasswordAsync(newPassword);
        await passwordService.SavePasswordToHistoryAsync(userId, user.HashedPassword);
        
        user.HashedPassword = hashedPassword;
        user.PasswordChangedAt = DateTime.UtcNow;
        
        await userRepository.UpdateAsync(user);
        return true;
    }

    public async Task<string> GenerateTemporaryPasswordAsync()
    {
        return passwordService.GenerateSecurePassword(12);
    }

    public async Task<PasswordValidationResult> ValidatePasswordAsync(string password, string? userId = null)
    {
        return await passwordService.ValidatePasswordAsync(password, userId);
    }
}

// Controller example
[ApiController]
[Route("api/[controller]")]
public class PasswordController : ControllerBase
{
    private readonly IPasswordService passwordService;

    [HttpPost("validate")]
    public async Task<IActionResult> ValidatePassword([FromBody] ValidatePasswordRequest request)
    {
        var result = await passwordService.ValidatePasswordAsync(request.Password, request.UserId);
        return Ok(result);
    }

    [HttpPost("generate")]
    public IActionResult GeneratePassword([FromQuery] int length = 16)
    {
        var password = passwordService.GenerateSecurePassword(length);
        return Ok(new { Password = password });
    }

    [HttpPost("check-breach")]
    public async Task<IActionResult> CheckBreach([FromBody] CheckBreachRequest request)
    {
        var isCompromised = await passwordService.IsPasswordCompromisedAsync(request.Password);
        return Ok(new { IsCompromised = isCompromised });
    }
}

// Configuration example (appsettings.json)
{
  "PasswordOptions": {
    "MinLength": 8,
    "MaxLength": 100,
    "RequireUppercase": true,
    "RequireLowercase": true,
    "RequireDigit": true,
    "RequireSpecialChar": true,
    "MaxConsecutiveChars": 2,
    "MinUniqueChars": 5,
    "PasswordHistoryLimit": 12,
    "MaxFailedAttempts": 5,
    "LockoutDuration": "00:15:00"
  },
  "Argon2Options": {
    "MemorySize": 65536,
    "Iterations": 3,
    "DegreeOfParallelism": 1,
    "SaltLength": 32,
    "HashLength": 64
  }
}

// Client-side password strength indicator
public class PasswordStrengthMeter
{
    public static string GetStrengthColor(PasswordStrength strength)
    {
        return strength switch
        {
            PasswordStrength.VeryWeak => "#ff0000",
            PasswordStrength.Weak => "#ff6600",
            PasswordStrength.Fair => "#ffcc00",
            PasswordStrength.Good => "#66cc00",
            PasswordStrength.Strong => "#00cc00",
            PasswordStrength.VeryStrong => "#006600",
            _ => "#cccccc"
        };
    }

    public static string GetStrengthText(PasswordStrength strength)
    {
        return strength switch
        {
            PasswordStrength.VeryWeak => "Very Weak",
            PasswordStrength.Weak => "Weak",
            PasswordStrength.Fair => "Fair",
            PasswordStrength.Good => "Good",
            PasswordStrength.Strong => "Strong",
            PasswordStrength.VeryStrong => "Very Strong",
            _ => "Unknown"
        };
    }
}
```

**Prerequisites**:

- .NET 8 or later
- Konscious.Security.Cryptography package (for Argon2id)
- HttpClient for breach checking
- Entity Framework Core (for password history and lockout storage)

**Notes**:

- **Argon2id**: Use Argon2id over bcrypt or PBKDF2 for superior resistance to both side-channel and GPU attacks
- **Salt Generation**: Always use cryptographically secure random salts for each password
- **Memory-Hard Function**: Argon2id's memory requirements make it expensive for attackers to parallelize
- **Breach Checking**: Implement k-anonymity pattern to check breaches without exposing passwords
- **Password History**: Store hashed versions of previous passwords to prevent reuse
- **Account Lockout**: Implement progressive lockout to prevent brute force attacks
- **Secure Generation**: Use cryptographically secure random number generator for password generation
- **Fail Open**: Breach checking should fail open to not block users if the service is unavailable

**Related Snippets**:

- [JWT Authentication](jwt-authentication.md)
- [Role-Based Authorization](role-based-authorization.md)
- [Audit Logging](audit-logging.md)

**References**:

- [OWASP Password Storage Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html)
- [Argon2 Specification](https://tools.ietf.org/html/rfc9106)
- [Have I Been Pwned API](https://haveibeenpwned.com/API/v3)

---

*Created: 2025-11-01*  
*Last Updated: 2025-11-01*  
*Tags: #password #security #argon2 #hashing #authentication #breach-checking*
