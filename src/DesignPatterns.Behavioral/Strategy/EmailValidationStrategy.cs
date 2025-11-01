namespace Snippets.DesignPatterns.Behavioral.Strategy;

/// <summary>
/// Email validation strategy with different levels of strictness
/// </summary>
public class EmailValidationStrategy(
    EmailValidationStrategy.EmailValidationLevel level = EmailValidationStrategy.EmailValidationLevel.Standard)
    : IValidationStrategy<string>
{
    public string Name => $"Email Validation ({level})";

    public enum EmailValidationLevel
    {
        Basic, // Simple regex check
        Standard, // More comprehensive regex
        Strict // RFC 5322 compliant
    }

    public ValidationResult Validate(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return new ValidationResult(false, "Email cannot be empty", "EMPTY_EMAIL");
        }

        var errors = new List<string>();
        var warnings = new List<string>();

        // Basic validation
        if (!email.Contains("@"))
        {
            errors.Add("Email must contain @ symbol");
            return new ValidationResult(false, string.Join(", ", errors), "MISSING_AT_SYMBOL");
        }

        var parts = email.Split('@');
        if (parts.Length != 2)
        {
            errors.Add("Email must contain exactly one @ symbol");
            return new ValidationResult(false, string.Join(", ", errors), "MULTIPLE_AT_SYMBOLS");
        }

        var localPart = parts[0];
        var domainPart = parts[1];

        // Validate based on level
        switch (level)
        {
            case EmailValidationLevel.Basic:
                ValidateBasic(localPart, domainPart, errors, warnings);
                break;
            case EmailValidationLevel.Standard:
                ValidateStandard(localPart, domainPart, errors, warnings);
                break;
            case EmailValidationLevel.Strict:
                ValidateStrict(localPart, domainPart, errors, warnings);
                break;
        }

        var isValid = !errors.Any();
        var message = isValid ? "Email is valid" : string.Join(", ", errors);

        return new ValidationResult(isValid, message, isValid ? "VALID" : "INVALID", warnings.ToArray());
    }

    private void ValidateBasic(string local, string domain, List<string> errors, List<string> warnings)
    {
        if (string.IsNullOrEmpty(local))
            errors.Add("Local part cannot be empty");

        if (string.IsNullOrEmpty(domain))
            errors.Add("Domain part cannot be empty");

        if (!domain.Contains("."))
            warnings.Add("Domain should contain at least one dot");
    }

    private void ValidateStandard(string local, string domain, List<string> errors, List<string> warnings)
    {
        ValidateBasic(local, domain, errors, warnings);

        if (local.Length > 64)
            errors.Add("Local part too long (max 64 characters)");

        if (domain.Length > 253)
            errors.Add("Domain part too long (max 253 characters)");

        if (local.StartsWith(".") || local.EndsWith("."))
            errors.Add("Local part cannot start or end with dot");

        if (local.Contains(".."))
            errors.Add("Local part cannot contain consecutive dots");

        if (domain.StartsWith(".") || domain.EndsWith("."))
            errors.Add("Domain cannot start or end with dot");

        if (!System.Text.RegularExpressions.Regex.IsMatch(domain, @"^[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"))
            errors.Add("Invalid domain format");
    }

    private void ValidateStrict(string local, string domain, List<string> errors, List<string> warnings)
    {
        ValidateStandard(local, domain, errors, warnings);

        // Additional RFC 5322 validations
        var invalidLocalChars = new[] { ' ', '\t', '\n', '\r' };
        if (local.Any(c => invalidLocalChars.Contains(c)))
            errors.Add("Local part contains invalid characters");

        if (domain.Any(char.IsWhiteSpace))
            errors.Add("Domain contains whitespace characters");

        // Check for valid TLD
        var tld = domain.Split('.').LastOrDefault();
        if (tld != null && tld.Length < 2)
            warnings.Add("Top-level domain seems too short");
    }
}