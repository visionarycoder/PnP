namespace Snippets.DesignPatterns.Behavioral.TemplateMethod;

/// <summary>
/// Technical report generator
/// </summary>
public class TechnicalReportGenerator : DocumentGenerator
{
    protected override bool ShouldIncludeTableOfContents(DocumentRequest request) => true;

    protected override void GenerateContent(DocumentRequest request)
    {
        LogMessage("📊 Generating technical report content");

        // Executive Summary
        AddSection(SectionType.Content, "Executive Summary",
            "This technical report provides a comprehensive analysis of the system requirements, " +
            "implementation details, and performance metrics.");

        // Technical Specifications
        if (request.Data.TryGetValue("specifications", out var specs))
        {
            AddSection(SectionType.Content, "Technical Specifications", specs.ToString() ?? "");
        }

        // Performance Analysis
        if (request.Data.TryGetValue("performance", out var perf))
        {
            AddSection(SectionType.Content, "Performance Analysis",
                $"System Performance Metrics:\n{perf}");
        }

        // Architecture Overview
        if (request.Data.TryGetValue("architecture", out var arch))
        {
            AddSection(SectionType.Content, "Architecture Overview", arch.ToString() ?? "");
        }

        // Recommendations
        AddSection(SectionType.Content, "Recommendations",
            "Based on the technical analysis, the following recommendations are proposed:\n" +
            "1. Optimize database queries for better performance\n" +
            "2. Implement caching strategies for frequently accessed data\n" +
            "3. Consider horizontal scaling for high-load scenarios");

        // Appendix
        if (request.Data.TryGetValue("codeExamples", out var code))
        {
            AddSection(SectionType.Appendix, "Code Examples", code.ToString() ?? "");
        }
    }

    protected override void ApplyFormatting()
    {
        Formatting.FontFamily = "Calibri";
        Formatting.FontSize = 11;
        Formatting.Styles["heading1"] = "Bold, 16pt, Blue";
        Formatting.Styles["heading2"] = "Bold, 14pt, DarkBlue";
        Formatting.Styles["code"] = "Courier New, 9pt, Background: LightGray";
        Formatting.Styles["table"] = "Border: 1pt solid black";
        LogMessage("🎨 Applied technical report formatting");
    }

    protected override GeneratedDocument FinalizeDocument()
    {
        var orderedSections = Sections.OrderBy(s => s.Order).ThenBy(s => s.Type);
        var content = string.Join("\n\n", orderedSections.Select(s =>
            $"{s.Title}\n{new string('=', s.Title.Length)}\n{s.Content}"));

        return new GeneratedDocument
        {
            Title = Metadata.Title,
            Content = content,
            Metadata = Metadata,
            PageCount = Math.Max(1, content.Length / 2500), // Rough estimation
            Success = true,
            Logs = [..Logs],
            Formatting = Formatting
        };
    }
}