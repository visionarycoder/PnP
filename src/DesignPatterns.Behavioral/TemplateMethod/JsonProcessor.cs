using System.Text.Json;

namespace Snippets.DesignPatterns.Behavioral.TemplateMethod;

/// <summary>
/// JSON data processing implementation
/// </summary>
public class JsonProcessor : DataProcessor<string>
{
    private JsonSerializerOptions jsonOptions = new() { WriteIndented = true };

    protected override void Initialize(ProcessingOptions options)
    {
        base.Initialize(options);

        if (options.CustomOptions.TryGetValue("indented", out var indented))
        {
            jsonOptions.WriteIndented = Convert.ToBoolean(indented);
        }
    }

    protected override IEnumerable<string> ProcessDataCore(IEnumerable<string> data, ProcessingOptions options)
    {
        LogMessage("🔄 Processing JSON: parsing and reformatting");

        var results = new List<string>();

        foreach (var json in data)
        {
            try
            {
                // Parse and reformat JSON
                using var document = JsonDocument.Parse(json);
                var reformatted = JsonSerializer.Serialize(document.RootElement, jsonOptions);
                results.Add(reformatted);
            }
            catch (JsonException ex)
            {
                LogMessage($"⚠️ Invalid JSON skipped: {ex.Message}");
                // Skip invalid JSON
            }
        }

        return results;
    }

    protected override bool IsValidInput(string item)
    {
        if (string.IsNullOrWhiteSpace(item)) return false;

        try
        {
            JsonDocument.Parse(item);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    protected override IEnumerable<string> PostProcessData(IEnumerable<string> data, ProcessingOptions options)
    {
        if (options.CustomOptions.TryGetValue("minify", out var minify) && Convert.ToBoolean(minify))
        {
            LogMessage("🔄 Post-processing: minifying JSON");
            return data.Select(json =>
            {
                using var document = JsonDocument.Parse(json);
                return JsonSerializer.Serialize(document.RootElement,
                    new JsonSerializerOptions { WriteIndented = false });
            });
        }

        return data;
    }
}