namespace Snippets.DesignPatterns.Structural.Facade;

public class EmailTemplate
{
    private readonly Dictionary<string, string> templates = new()
    {
        ["welcome"] = "<h1>Welcome {name}!</h1><p>Thank you for joining us.</p>",
        ["password-reset"] =
            "<h2>Password Reset</h2><p>Click <a href='{link}'>here</a> to reset your password.</p>",
        ["notification"] = "<h3>Notification</h3><p>{message}</p>",
        ["invoice"] = "<h2>Invoice #{invoice_id}</h2><p>Amount: ${amount}</p><p>Due: {due_date}</p>"
    };

    public string GetTemplate(string templateName)
    {
        return templates.TryGetValue(templateName, out var template) ? template : "<p>{message}</p>";
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