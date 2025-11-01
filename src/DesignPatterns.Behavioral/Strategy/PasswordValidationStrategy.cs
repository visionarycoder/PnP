namespace Snippets.DesignPatterns.Behavioral.Strategy;

/// <summary>
/// Password strength validation strategy
/// </summary>
public class PasswordValidationStrategy(PasswordPolicy? policy = null) : IValidationStrategy<string>
{
    private readonly PasswordPolicy policy = policy ?? new PasswordPolicy();

    public string Name => "Password Validation";

    public ValidationResult Validate(string password)
    {
        if (string.IsNullOrEmpty(password))
        {
            return new ValidationResult(false, "Password cannot be empty", "EMPTY_PASSWORD");
        }

        var errors = new List<string>();
        var warnings = new List<string>();
        var score = 0;

        // Length check
        if (password.Length < policy.MinLength)
            errors.Add($"Password must be at least {policy.MinLength} characters");
        else if (password.Length >= policy.MinLength)
            score += 20;

        if (password.Length > policy.MaxLength)
            errors.Add($"Password cannot exceed {policy.MaxLength} characters");

        // Character requirements
        if (policy.RequireUppercase && !password.Any(char.IsUpper))
            errors.Add("Password must contain uppercase letters");
        else if (password.Any(char.IsUpper))
            score += 15;

        if (policy.RequireLowercase && !password.Any(char.IsLower))
            errors.Add("Password must contain lowercase letters");
        else if (password.Any(char.IsLower))
            score += 15;

        if (policy.RequireNumbers && !password.Any(char.IsDigit))
            errors.Add("Password must contain numbers");
        else if (password.Any(char.IsDigit))
            score += 15;

        if (policy.RequireSpecialChars && !password.Any(c => !char.IsLetterOrDigit(c)))
            errors.Add("Password must contain special characters");
        else if (password.Any(c => !char.IsLetterOrDigit(c)))
            score += 15;

        // Common patterns
        if (IsCommonPassword(password))
            warnings.Add("Password is commonly used");

        if (HasRepeatingCharacters(password))
            warnings.Add("Password contains repeating characters");

        if (IsSequential(password))
            warnings.Add("Password contains sequential characters");

        // Additional scoring
        if (password.Length >= 12) score += 10;
        if (password.Length >= 16) score += 10;

        var strength = GetPasswordStrength(score);
        if (strength == "Weak" && !errors.Any())
            warnings.Add($"Password strength is {strength}");

        var isValid = !errors.Any();
        var message = isValid ? $"Password is valid (Strength: {strength})" : string.Join(", ", errors);

        return new ValidationResult(isValid, message, isValid ? "VALID" : "INVALID", warnings.ToArray());
    }

    private bool IsCommonPassword(string password)
    {
        var commonPasswords = new[] { "password", "123456", "password123", "admin", "qwerty", "letmein" };
        return commonPasswords.Contains(password.ToLower());
    }

    private bool HasRepeatingCharacters(string password)
    {
        for (int i = 0; i < password.Length - 2; i++)
        {
            if (password[i] == password[i + 1] && password[i + 1] == password[i + 2])
                return true;
        }

        return false;
    }

    private bool IsSequential(string password)
    {
        var lower = password.ToLower();
        var sequences = new[] { "abc", "123", "qwerty", "asdf" };
        return sequences.Any(seq => lower.Contains(seq));
    }

    private string GetPasswordStrength(int score)
    {
        return score switch
        {
            >= 80 => "Very Strong",
            >= 60 => "Strong",
            >= 40 => "Medium",
            >= 20 => "Weak",
            _ => "Very Weak"
        };
    }
}