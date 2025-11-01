using System.Text.RegularExpressions;

namespace Snippets.DesignPatterns.Behavioral.TemplateMethod;

/// <summary>
/// Text processing implementation with advanced string operations
/// </summary>
public class TextProcessor : DataProcessor<string>
{
    private string prefix = "";
    private string suffix = "";
    private bool normalize = false;

    protected override void Initialize(ProcessingOptions options)
    {
        base.Initialize(options);

        if (options.CustomOptions.TryGetValue("prefix", out var prefix))
        {
            this.prefix = prefix.ToString() ?? "";
            LogMessage($"📝 Prefix set to: '{this.prefix}'");
        }

        if (options.CustomOptions.TryGetValue("suffix", out var suffix))
        {
            this.suffix = suffix.ToString() ?? "";
            LogMessage($"📝 Suffix set to: '{this.suffix}'");
        }

        normalize = options.CustomOptions.ContainsKey("normalize");
        if (normalize)
        {
            LogMessage("📝 Text normalization enabled");
        }
    }

    protected override IEnumerable<string> ProcessDataCore(IEnumerable<string> data, ProcessingOptions options)
    {
        LogMessage("📝 Processing text: applying transformations");

        return data.Select(text =>
        {
            var processed = text.Trim();

            // Apply case transformation
            if (options.CustomOptions.TryGetValue("caseTransform", out var caseType))
            {
                processed = caseType.ToString()?.ToLower() switch
                {
                    "upper" => processed.ToUpperInvariant(),
                    "lower" => processed.ToLowerInvariant(),
                    "title" => System.Globalization.CultureInfo.CurrentCulture.TextInfo
                        .ToTitleCase(processed.ToLower()),
                    _ => processed
                };
            }

            return $"{prefix}{processed}{suffix}";
        });
    }

    protected override bool IsValidInput(string item) => !string.IsNullOrWhiteSpace(item);
    protected override bool IsValidOutput(string item) => !string.IsNullOrEmpty(item);

    protected override IEnumerable<string> PreProcessData(IEnumerable<string> data)
    {
        if (!normalize) return data;

        LogMessage("🔄 Pre-processing: normalizing whitespace and removing empty entries");
        return data
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => Regex.Replace(s, @"\s+", " ").Trim());
    }

    protected override IEnumerable<string> PostProcessData(IEnumerable<string> data, ProcessingOptions options)
    {
        var minLength = options.CustomOptions.TryGetValue("minLength", out var min) ? Convert.ToInt32(min) : 1;
        var maxLength = options.CustomOptions.TryGetValue("maxLength", out var max)
            ? Convert.ToInt32(max)
            : int.MaxValue;

        LogMessage($"🔄 Post-processing: filtering by length ({minLength}-{maxLength})");
        return data.Where(s => s.Length >= minLength && s.Length <= maxLength);
    }
}