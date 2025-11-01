namespace Snippets.DesignPatterns.Behavioral.TemplateMethod;

/// <summary>
/// Template Method Pattern Implementation
/// Defines the skeleton of an algorithm in a base class, letting subclasses override
/// specific steps without changing the algorithm's structure.
/// </summary>

#region Data Processing Pipeline

/// <summary>
/// Abstract base class for data processing with template method pattern
/// </summary>
public abstract class DataProcessor<T>
{
    protected List<string> Logs = [];
    protected DateTime StartTime;
    protected ProcessingStatistics Statistics = new();

    /// <summary>
    /// Template method that defines the data processing algorithm
    /// </summary>
    public ProcessingResult<T> ProcessData(IEnumerable<T> inputData, ProcessingOptions options)
    {
        try
        {
            StartTime = DateTime.UtcNow;
            Statistics.Reset();
            LogMessage("🚀 Starting data processing");

            // Step 1: Initialize processor
            Initialize(options);

            // Step 2: Validate input
            var validatedData = ValidateInput(inputData);
            if (!validatedData.Any())
            {
                LogMessage("⚠️ No valid data to process");
                return CreateResult([], ProcessingStatus.NoData);
            }

            Statistics.InputCount = validatedData.Count();

            // Step 3: Pre-process (optional)
            if (ShouldPreProcess(options))
            {
                validatedData = PreProcessData(validatedData);
                LogMessage($"✅ Pre-processing completed: {validatedData.Count()} items");
            }

            // Step 4: Main processing (abstract - must be implemented)
            var processedData = ProcessDataCore(validatedData, options);
            Statistics.ProcessedCount = processedData.Count();
            LogMessage($"⚡ Core processing completed: {processedData.Count()} items");

            // Step 5: Post-process (optional)
            if (ShouldPostProcess(options))
            {
                processedData = PostProcessData(processedData, options);
                LogMessage($"🔧 Post-processing completed: {processedData.Count()} items");
            }

            // Step 6: Validate output
            var finalData = ValidateOutput(processedData);
            Statistics.OutputCount = finalData.Count();
            LogMessage($"✅ Output validation completed: {finalData.Count()} valid items");

            // Step 7: Finalize
            Finalize(options);

            var duration = DateTime.UtcNow - StartTime;
            Statistics.ProcessingTime = duration;
            LogMessage($"🎉 Processing completed in {duration.TotalMilliseconds:F0}ms");

            return CreateResult(finalData, ProcessingStatus.Success);
        }
        catch (Exception ex)
        {
            LogMessage($"❌ Error during processing: {ex.Message}");
            HandleError(ex);
            return CreateResult([], ProcessingStatus.Error, ex);
        }
    }

    // Abstract methods - must be implemented by subclasses
    protected abstract IEnumerable<T> ProcessDataCore(IEnumerable<T> data, ProcessingOptions options);

    // Virtual methods with default implementations - can be overridden
    protected virtual void Initialize(ProcessingOptions options)
    {
        LogMessage("🔧 Initializing processor");
        Logs.Clear();
    }

    protected virtual IEnumerable<T> ValidateInput(IEnumerable<T> data)
    {
        var validItems = data.Where(item => IsValidInput(item)).ToList();
        LogMessage($"📋 Input validation: {validItems.Count}/{data.Count()} items valid");
        return validItems;
    }

    protected virtual IEnumerable<T> PreProcessData(IEnumerable<T> data)
    {
        LogMessage("🔄 Default pre-processing (no changes)");
        return data;
    }

    protected virtual IEnumerable<T> PostProcessData(IEnumerable<T> data, ProcessingOptions options)
    {
        LogMessage("🔄 Default post-processing (no changes)");
        return data;
    }

    protected virtual IEnumerable<T> ValidateOutput(IEnumerable<T> data)
    {
        var validItems = data.Where(item => IsValidOutput(item)).ToList();
        LogMessage($"📋 Output validation: {validItems.Count}/{data.Count()} items valid");
        return validItems;
    }

    protected virtual void Finalize(ProcessingOptions options)
    {
        LogMessage("🏁 Finalizing processor");
    }

    protected virtual void HandleError(Exception ex)
    {
        LogMessage($"⚠️ Handling error: {ex.GetType().Name}");
    }

    // Hook methods for conditional processing
    protected virtual bool ShouldPreProcess(ProcessingOptions options) => options.EnablePreProcessing;
    protected virtual bool ShouldPostProcess(ProcessingOptions options) => options.EnablePostProcessing;
    protected virtual bool IsValidInput(T item) => item != null;
    protected virtual bool IsValidOutput(T item) => item != null;

    // Helper methods
    protected void LogMessage(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        var logEntry = $"[{timestamp}] {GetType().Name}: {message}";
        Logs.Add(logEntry);
        Console.WriteLine(logEntry);
    }

    protected virtual ProcessingResult<T> CreateResult(IEnumerable<T> data, ProcessingStatus status,
        Exception? error = null)
    {
        return new ProcessingResult<T>
        {
            Data = data.ToList(),
            Status = status,
            Error = error,
            Statistics = Statistics,
            Logs = [..Logs]
        };
    }
}

#endregion

#region Document Generation System

#endregion

#region Game AI Template

#endregion

#region Supporting Classes

// Data Processing Supporting Classes

// Document Generation Supporting Classes

// Game AI Supporting Classes

#endregion