# Template Method Pattern

**Description**: Defines the skeleton of an algorithm in a base class, letting subclasses override specific steps without changing the algorithm's structure. The template method calls abstract methods and hooks that subclasses can implement or override to customize behavior while maintaining the overall algorithm flow.

**Language/Technology**: C#

**Code**:

## 1. Basic Template Method Structure

```csharp
// Abstract base class with template method
public abstract class AlgorithmTemplate
{
    // Template method - defines the algorithm skeleton
    public void Execute()
    {
        Initialize();
        
        if (ShouldPerformPreProcessing())
        {
            PreProcess();
        }
        
        var data = LoadData();
        var processedData = ProcessCore(data);
        
        if (ShouldPerformPostProcessing())
        {
            PostProcess(processedData);
        }
        
        SaveResults(processedData);
        Cleanup();
    }
    
    // Abstract methods - must be implemented by subclasses
    protected abstract object LoadData();
    protected abstract object ProcessCore(object data);
    protected abstract void SaveResults(object results);
    
    // Hook methods - default implementation, can be overridden
    protected virtual void Initialize() { }
    protected virtual void PreProcess() { }
    protected virtual void PostProcess(object results) { }
    protected virtual void Cleanup() { }
    
    // Conditional hooks - control algorithm flow
    protected virtual bool ShouldPerformPreProcessing() => true;
    protected virtual bool ShouldPerformPostProcessing() => true;
}
```

## 2. Data Processing Pipeline

```csharp
// Base data processor
public abstract class DataProcessor<T>
{
    protected List<string> _logs = new();
    protected DateTime _startTime;
    
    // Template method
    public ProcessingResult ProcessData(IEnumerable<T> inputData, ProcessingOptions options)
    {
        try
        {
            _startTime = DateTime.UtcNow;
            LogMessage("Starting data processing");
            
            // Step 1: Initialize
            Initialize(options);
            
            // Step 2: Validate input
            var validatedData = ValidateInput(inputData);
            if (!validatedData.Any())
            {
                LogMessage("No valid data to process");
                return CreateResult(Enumerable.Empty<T>(), ProcessingStatus.NoData);
            }
            
            // Step 3: Pre-process (optional)
            if (ShouldPreProcess(options))
            {
                validatedData = PreProcessData(validatedData);
                LogMessage($"Pre-processing completed. {validatedData.Count()} items");
            }
            
            // Step 4: Main processing
            var processedData = ProcessDataCore(validatedData, options);
            LogMessage($"Core processing completed. {processedData.Count()} items");
            
            // Step 5: Post-process (optional)
            if (ShouldPostProcess(options))
            {
                processedData = PostProcessData(processedData, options);
                LogMessage($"Post-processing completed. {processedData.Count()} items");
            }
            
            // Step 6: Validate output
            var finalData = ValidateOutput(processedData);
            LogMessage($"Output validation completed. {finalData.Count()} valid items");
            
            // Step 7: Finalize
            Finalize(options);
            
            var duration = DateTime.UtcNow - _startTime;
            LogMessage($"Processing completed in {duration.TotalMilliseconds:F0}ms");
            
            return CreateResult(finalData, ProcessingStatus.Success);
        }
        catch (Exception ex)
        {
            LogMessage($"Error during processing: {ex.Message}");
            HandleError(ex);
            return CreateResult(Enumerable.Empty<T>(), ProcessingStatus.Error, ex);
        }
    }
    
    // Abstract methods - must be implemented
    protected abstract IEnumerable<T> ProcessDataCore(IEnumerable<T> data, ProcessingOptions options);
    
    // Virtual methods with default implementations
    protected virtual void Initialize(ProcessingOptions options)
    {
        LogMessage("Initializing processor");
    }
    
    protected virtual IEnumerable<T> ValidateInput(IEnumerable<T> data)
    {
        var validItems = data.Where(item => IsValidInput(item)).ToList();
        LogMessage($"Input validation: {validItems.Count}/{data.Count()} items valid");
        return validItems;
    }
    
    protected virtual IEnumerable<T> PreProcessData(IEnumerable<T> data)
    {
        LogMessage("Default pre-processing (no changes)");
        return data;
    }
    
    protected virtual IEnumerable<T> PostProcessData(IEnumerable<T> data, ProcessingOptions options)
    {
        LogMessage("Default post-processing (no changes)");
        return data;
    }
    
    protected virtual IEnumerable<T> ValidateOutput(IEnumerable<T> data)
    {
        var validItems = data.Where(item => IsValidOutput(item)).ToList();
        LogMessage($"Output validation: {validItems.Count}/{data.Count()} items valid");
        return validItems;
    }
    
    protected virtual void Finalize(ProcessingOptions options)
    {
        LogMessage("Finalizing processor");
    }
    
    protected virtual void HandleError(Exception ex)
    {
        LogMessage($"Handling error: {ex.GetType().Name}");
    }
    
    // Hook methods for conditional processing
    protected virtual bool ShouldPreProcess(ProcessingOptions options) => true;
    protected virtual bool ShouldPostProcess(ProcessingOptions options) => true;
    protected virtual bool IsValidInput(T item) => item != null;
    protected virtual bool IsValidOutput(T item) => item != null;
    
    // Helper methods
    protected void LogMessage(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        var logEntry = $"[{timestamp}] {GetType().Name}: {message}";
        _logs.Add(logEntry);
        Console.WriteLine(logEntry);
    }
    
    protected virtual ProcessingResult CreateResult(IEnumerable<T> data, ProcessingStatus status, Exception? error = null)
    {
        return new ProcessingResult
        {
            Data = data.Cast<object>().ToList(),
            Status = status,
            Error = error,
            ProcessingTime = DateTime.UtcNow - _startTime,
            Logs = new List<string>(_logs)
        };
    }
}

// Supporting classes
public class ProcessingOptions
{
    public bool EnablePreProcessing { get; set; } = true;
    public bool EnablePostProcessing { get; set; } = true;
    public int BatchSize { get; set; } = 100;
    public bool ParallelProcessing { get; set; } = false;
    public Dictionary<string, object> CustomOptions { get; set; } = new();
}

public enum ProcessingStatus
{
    Success,
    NoData,
    PartialSuccess,
    Error
}

public class ProcessingResult
{
    public List<object> Data { get; set; } = new();
    public ProcessingStatus Status { get; set; }
    public Exception? Error { get; set; }
    public TimeSpan ProcessingTime { get; set; }
    public List<string> Logs { get; set; } = new();
}

// Concrete implementations
public class NumberProcessor : DataProcessor<int>
{
    private int _multiplier = 1;
    
    protected override void Initialize(ProcessingOptions options)
    {
        base.Initialize(options);
        
        if (options.CustomOptions.TryGetValue("multiplier", out var mult))
        {
            _multiplier = Convert.ToInt32(mult);
            LogMessage($"Multiplier set to {_multiplier}");
        }
    }
    
    protected override IEnumerable<int> ProcessDataCore(IEnumerable<int> data, ProcessingOptions options)
    {
        LogMessage("Processing numbers: multiplication and filtering");
        
        var processed = data.Select(x => x * _multiplier).Where(x => x > 0);
        
        if (options.ParallelProcessing)
        {
            processed = processed.AsParallel();
        }
        
        return processed.ToList();
    }
    
    protected override bool IsValidInput(int item) => item != 0;
    
    protected override IEnumerable<int> PreProcessData(IEnumerable<int> data)
    {
        LogMessage("Pre-processing: removing duplicates and sorting");
        return data.Distinct().OrderBy(x => x);
    }
    
    protected override IEnumerable<int> PostProcessData(IEnumerable<int> data, ProcessingOptions options)
    {
        LogMessage("Post-processing: taking top values");
        return data.OrderByDescending(x => x).Take(10);
    }
}

public class TextProcessor : DataProcessor<string>
{
    private string _prefix = "";
    private string _suffix = "";
    
    protected override void Initialize(ProcessingOptions options)
    {
        base.Initialize(options);
        
        if (options.CustomOptions.TryGetValue("prefix", out var prefix))
        {
            _prefix = prefix.ToString() ?? "";
        }
        
        if (options.CustomOptions.TryGetValue("suffix", out var suffix))
        {
            _suffix = suffix.ToString() ?? "";
        }
    }
    
    protected override IEnumerable<string> ProcessDataCore(IEnumerable<string> data, ProcessingOptions options)
    {
        LogMessage("Processing text: applying transformations");
        
        return data.Select(text => $"{_prefix}{text.Trim().ToUpperInvariant()}{_suffix}");
    }
    
    protected override bool IsValidInput(string item) => !string.IsNullOrWhiteSpace(item);
    protected override bool IsValidOutput(string item) => !string.IsNullOrEmpty(item);
    
    protected override IEnumerable<string> PreProcessData(IEnumerable<string> data)
    {
        LogMessage("Pre-processing: normalizing whitespace");
        return data.Select(s => System.Text.RegularExpressions.Regex.Replace(s, @"\s+", " ").Trim());
    }
    
    protected override IEnumerable<string> PostProcessData(IEnumerable<string> data, ProcessingOptions options)
    {
        LogMessage("Post-processing: filtering by length");
        return data.Where(s => s.Length >= 3 && s.Length <= 50);
    }
    
    protected override bool ShouldPreProcess(ProcessingOptions options)
    {
        return options.EnablePreProcessing && options.CustomOptions.ContainsKey("normalize");
    }
}
```

## 3. Document Generation Pipeline

```csharp
// Document generator template
public abstract class DocumentGenerator
{
    protected DocumentMetadata _metadata = new();
    protected List<DocumentSection> _sections = new();
    protected DocumentFormatting _formatting = new();
    
    // Template method
    public GeneratedDocument GenerateDocument(DocumentRequest request)
    {
        try
        {
            Console.WriteLine($"Starting document generation: {request.Title}");
            
            // Step 1: Initialize document
            InitializeDocument(request);
            
            // Step 2: Create metadata
            CreateMetadata(request);
            
            // Step 3: Generate header (if needed)
            if (ShouldIncludeHeader())
            {
                GenerateHeader(request);
            }
            
            // Step 4: Generate table of contents (if needed)
            if (ShouldIncludeTableOfContents())
            {
                GenerateTableOfContents();
            }
            
            // Step 5: Generate content sections
            GenerateContent(request);
            
            // Step 6: Generate footer (if needed)
            if (ShouldIncludeFooter())
            {
                GenerateFooter(request);
            }
            
            // Step 7: Apply formatting
            ApplyFormatting();
            
            // Step 8: Validate document
            ValidateDocument();
            
            // Step 9: Finalize
            var result = FinalizeDocument();
            
            Console.WriteLine($"Document generation completed: {result.PageCount} pages");
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Document generation failed: {ex.Message}");
            return HandleGenerationError(ex, request);
        }
    }
    
    // Abstract methods
    protected abstract void GenerateContent(DocumentRequest request);
    protected abstract void ApplyFormatting();
    protected abstract GeneratedDocument FinalizeDocument();
    
    // Virtual methods with default behavior
    protected virtual void InitializeDocument(DocumentRequest request)
    {
        _sections.Clear();
        _formatting = new DocumentFormatting();
        Console.WriteLine("Document initialized");
    }
    
    protected virtual void CreateMetadata(DocumentRequest request)
    {
        _metadata = new DocumentMetadata
        {
            Title = request.Title,
            Author = request.Author ?? "Unknown",
            CreatedDate = DateTime.UtcNow,
            Version = "1.0",
            Subject = request.Subject
        };
        Console.WriteLine($"Metadata created for: {_metadata.Title}");
    }
    
    protected virtual void GenerateHeader(DocumentRequest request)
    {
        var header = new DocumentSection
        {
            Type = SectionType.Header,
            Title = _metadata.Title,
            Content = $"Document: {_metadata.Title}\nAuthor: {_metadata.Author}\nDate: {_metadata.CreatedDate:yyyy-MM-dd}"
        };
        _sections.Insert(0, header);
        Console.WriteLine("Header generated");
    }
    
    protected virtual void GenerateTableOfContents()
    {
        var tocSection = new DocumentSection
        {
            Type = SectionType.TableOfContents,
            Title = "Table of Contents",
            Content = "Table of contents will be generated based on sections"
        };
        _sections.Add(tocSection);
        Console.WriteLine("Table of contents placeholder added");
    }
    
    protected virtual void GenerateFooter(DocumentRequest request)
    {
        var footer = new DocumentSection
        {
            Type = SectionType.Footer,
            Title = "Footer",
            Content = $"Generated on {DateTime.UtcNow:yyyy-MM-dd HH:mm} | Page {{page}} of {{total}}"
        };
        _sections.Add(footer);
        Console.WriteLine("Footer generated");
    }
    
    protected virtual void ValidateDocument()
    {
        if (!_sections.Any(s => s.Type == SectionType.Content))
        {
            throw new InvalidOperationException("Document must contain at least one content section");
        }
        
        if (string.IsNullOrWhiteSpace(_metadata.Title))
        {
            throw new InvalidOperationException("Document must have a title");
        }
        
        Console.WriteLine("Document validation passed");
    }
    
    protected virtual GeneratedDocument HandleGenerationError(Exception ex, DocumentRequest request)
    {
        return new GeneratedDocument
        {
            Title = request.Title ?? "Error Document",
            Content = $"Error generating document: {ex.Message}",
            Metadata = _metadata,
            Success = false,
            Error = ex.Message
        };
    }
    
    // Hook methods
    protected virtual bool ShouldIncludeHeader() => true;
    protected virtual bool ShouldIncludeTableOfContents() => false;
    protected virtual bool ShouldIncludeFooter() => true;
    
    // Helper methods
    protected void AddSection(SectionType type, string title, string content)
    {
        _sections.Add(new DocumentSection
        {
            Type = type,
            Title = title,
            Content = content,
            Order = _sections.Count
        });
    }
}

// Supporting classes
public class DocumentRequest
{
    public string? Title { get; set; }
    public string? Author { get; set; }
    public string? Subject { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
    public DocumentOptions Options { get; set; } = new();
}

public class DocumentOptions
{
    public bool IncludeHeader { get; set; } = true;
    public bool IncludeFooter { get; set; } = true;
    public bool IncludeTableOfContents { get; set; } = false;
    public string Format { get; set; } = "PDF";
    public Dictionary<string, object> CustomOptions { get; set; } = new();
}

public class DocumentMetadata
{
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
    public DateTime CreatedDate { get; set; }
    public string Version { get; set; } = "1.0";
    public string Subject { get; set; } = "";
}

public class DocumentSection
{
    public SectionType Type { get; set; }
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public int Order { get; set; }
}

public enum SectionType
{
    Header,
    TableOfContents,
    Content,
    Appendix,
    Footer
}

public class DocumentFormatting
{
    public string FontFamily { get; set; } = "Arial";
    public int FontSize { get; set; } = 12;
    public string PageSize { get; set; } = "A4";
    public Dictionary<string, object> Styles { get; set; } = new();
}

public class GeneratedDocument
{
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public DocumentMetadata Metadata { get; set; } = new();
    public int PageCount { get; set; }
    public bool Success { get; set; } = true;
    public string? Error { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

// Concrete implementations
public class ReportGenerator : DocumentGenerator
{
    protected override bool ShouldIncludeTableOfContents() => true;
    
    protected override void GenerateContent(DocumentRequest request)
    {
        Console.WriteLine("Generating report content");
        
        // Executive Summary
        AddSection(SectionType.Content, "Executive Summary", 
            "This report provides an overview of the key findings and recommendations.");
        
        // Data sections
        if (request.Data.TryGetValue("charts", out var charts))
        {
            AddSection(SectionType.Content, "Charts and Graphs",
                $"This section contains {charts} charts and visualizations.");
        }
        
        if (request.Data.TryGetValue("analysis", out var analysis))
        {
            AddSection(SectionType.Content, "Data Analysis",
                $"Analysis results: {analysis}");
        }
        
        // Recommendations
        AddSection(SectionType.Content, "Recommendations",
            "Based on the analysis, the following recommendations are proposed...");
    }
    
    protected override void ApplyFormatting()
    {
        _formatting.FontFamily = "Times New Roman";
        _formatting.FontSize = 11;
        _formatting.Styles["heading"] = "Bold, 14pt";
        _formatting.Styles["subheading"] = "Bold, 12pt";
        Console.WriteLine("Applied report formatting");
    }
    
    protected override GeneratedDocument FinalizeDocument()
    {
        var content = string.Join("\n\n", _sections.Select(s => 
            $"{s.Title}\n{new string('=', s.Title.Length)}\n{s.Content}"));
        
        return new GeneratedDocument
        {
            Title = _metadata.Title,
            Content = content,
            Metadata = _metadata,
            PageCount = Math.Max(1, content.Length / 2000), // Rough estimation
            Success = true
        };
    }
}

public class InvoiceGenerator : DocumentGenerator
{
    protected override bool ShouldIncludeTableOfContents() => false;
    
    protected override void GenerateContent(DocumentRequest request)
    {
        Console.WriteLine("Generating invoice content");
        
        // Invoice details
        if (request.Data.TryGetValue("invoiceNumber", out var invNum))
        {
            AddSection(SectionType.Content, "Invoice Information",
                $"Invoice Number: {invNum}\nDate: {DateTime.UtcNow:yyyy-MM-dd}");
        }
        
        // Billing information
        if (request.Data.TryGetValue("billingAddress", out var billing))
        {
            AddSection(SectionType.Content, "Bill To", billing.ToString() ?? "");
        }
        
        // Line items
        if (request.Data.TryGetValue("items", out var items))
        {
            AddSection(SectionType.Content, "Items", 
                $"Line items: {items}");
        }
        
        // Totals
        if (request.Data.TryGetValue("total", out var total))
        {
            AddSection(SectionType.Content, "Total Amount",
                $"Total: ${total}");
        }
    }
    
    protected override void ApplyFormatting()
    {
        _formatting.FontFamily = "Arial";
        _formatting.FontSize = 10;
        _formatting.Styles["table"] = "Border, 1pt";
        _formatting.Styles["total"] = "Bold, 12pt";
        Console.WriteLine("Applied invoice formatting");
    }
    
    protected override GeneratedDocument FinalizeDocument()
    {
        var content = string.Join("\n", _sections.Select(s => 
            $"{s.Title}: {s.Content}"));
        
        return new GeneratedDocument
        {
            Title = _metadata.Title,
            Content = content,
            Metadata = _metadata,
            PageCount = 1, // Invoices are typically single page
            Success = true
        };
    }
}

public class LetterGenerator : DocumentGenerator
{
    protected override bool ShouldIncludeHeader() => false;
    protected override bool ShouldIncludeFooter() => false;
    
    protected override void GenerateContent(DocumentRequest request)
    {
        Console.WriteLine("Generating letter content");
        
        // Date
        AddSection(SectionType.Content, "", DateTime.UtcNow.ToString("MMMM dd, yyyy"));
        
        // Recipient address
        if (request.Data.TryGetValue("recipientAddress", out var address))
        {
            AddSection(SectionType.Content, "", address.ToString() ?? "");
        }
        
        // Salutation
        var salutation = request.Data.TryGetValue("salutation", out var sal) 
            ? sal.ToString() : "Dear Sir/Madam";
        AddSection(SectionType.Content, "", $"{salutation},");
        
        // Body
        if (request.Data.TryGetValue("body", out var body))
        {
            AddSection(SectionType.Content, "", body.ToString() ?? "");
        }
        
        // Closing
        var closing = request.Data.TryGetValue("closing", out var close) 
            ? close.ToString() : "Sincerely";
        AddSection(SectionType.Content, "", $"{closing},\n\n{_metadata.Author}");
    }
    
    protected override void ApplyFormatting()
    {
        _formatting.FontFamily = "Calibri";
        _formatting.FontSize = 11;
        _formatting.Styles["paragraph"] = "Justified, 1.5 line spacing";
        Console.WriteLine("Applied letter formatting");
    }
    
    protected override GeneratedDocument FinalizeDocument()
    {
        var content = string.Join("\n\n", _sections.Select(s => s.Content));
        
        return new GeneratedDocument
        {
            Title = _metadata.Title,
            Content = content,
            Metadata = _metadata,
            PageCount = Math.Max(1, content.Length / 1500),
            Success = true
        };
    }
}
```

## 4. Test Framework Template

```csharp
// Test runner template
public abstract class TestRunner<T>
{
    protected List<TestResult> _results = new();
    protected TestConfiguration _config = new();
    protected DateTime _startTime;
    
    // Template method
    public TestSuiteResult RunTests(IEnumerable<T> testCases, TestConfiguration config)
    {
        _config = config;
        _results.Clear();
        _startTime = DateTime.UtcNow;
        
        try
        {
            Console.WriteLine($"Starting test execution with {testCases.Count()} test cases");
            
            // Step 1: Setup
            SetupTestEnvironment();
            
            // Step 2: Initialize
            if (ShouldInitializeTestData())
            {
                InitializeTestData();
            }
            
            // Step 3: Run tests
            foreach (var testCase in testCases)
            {
                var result = RunSingleTest(testCase);
                _results.Add(result);
                
                if (result.Status == TestStatus.Failed && _config.StopOnFirstFailure)
                {
                    Console.WriteLine("Stopping test execution due to failure");
                    break;
                }
            }
            
            // Step 4: Generate report
            var report = GenerateTestReport();
            
            // Step 5: Cleanup
            CleanupTestEnvironment();
            
            Console.WriteLine($"Test execution completed. {_results.Count(r => r.Status == TestStatus.Passed)} passed, {_results.Count(r => r.Status == TestStatus.Failed)} failed");
            
            return new TestSuiteResult
            {
                Results = _results,
                Report = report,
                TotalTime = DateTime.UtcNow - _startTime,
                Success = !_results.Any(r => r.Status == TestStatus.Failed)
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Test execution failed: {ex.Message}");
            return HandleTestExecutionError(ex);
        }
    }
    
    // Abstract methods
    protected abstract void SetupTestEnvironment();
    protected abstract TestResult ExecuteTest(T testCase);
    protected abstract void CleanupTestEnvironment();
    
    // Virtual methods with default implementations
    protected virtual void InitializeTestData()
    {
        Console.WriteLine("Initializing test data");
    }
    
    protected virtual TestResult RunSingleTest(T testCase)
    {
        var testStart = DateTime.UtcNow;
        Console.WriteLine($"Running test: {GetTestName(testCase)}");
        
        try
        {
            // Pre-test hook
            PreTestExecution(testCase);
            
            // Execute test
            var result = ExecuteTest(testCase);
            result.ExecutionTime = DateTime.UtcNow - testStart;
            
            // Post-test hook
            PostTestExecution(testCase, result);
            
            Console.WriteLine($"Test {GetTestName(testCase)}: {result.Status} ({result.ExecutionTime.TotalMilliseconds:F0}ms)");
            return result;
        }
        catch (Exception ex)
        {
            var failureResult = new TestResult
            {
                TestName = GetTestName(testCase),
                Status = TestStatus.Failed,
                ErrorMessage = ex.Message,
                ExecutionTime = DateTime.UtcNow - testStart
            };
            
            PostTestExecution(testCase, failureResult);
            return failureResult;
        }
    }
    
    protected virtual void PreTestExecution(T testCase)
    {
        // Hook for test setup
    }
    
    protected virtual void PostTestExecution(T testCase, TestResult result)
    {
        // Hook for test cleanup
        if (_config.VerboseOutput)
        {
            Console.WriteLine($"  Result: {result.Status}, Time: {result.ExecutionTime.TotalMilliseconds:F0}ms");
            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                Console.WriteLine($"  Error: {result.ErrorMessage}");
            }
        }
    }
    
    protected virtual string GenerateTestReport()
    {
        var passed = _results.Count(r => r.Status == TestStatus.Passed);
        var failed = _results.Count(r => r.Status == TestStatus.Failed);
        var skipped = _results.Count(r => r.Status == TestStatus.Skipped);
        var totalTime = DateTime.UtcNow - _startTime;
        
        var report = $"Test Execution Report\n";
        report += $"=====================\n";
        report += $"Total Tests: {_results.Count}\n";
        report += $"Passed: {passed}\n";
        report += $"Failed: {failed}\n";
        report += $"Skipped: {skipped}\n";
        report += $"Total Time: {totalTime.TotalSeconds:F2}s\n";
        
        if (failed > 0)
        {
            report += $"\nFailed Tests:\n";
            foreach (var failure in _results.Where(r => r.Status == TestStatus.Failed))
            {
                report += $"- {failure.TestName}: {failure.ErrorMessage}\n";
            }
        }
        
        return report;
    }
    
    protected virtual TestSuiteResult HandleTestExecutionError(Exception ex)
    {
        return new TestSuiteResult
        {
            Results = _results,
            Report = $"Test execution failed: {ex.Message}",
            TotalTime = DateTime.UtcNow - _startTime,
            Success = false,
            Error = ex.Message
        };
    }
    
    // Hook methods
    protected virtual bool ShouldInitializeTestData() => _config.InitializeTestData;
    protected virtual string GetTestName(T testCase) => testCase?.ToString() ?? "Unknown Test";
    
    // Helper methods
    protected void LogTestMessage(string message)
    {
        if (_config.VerboseOutput)
        {
            Console.WriteLine($"  {message}");
        }
    }
}

// Supporting classes
public class TestConfiguration
{
    public bool StopOnFirstFailure { get; set; } = false;
    public bool VerboseOutput { get; set; } = true;
    public bool InitializeTestData { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 30;
    public Dictionary<string, object> Properties { get; set; } = new();
}

public enum TestStatus
{
    Passed,
    Failed,
    Skipped
}

public class TestResult
{
    public string TestName { get; set; } = "";
    public TestStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan ExecutionTime { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
}

public class TestSuiteResult
{
    public List<TestResult> Results { get; set; } = new();
    public string Report { get; set; } = "";
    public TimeSpan TotalTime { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
}

// Concrete test runners
public class UnitTestRunner : TestRunner<UnitTestCase>
{
    private Dictionary<string, object> _testData = new();
    
    protected override void SetupTestEnvironment()
    {
        Console.WriteLine("Setting up unit test environment");
        _testData.Clear();
    }
    
    protected override void InitializeTestData()
    {
        base.InitializeTestData();
        _testData["sampleData"] = "Unit test data";
        _testData["mockServices"] = new List<string> { "Service1", "Service2" };
    }
    
    protected override TestResult ExecuteTest(UnitTestCase testCase)
    {
        LogTestMessage($"Executing unit test: {testCase.MethodName}");
        
        var result = new TestResult { TestName = testCase.MethodName };
        
        try
        {
            // Simulate test execution
            if (testCase.ShouldFail)
            {
                throw new Exception($"Test {testCase.MethodName} intentionally failed");
            }
            
            // Simulate some work
            Thread.Sleep(Random.Shared.Next(10, 100));
            
            result.Status = TestStatus.Passed;
            result.Data["assertions"] = testCase.AssertionCount;
        }
        catch (Exception ex)
        {
            result.Status = TestStatus.Failed;
            result.ErrorMessage = ex.Message;
        }
        
        return result;
    }
    
    protected override void CleanupTestEnvironment()
    {
        Console.WriteLine("Cleaning up unit test environment");
        _testData.Clear();
    }
    
    protected override void PreTestExecution(UnitTestCase testCase)
    {
        LogTestMessage($"Preparing test {testCase.MethodName}");
        // Setup test-specific data
    }
    
    protected override void PostTestExecution(UnitTestCase testCase, TestResult result)
    {
        base.PostTestExecution(testCase, result);
        LogTestMessage($"Cleaning up test {testCase.MethodName}");
        // Cleanup test-specific data
    }
}

public class IntegrationTestRunner : TestRunner<IntegrationTestCase>
{
    private string _connectionString = "";
    
    protected override void SetupTestEnvironment()
    {
        Console.WriteLine("Setting up integration test environment");
        _connectionString = _config.Properties.TryGetValue("connectionString", out var cs) 
            ? cs.ToString() ?? "" : "DefaultConnectionString";
    }
    
    protected override void InitializeTestData()
    {
        base.InitializeTestData();
        Console.WriteLine("Initializing integration test database");
        // Simulate database setup
    }
    
    protected override TestResult ExecuteTest(IntegrationTestCase testCase)
    {
        LogTestMessage($"Executing integration test: {testCase.TestName}");
        
        var result = new TestResult { TestName = testCase.TestName };
        
        try
        {
            // Simulate integration test execution
            LogTestMessage($"Connecting to: {testCase.ServiceEndpoint}");
            
            // Simulate network delay
            Thread.Sleep(Random.Shared.Next(100, 500));
            
            if (testCase.ServiceEndpoint.Contains("fail"))
            {
                throw new Exception($"Service at {testCase.ServiceEndpoint} is not available");
            }
            
            result.Status = TestStatus.Passed;
            result.Data["serviceResponse"] = "Success";
            result.Data["responseTime"] = Random.Shared.Next(50, 200);
        }
        catch (Exception ex)
        {
            result.Status = TestStatus.Failed;
            result.ErrorMessage = ex.Message;
        }
        
        return result;
    }
    
    protected override void CleanupTestEnvironment()
    {
        Console.WriteLine("Cleaning up integration test environment");
        Console.WriteLine("Clearing test database");
    }
    
    protected override bool ShouldInitializeTestData()
    {
        return base.ShouldInitializeTestData() && 
               _config.Properties.ContainsKey("useTestDatabase");
    }
}

// Test case classes
public class UnitTestCase
{
    public string MethodName { get; set; } = "";
    public bool ShouldFail { get; set; } = false;
    public int AssertionCount { get; set; } = 1;
}

public class IntegrationTestCase
{
    public string TestName { get; set; } = "";
    public string ServiceEndpoint { get; set; } = "";
    public Dictionary<string, object> Parameters { get; set; } = new();
}
```

**Usage**:

```csharp
// 1. Data Processing Example
var numberProcessor = new NumberProcessor();
var textProcessor = new TextProcessor();

var numbers = new[] { 1, -2, 3, 0, 5, -1, 2, 3 };
var texts = new[] { "  hello  ", "WORLD", "", "  test  data  ", "sample" };

var numberOptions = new ProcessingOptions
{
    ParallelProcessing = true,
    CustomOptions = { ["multiplier"] = 2 }
};

var textOptions = new ProcessingOptions
{
    CustomOptions = 
    { 
        ["prefix"] = ">>> ",
        ["suffix"] = " <<<",
        ["normalize"] = true
    }
};

var numberResult = numberProcessor.ProcessData(numbers, numberOptions);
var textResult = textProcessor.ProcessData(texts, textOptions);

Console.WriteLine($"Number processing: {numberResult.Status}, {numberResult.Data.Count} items");
Console.WriteLine($"Text processing: {textResult.Status}, {textResult.Data.Count} items");

// 2. Document Generation Example
var reportGen = new ReportGenerator();
var invoiceGen = new InvoiceGenerator();
var letterGen = new LetterGenerator();

var reportRequest = new DocumentRequest
{
    Title = "Monthly Sales Report",
    Author = "Sales Team",
    Subject = "Sales Analysis",
    Data = 
    {
        ["charts"] = 5,
        ["analysis"] = "Revenue increased by 15%"
    }
};

var invoiceRequest = new DocumentRequest
{
    Title = "Invoice #12345",
    Author = "Billing Department",
    Data = 
    {
        ["invoiceNumber"] = "INV-12345",
        ["billingAddress"] = "123 Main St, City, State",
        ["items"] = "Product A x2, Product B x1",
        ["total"] = 150.00m
    }
};

var letterRequest = new DocumentRequest
{
    Title = "Business Letter",
    Author = "John Smith",
    Data = 
    {
        ["recipientAddress"] = "Jane Doe\n456 Oak Ave\nTown, State",
        ["salutation"] = "Dear Ms. Doe",
        ["body"] = "Thank you for your interest in our services...",
        ["closing"] = "Best regards"
    }
};

var report = reportGen.GenerateDocument(reportRequest);
var invoice = invoiceGen.GenerateDocument(invoiceRequest);
var letter = letterGen.GenerateDocument(letterRequest);

Console.WriteLine($"Report: {report.Success}, {report.PageCount} pages");
Console.WriteLine($"Invoice: {invoice.Success}, {invoice.PageCount} pages");
Console.WriteLine($"Letter: {letter.Success}, {letter.PageCount} pages");

// 3. Test Framework Example
var unitRunner = new UnitTestRunner();
var integrationRunner = new IntegrationTestRunner();

var unitTests = new[]
{
    new UnitTestCase { MethodName = "TestAddition", AssertionCount = 3 },
    new UnitTestCase { MethodName = "TestSubtraction", AssertionCount = 2 },
    new UnitTestCase { MethodName = "TestDivision", ShouldFail = true, AssertionCount = 1 },
    new UnitTestCase { MethodName = "TestMultiplication", AssertionCount = 2 }
};

var integrationTests = new[]
{
    new IntegrationTestCase { TestName = "TestUserService", ServiceEndpoint = "http://localhost:8080/users" },
    new IntegrationTestCase { TestName = "TestPaymentService", ServiceEndpoint = "http://localhost:8080/payments" },
    new IntegrationTestCase { TestName = "TestFailingService", ServiceEndpoint = "http://localhost:8080/fail" }
};

var unitConfig = new TestConfiguration
{
    VerboseOutput = true,
    StopOnFirstFailure = false
};

var integrationConfig = new TestConfiguration
{
    VerboseOutput = false,
    Properties = { ["connectionString"] = "Server=localhost;Database=TestDB", ["useTestDatabase"] = true }
};

var unitResults = unitRunner.RunTests(unitTests, unitConfig);
var integrationResults = integrationRunner.RunTests(integrationTests, integrationConfig);

Console.WriteLine("\nUnit Test Report:");
Console.WriteLine(unitResults.Report);
Console.WriteLine("\nIntegration Test Report:");
Console.WriteLine(integrationResults.Report);

// Expected output demonstrates:
// - Template method defining algorithm skeleton
// - Abstract and virtual method customization points
// - Hook methods controlling algorithm flow
// - Consistent processing pipeline with customizable steps
// - Error handling and validation within template structure
// - Logging and reporting integrated into algorithm flow
```

**Notes**:

- **Algorithm Invariant**: Template method ensures algorithm steps execute in correct order
- **Customization Points**: Abstract methods force implementation, virtual methods allow optional customization
- **Hook Methods**: Control algorithm flow with boolean conditions
- **Code Reuse**: Common algorithm structure shared across subclasses
- **Inversion of Control**: Base class controls flow, subclasses provide specific implementation
- **Open/Closed Principle**: Open for extension via subclassing, closed for modification
- **Single Responsibility**: Each method handles one specific step of the algorithm
- **Error Handling**: Template can provide consistent error handling across implementations

**Prerequisites**:

- .NET 6.0 or later for modern C# features
- Understanding of inheritance and polymorphism
- Knowledge of abstract classes and virtual methods
- Familiarity with the Hollywood Principle ("Don't call us, we'll call you")

**Related Patterns**:

- **Strategy**: Template Method uses inheritance, Strategy uses composition
- **Factory Method**: Often used within template methods for object creation
- **Observer**: Template methods can notify observers at specific steps
- **Command**: Steps in template method can be implemented as commands

