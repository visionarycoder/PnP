namespace Snippets.DesignPatterns.Behavioral.TemplateMethod;

/// <summary>
/// Number processing implementation
/// </summary>
public class NumberProcessor : DataProcessor<int>
{
    private int multiplier = 1;
    private int threshold = 0;

    protected override void Initialize(ProcessingOptions options)
    {
        base.Initialize(options);

        if (options.CustomOptions.TryGetValue("multiplier", out var mult))
        {
            multiplier = Convert.ToInt32(mult);
            LogMessage($"📊 Multiplier set to {multiplier}");
        }

        if (options.CustomOptions.TryGetValue("threshold", out var thresh))
        {
            threshold = Convert.ToInt32(thresh);
            LogMessage($"📊 Threshold set to {threshold}");
        }
    }

    protected override IEnumerable<int> ProcessDataCore(IEnumerable<int> data, ProcessingOptions options)
    {
        LogMessage("🔢 Processing numbers: multiplication and filtering");

        var processed = data
            .Select(x => x * multiplier)
            .Where(x => x > threshold);

        if (options.ParallelProcessing)
        {
            LogMessage("⚡ Using parallel processing");
            processed = processed.AsParallel();
        }

        return processed.ToList();
    }

    protected override bool IsValidInput(int item) => true; // Accept all integers

    protected override IEnumerable<int> PreProcessData(IEnumerable<int> data)
    {
        LogMessage("🔄 Pre-processing: removing duplicates and sorting");
        return data.Distinct().OrderBy(x => x);
    }

    protected override IEnumerable<int> PostProcessData(IEnumerable<int> data, ProcessingOptions options)
    {
        var limit = options.CustomOptions.TryGetValue("limit", out var l) ? Convert.ToInt32(l) : 10;
        LogMessage($"🔄 Post-processing: taking top {limit} values");
        return data.OrderByDescending(x => x).Take(limit);
    }
}