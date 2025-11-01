namespace Snippets.DesignPatterns.Behavioral.TemplateMethod;

/// <summary>
/// Abstract document generator with template method pattern
/// </summary>
public abstract class DocumentGenerator
{
    protected DocumentMetadata Metadata = new();
    protected List<DocumentSection> Sections = [];
    protected DocumentFormatting Formatting = new();
    protected List<string> Logs = [];

    /// <summary>
    /// Template method for document generation
    /// </summary>
    public GeneratedDocument GenerateDocument(DocumentRequest request)
    {
        try
        {
            LogMessage($"📄 Starting document generation: {request.Title}");

            // Step 1: Initialize document
            InitializeDocument(request);

            // Step 2: Create metadata
            CreateMetadata(request);

            // Step 3: Generate header (conditional)
            if (ShouldIncludeHeader(request))
            {
                GenerateHeader(request);
            }

            // Step 4: Generate table of contents (conditional)
            if (ShouldIncludeTableOfContents(request))
            {
                GenerateTableOfContents();
            }

            // Step 5: Generate main content (abstract - must be implemented)
            GenerateContent(request);

            // Step 6: Generate appendices (conditional)
            if (ShouldIncludeAppendices(request))
            {
                GenerateAppendices(request);
            }

            // Step 7: Generate footer (conditional)
            if (ShouldIncludeFooter(request))
            {
                GenerateFooter(request);
            }

            // Step 8: Apply formatting (abstract - must be implemented)
            ApplyFormatting();

            // Step 9: Validate document
            ValidateDocument();

            // Step 10: Finalize document (abstract - must be implemented)
            var result = FinalizeDocument();

            LogMessage($"✅ Document generation completed: {result.PageCount} pages");
            return result;
        }
        catch (Exception ex)
        {
            LogMessage($"❌ Document generation failed: {ex.Message}");
            return HandleGenerationError(ex, request);
        }
    }

    // Abstract methods - must be implemented by subclasses
    protected abstract void GenerateContent(DocumentRequest request);
    protected abstract void ApplyFormatting();
    protected abstract GeneratedDocument FinalizeDocument();

    // Virtual methods with default behavior - can be overridden
    protected virtual void InitializeDocument(DocumentRequest request)
    {
        Sections.Clear();
        Logs.Clear();
        Formatting = new DocumentFormatting();
        LogMessage("📋 Document initialized");
    }

    protected virtual void CreateMetadata(DocumentRequest request)
    {
        Metadata = new DocumentMetadata
        {
            Title = request.Title ?? "Untitled Document",
            Author = request.Author ?? "Unknown Author",
            CreatedDate = DateTime.UtcNow,
            Version = "1.0",
            Subject = request.Subject ?? "",
            Keywords = request.Keywords ?? []
        };
        LogMessage($"📊 Metadata created for: {Metadata.Title}");
    }

    protected virtual void GenerateHeader(DocumentRequest request)
    {
        var header = new DocumentSection
        {
            Type = SectionType.Header,
            Title = Metadata.Title,
            Content = $"""
                       {Metadata.Title}
                       Author: {Metadata.Author}
                       Date: {Metadata.CreatedDate:yyyy-MM-dd}
                       Version: {Metadata.Version}
                       """,
            Order = 0
        };
        Sections.Insert(0, header);
        LogMessage("📋 Header generated");
    }

    protected virtual void GenerateTableOfContents()
    {
        var contentSections = Sections.Where(s => s.Type == SectionType.Content).ToList();
        var tocContent = string.Join("\n", contentSections.Select((s, i) => $"{i + 1}. {s.Title}"));

        var tocSection = new DocumentSection
        {
            Type = SectionType.TableOfContents,
            Title = "Table of Contents",
            Content = tocContent,
            Order = 1
        };

        // Insert after header if exists, otherwise at the beginning
        var insertIndex = Sections.Any(s => s.Type == SectionType.Header) ? 1 : 0;
        Sections.Insert(insertIndex, tocSection);
        LogMessage("📑 Table of contents generated");
    }

    protected virtual void GenerateAppendices(DocumentRequest request)
    {
        if (request.Data.TryGetValue("appendices", out var appendicesData))
        {
            LogMessage("📎 Generating appendices");
            // Override in subclasses to handle specific appendix types
        }
    }

    protected virtual void GenerateFooter(DocumentRequest request)
    {
        var footer = new DocumentSection
        {
            Type = SectionType.Footer,
            Title = "Footer",
            Content = $"Generated on {DateTime.UtcNow:yyyy-MM-dd HH:mm} | Page {{page}} of {{total}}",
            Order = int.MaxValue
        };
        Sections.Add(footer);
        LogMessage("📋 Footer generated");
    }

    protected virtual void ValidateDocument()
    {
        if (!Sections.Any(s => s.Type == SectionType.Content))
        {
            throw new InvalidOperationException("Document must contain at least one content section");
        }

        if (string.IsNullOrWhiteSpace(Metadata.Title))
        {
            throw new InvalidOperationException("Document must have a title");
        }

        LogMessage("✅ Document validation passed");
    }

    protected virtual GeneratedDocument HandleGenerationError(Exception ex, DocumentRequest request)
    {
        return new GeneratedDocument
        {
            Title = request.Title ?? "Error Document",
            Content = $"Error generating document: {ex.Message}",
            Metadata = Metadata,
            Success = false,
            Error = ex.Message,
            Logs = [..Logs]
        };
    }

    // Hook methods - control document structure
    protected virtual bool ShouldIncludeHeader(DocumentRequest request) => request.Options.IncludeHeader;

    protected virtual bool ShouldIncludeTableOfContents(DocumentRequest request) =>
        request.Options.IncludeTableOfContents;

    protected virtual bool ShouldIncludeAppendices(DocumentRequest request) => request.Data.ContainsKey("appendices");
    protected virtual bool ShouldIncludeFooter(DocumentRequest request) => request.Options.IncludeFooter;

    // Helper methods
    protected void AddSection(SectionType type, string title, string content)
    {
        Sections.Add(new DocumentSection
        {
            Type = type,
            Title = title,
            Content = content,
            Order = Sections.Count(s => s.Type == type)
        });
    }

    protected void LogMessage(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        var logEntry = $"[{timestamp}] {GetType().Name}: {message}";
        Logs.Add(logEntry);
        Console.WriteLine(logEntry);
    }
}