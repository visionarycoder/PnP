namespace Snippets.DesignPatterns.Behavioral.TemplateMethod;

/// <summary>
/// Business proposal generator
/// </summary>
public class BusinessProposalGenerator : DocumentGenerator
{
    protected override void GenerateContent(DocumentRequest request)
    {
        LogMessage("💼 Generating business proposal content");

        // Cover Letter
        AddSection(SectionType.Content, "Executive Summary",
            $"We are pleased to submit this proposal for {request.Subject}. " +
            "Our team brings extensive experience and innovative solutions to address your business needs.");

        // Problem Statement
        if (request.Data.TryGetValue("problem", out var problem))
        {
            AddSection(SectionType.Content, "Problem Statement", problem.ToString() ?? "");
        }

        // Proposed Solution
        if (request.Data.TryGetValue("solution", out var solution))
        {
            AddSection(SectionType.Content, "Proposed Solution", solution.ToString() ?? "");
        }

        // Timeline and Deliverables
        if (request.Data.TryGetValue("timeline", out var timeline))
        {
            AddSection(SectionType.Content, "Project Timeline", timeline.ToString() ?? "");
        }

        // Investment and ROI
        if (request.Data.TryGetValue("budget", out var budget))
        {
            AddSection(SectionType.Content, "Investment Analysis",
                $"Project Budget: {budget}\n" +
                "Expected ROI: 250% within 18 months\n" +
                "Break-even point: Month 8");
        }

        // Team and Qualifications
        AddSection(SectionType.Content, "Our Team",
            "Our experienced team includes certified professionals with proven track records in delivering " +
            "successful projects within budget and timeline constraints.");

        // Next Steps
        AddSection(SectionType.Content, "Next Steps",
            "1. Review and approve this proposal\n" +
            "2. Sign the project agreement\n" +
            "3. Begin project kickoff within 5 business days\n" +
            "4. Regular milestone reviews and progress updates");
    }

    protected override void ApplyFormatting()
    {
        Formatting.FontFamily = "Arial";
        Formatting.FontSize = 11;
        Formatting.Styles["heading1"] = "Bold, 14pt, DarkBlue";
        Formatting.Styles["heading2"] = "Bold, 12pt, Blue";
        Formatting.Styles["emphasis"] = "Bold, Italic";
        Formatting.Styles["callout"] = "Background: LightYellow, Border: 1pt";
        LogMessage("🎨 Applied business proposal formatting");
    }

    protected override GeneratedDocument FinalizeDocument()
    {
        var content = string.Join("\n\n", Sections.OrderBy(s => s.Order).Select(s =>
            $"## {s.Title}\n\n{s.Content}"));

        return new GeneratedDocument
        {
            Title = Metadata.Title,
            Content = content,
            Metadata = Metadata,
            PageCount = Math.Max(1, content.Length / 2200),
            Success = true,
            Logs = [..Logs],
            Formatting = Formatting
        };
    }
}