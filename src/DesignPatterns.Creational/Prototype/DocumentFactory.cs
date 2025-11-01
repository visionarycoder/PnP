namespace Snippets.DesignPatterns.Creational.Prototype;

/// <summary>
/// Factory that uses prototype pattern
/// </summary>
public class DocumentFactory
{
    private readonly DocumentPrototypeRegistry registry;

    public DocumentFactory()
    {
        registry = new DocumentPrototypeRegistry();
        InitializeStandardPrototypes();
    }

    private void InitializeStandardPrototypes()
    {
        // Create standard document templates
        var reportTemplate = new Document("Monthly Report", "This is a standard monthly report template.")
        {
            Metadata = new DocumentMetadata
            {
                Author = "System",
                Department = "Management",
                Version = "2.0"
            }
        };
        reportTemplate.Tags.AddRange(["report", "monthly", "template"]);
        reportTemplate.AddSection("Executive Summary", "[Executive summary content goes here]");
        reportTemplate.AddSection("Detailed Analysis", "[Detailed analysis content goes here]");
        reportTemplate.AddSection("Recommendations", "[Recommendations content goes here]");

        var memoTemplate = new Document("Internal Memo", "Standard internal memo format.")
        {
            Metadata = new DocumentMetadata
            {
                Author = "System",
                Department = "HR",
                Version = "1.5"
            }
        };
        memoTemplate.Tags.AddRange(["memo", "internal", "template"]);
        memoTemplate.AddSection("Purpose", "[Purpose of the memo]");
        memoTemplate.AddSection("Details", "[Detailed information]");

        registry.RegisterPrototype("report", reportTemplate);
        registry.RegisterPrototype("memo", memoTemplate);
    }

    public Document CreateReport()
    {
        return registry.CreateDocument("report");
    }

    public Document CreateMemo()
    {
        return registry.CreateDocument("memo");
    }

    public Document CreateDocument(string type)
    {
        return registry.CreateDocument(type);
    }

    public void RegisterCustomTemplate(string key, Document template)
    {
        registry.RegisterPrototype(key, template);
    }

    public IEnumerable<string> GetAvailableTemplates()
    {
        return registry.GetAvailablePrototypes();
    }
}