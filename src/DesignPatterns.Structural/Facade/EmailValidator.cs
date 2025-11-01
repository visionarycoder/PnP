namespace Snippets.DesignPatterns.Structural.Facade;

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