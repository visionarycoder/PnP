# Decorator Pattern

**Description**: Attaches additional responsibilities to an object dynamically. Decorators provide a flexible alternative to subclassing for extending functionality. Useful for adding features like formatting, encryption, compression, or validation without modifying the original object.

**Language/Technology**: C# / .NET

**Code**:

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

// Component interface - defines the interface for objects that can have responsibilities added dynamically
public interface ITextProcessor
{
    string Process(string text);
    string GetDescription();
    int GetProcessingCost();
}

// Concrete Component - defines an object to which additional responsibilities can be attached
public class BasicTextProcessor : ITextProcessor
{
    public string Process(string text)
    {
        return text ?? string.Empty;
    }
    
    public string GetDescription()
    {
        return "Basic Text";
    }
    
    public int GetProcessingCost()
    {
        return 1;
    }
}

// Base Decorator - maintains a reference to a Component object and defines interface that conforms to Component
public abstract class TextDecorator : ITextProcessor
{
    protected ITextProcessor _textProcessor;
    
    protected TextDecorator(ITextProcessor textProcessor)
    {
        _textProcessor = textProcessor ?? throw new ArgumentNullException(nameof(textProcessor));
    }
    
    public virtual string Process(string text)
    {
        return _textProcessor.Process(text);
    }
    
    public virtual string GetDescription()
    {
        return _textProcessor.GetDescription();
    }
    
    public virtual int GetProcessingCost()
    {
        return _textProcessor.GetProcessingCost();
    }
}

// Concrete Decorators - add responsibilities to the component
public class UpperCaseDecorator : TextDecorator
{
    public UpperCaseDecorator(ITextProcessor textProcessor) : base(textProcessor)
    {
    }
    
    public override string Process(string text)
    {
        var processed = base.Process(text);
        return processed.ToUpperCase();
    }
    
    public override string GetDescription()
    {
        return $"{base.GetDescription()} + UpperCase";
    }
    
    public override int GetProcessingCost()
    {
        return base.GetProcessingCost() + 2;
    }
}

public class LowerCaseDecorator : TextDecorator
{
    public LowerCaseDecorator(ITextProcessor textProcessor) : base(textProcessor)
    {
    }
    
    public override string Process(string text)
    {
        var processed = base.Process(text);
        return processed.ToLowerCase();
    }
    
    public override string GetDescription()
    {
        return $"{base.GetDescription()} + LowerCase";
    }
    
    public override int GetProcessingCost()
    {
        return base.GetProcessingCost() + 2;
    }
}

public class TrimDecorator : TextDecorator
{
    public TrimDecorator(ITextProcessor textProcessor) : base(textProcessor)
    {
    }
    
    public override string Process(string text)
    {
        var processed = base.Process(text);
        return processed.Trim();
    }
    
    public override string GetDescription()
    {
        return $"{base.GetDescription()} + Trim";
    }
    
    public override int GetProcessingCost()
    {
        return base.GetProcessingCost() + 1;
    }
}

public class ReverseDecorator : TextDecorator
{
    public ReverseDecorator(ITextProcessor textProcessor) : base(textProcessor)
    {
    }
    
    public override string Process(string text)
    {
        var processed = base.Process(text);
        var charArray = processed.ToCharArray();
        Array.Reverse(charArray);
        return new string(charArray);
    }
    
    public override string GetDescription()
    {
        return $"{base.GetDescription()} + Reverse";
    }
    
    public override int GetProcessingCost()
    {
        return base.GetProcessingCost() + 3;
    }
}

public class EncryptDecorator : TextDecorator
{
    private readonly string _key;
    
    public EncryptDecorator(ITextProcessor textProcessor, string key = "DefaultKey123") : base(textProcessor)
    {
        _key = key ?? "DefaultKey123";
    }
    
    public override string Process(string text)
    {
        var processed = base.Process(text);
        return SimpleEncrypt(processed, _key);
    }
    
    public override string GetDescription()
    {
        return $"{base.GetDescription()} + Encrypt";
    }
    
    public override int GetProcessingCost()
    {
        return base.GetProcessingCost() + 10;
    }
    
    private string SimpleEncrypt(string text, string key)
    {
        // Simple XOR encryption for demo purposes
        var result = new StringBuilder();
        for (int i = 0; i < text.Length; i++)
        {
            result.Append((char)(text[i] ^ key[i % key.Length]));
        }
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(result.ToString()));
    }
}

public class CompressDecorator : TextDecorator
{
    public CompressDecorator(ITextProcessor textProcessor) : base(textProcessor)
    {
    }
    
    public override string Process(string text)
    {
        var processed = base.Process(text);
        return Compress(processed);
    }
    
    public override string GetDescription()
    {
        return $"{base.GetDescription()} + Compress";
    }
    
    public override int GetProcessingCost()
    {
        return base.GetProcessingCost() + 8;
    }
    
    private string Compress(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionMode.Compress))
        {
            gzip.Write(bytes, 0, bytes.Length);
        }
        return Convert.ToBase64String(output.ToArray());
    }
}

// Advanced Decorator Example: Coffee Shop
public interface IBeverage
{
    string GetDescription();
    decimal GetCost();
    int GetCalories();
    string GetSize();
}

public class Espresso : IBeverage
{
    private readonly string _size;
    
    public Espresso(string size = "Medium")
    {
        _size = size;
    }
    
    public string GetDescription() => $"{_size} Espresso";
    
    public decimal GetCost() => _size switch
    {
        "Small" => 1.99m,
        "Medium" => 2.49m,
        "Large" => 2.99m,
        _ => 2.49m
    };
    
    public int GetCalories() => _size switch
    {
        "Small" => 5,
        "Medium" => 8,
        "Large" => 12,
        _ => 8
    };
    
    public string GetSize() => _size;
}

public class HouseBlend : IBeverage
{
    private readonly string _size;
    
    public HouseBlend(string size = "Medium")
    {
        _size = size;
    }
    
    public string GetDescription() => $"{_size} House Blend Coffee";
    
    public decimal GetCost() => _size switch
    {
        "Small" => 1.49m,
        "Medium" => 1.89m,
        "Large" => 2.29m,
        _ => 1.89m
    };
    
    public int GetCalories() => _size switch
    {
        "Small" => 2,
        "Medium" => 3,
        "Large" => 5,
        _ => 3
    };
    
    public string GetSize() => _size;
}

public class DarkRoast : IBeverage
{
    private readonly string _size;
    
    public DarkRoast(string size = "Medium")
    {
        _size = size;
    }
    
    public string GetDescription() => $"{_size} Dark Roast Coffee";
    
    public decimal GetCost() => _size switch
    {
        "Small" => 1.69m,
        "Medium" => 2.09m,
        "Large" => 2.49m,
        _ => 2.09m
    };
    
    public int GetCalories() => _size switch
    {
        "Small" => 3,
        "Medium" => 5,
        "Large" => 7,
        _ => 5
    };
    
    public string GetSize() => _size;
}

// Beverage decorators
public abstract class CondimentDecorator : IBeverage
{
    protected IBeverage _beverage;
    
    protected CondimentDecorator(IBeverage beverage)
    {
        _beverage = beverage ?? throw new ArgumentNullException(nameof(beverage));
    }
    
    public abstract string GetDescription();
    public abstract decimal GetCost();
    public abstract int GetCalories();
    
    public string GetSize() => _beverage.GetSize();
}

public class Milk : CondimentDecorator
{
    public Milk(IBeverage beverage) : base(beverage) { }
    
    public override string GetDescription()
    {
        return $"{_beverage.GetDescription()} + Milk";
    }
    
    public override decimal GetCost()
    {
        return _beverage.GetCost() + 0.60m;
    }
    
    public override int GetCalories()
    {
        return _beverage.GetCalories() + 20;
    }
}

public class Mocha : CondimentDecorator
{
    public Mocha(IBeverage beverage) : base(beverage) { }
    
    public override string GetDescription()
    {
        return $"{_beverage.GetDescription()} + Mocha";
    }
    
    public override decimal GetCost()
    {
        return _beverage.GetCost() + 0.80m;
    }
    
    public override int GetCalories()
    {
        return _beverage.GetCalories() + 35;
    }
}

public class Whip : CondimentDecorator
{
    public Whip(IBeverage beverage) : base(beverage) { }
    
    public override string GetDescription()
    {
        return $"{_beverage.GetDescription()} + Whipped Cream";
    }
    
    public override decimal GetCost()
    {
        return _beverage.GetCost() + 0.70m;
    }
    
    public override int GetCalories()
    {
        return _beverage.GetCalories() + 50;
    }
}

public class Soy : CondimentDecorator
{
    public Soy(IBeverage beverage) : base(beverage) { }
    
    public override string GetDescription()
    {
        return $"{_beverage.GetDescription()} + Soy";
    }
    
    public override decimal GetCost()
    {
        return _beverage.GetCost() + 0.55m;
    }
    
    public override int GetCalories()
    {
        return _beverage.GetCalories() + 15;
    }
}

public class ExtraShot : CondimentDecorator
{
    public ExtraShot(IBeverage beverage) : base(beverage) { }
    
    public override string GetDescription()
    {
        return $"{_beverage.GetDescription()} + Extra Shot";
    }
    
    public override decimal GetCost()
    {
        return _beverage.GetCost() + 0.75m;
    }
    
    public override int GetCalories()
    {
        return _beverage.GetCalories() + 5;
    }
}

// Data Processing Decorator Pattern
public interface IDataProcessor
{
    byte[] ProcessData(byte[] data);
    string GetProcessingSteps();
    TimeSpan GetEstimatedTime(int dataSize);
}

public class RawDataProcessor : IDataProcessor
{
    public byte[] ProcessData(byte[] data)
    {
        return data ?? Array.Empty<byte>();
    }
    
    public string GetProcessingSteps()
    {
        return "Raw Data";
    }
    
    public TimeSpan GetEstimatedTime(int dataSize)
    {
        return TimeSpan.FromMilliseconds(1);
    }
}

public abstract class DataProcessorDecorator : IDataProcessor
{
    protected IDataProcessor _processor;
    
    protected DataProcessorDecorator(IDataProcessor processor)
    {
        _processor = processor ?? throw new ArgumentNullException(nameof(processor));
    }
    
    public virtual byte[] ProcessData(byte[] data)
    {
        return _processor.ProcessData(data);
    }
    
    public virtual string GetProcessingSteps()
    {
        return _processor.GetProcessingSteps();
    }
    
    public virtual TimeSpan GetEstimatedTime(int dataSize)
    {
        return _processor.GetEstimatedTime(dataSize);
    }
}

public class ValidationDecorator : DataProcessorDecorator
{
    private readonly Func<byte[], bool> _validator;
    
    public ValidationDecorator(IDataProcessor processor, Func<byte[], bool> validator = null) 
        : base(processor)
    {
        _validator = validator ?? (data => data != null && data.Length > 0);
    }
    
    public override byte[] ProcessData(byte[] data)
    {
        if (!_validator(data))
        {
            throw new ArgumentException("Data validation failed");
        }
        
        return base.ProcessData(data);
    }
    
    public override string GetProcessingSteps()
    {
        return $"{base.GetProcessingSteps()} → Validate";
    }
    
    public override TimeSpan GetEstimatedTime(int dataSize)
    {
        return base.GetEstimatedTime(dataSize).Add(TimeSpan.FromMilliseconds(dataSize * 0.001));
    }
}

public class EncryptionDecorator : DataProcessorDecorator
{
    private readonly string _algorithm;
    
    public EncryptionDecorator(IDataProcessor processor, string algorithm = "AES") 
        : base(processor)
    {
        _algorithm = algorithm;
    }
    
    public override byte[] ProcessData(byte[] data)
    {
        var processed = base.ProcessData(data);
        
        // Simple XOR encryption for demo
        var key = Encoding.UTF8.GetBytes("SecretKey123");
        for (int i = 0; i < processed.Length; i++)
        {
            processed[i] ^= key[i % key.Length];
        }
        
        return processed;
    }
    
    public override string GetProcessingSteps()
    {
        return $"{base.GetProcessingSteps()} → Encrypt({_algorithm})";
    }
    
    public override TimeSpan GetEstimatedTime(int dataSize)
    {
        return base.GetEstimatedTime(dataSize).Add(TimeSpan.FromMilliseconds(dataSize * 0.01));
    }
}

public class CompressionDecorator : DataProcessorDecorator
{
    private readonly string _algorithm;
    
    public CompressionDecorator(IDataProcessor processor, string algorithm = "GZIP") 
        : base(processor)
    {
        _algorithm = algorithm;
    }
    
    public override byte[] ProcessData(byte[] data)
    {
        var processed = base.ProcessData(data);
        
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionMode.Compress))
        {
            gzip.Write(processed, 0, processed.Length);
        }
        
        return output.ToArray();
    }
    
    public override string GetProcessingSteps()
    {
        return $"{base.GetProcessingSteps()} → Compress({_algorithm})";
    }
    
    public override TimeSpan GetEstimatedTime(int dataSize)
    {
        return base.GetEstimatedTime(dataSize).Add(TimeSpan.FromMilliseconds(dataSize * 0.005));
    }
}

public class LoggingDecorator : DataProcessorDecorator
{
    private readonly string _logLevel;
    
    public LoggingDecorator(IDataProcessor processor, string logLevel = "INFO") 
        : base(processor)
    {
        _logLevel = logLevel;
    }
    
    public override byte[] ProcessData(byte[] data)
    {
        Console.WriteLine($"[{_logLevel}] Processing {data?.Length ?? 0} bytes of data");
        var start = DateTime.Now;
        
        var result = base.ProcessData(data);
        
        var duration = DateTime.Now - start;
        Console.WriteLine($"[{_logLevel}] Processed {result?.Length ?? 0} bytes in {duration.TotalMilliseconds:F2}ms");
        
        return result;
    }
    
    public override string GetProcessingSteps()
    {
        return $"{base.GetProcessingSteps()} → Log({_logLevel})";
    }
    
    public override TimeSpan GetEstimatedTime(int dataSize)
    {
        return base.GetEstimatedTime(dataSize).Add(TimeSpan.FromMilliseconds(1));
    }
}

// Decorator Factory for common combinations
public static class DecoratorFactory
{
    public static ITextProcessor CreateStandardTextProcessor(ITextProcessor baseProcessor)
    {
        return new TrimDecorator(
            new LowerCaseDecorator(baseProcessor));
    }
    
    public static ITextProcessor CreateSecureTextProcessor(ITextProcessor baseProcessor, string encryptionKey)
    {
        return new CompressDecorator(
            new EncryptDecorator(
                new TrimDecorator(baseProcessor), encryptionKey));
    }
    
    public static IBeverage CreateMochaLatte(string size = "Medium")
    {
        return new Whip(
            new Mocha(
                new Milk(
                    new Espresso(size))));
    }
    
    public static IBeverage CreateCustomCoffee(IBeverage baseCoffee, params string[] addOns)
    {
        IBeverage result = baseCoffee;
        
        foreach (var addOn in addOns)
        {
            result = addOn.ToLower() switch
            {
                "milk" => new Milk(result),
                "mocha" => new Mocha(result),
                "whip" => new Whip(result),
                "soy" => new Soy(result),
                "extra shot" => new ExtraShot(result),
                _ => result
            };
        }
        
        return result;
    }
    
    public static IDataProcessor CreateSecureDataPipeline(IDataProcessor baseProcessor)
    {
        return new LoggingDecorator(
            new CompressionDecorator(
                new EncryptionDecorator(
                    new ValidationDecorator(baseProcessor))), "DEBUG");
    }
}
```

**Usage**:

```csharp
using System;
using System.Text;

class Program
{
    static void Main()
    {
        Console.WriteLine("=== Text Processing Decorators ===");
        
        // Start with basic text processor
        ITextProcessor processor = new BasicTextProcessor();
        
        string sampleText = "  Hello World! This is a Test.  ";
        Console.WriteLine($"Original: '{sampleText}'");
        Console.WriteLine($"Basic: '{processor.Process(sampleText)}'");
        Console.WriteLine($"Description: {processor.GetDescription()}");
        Console.WriteLine($"Cost: {processor.GetProcessingCost()}");
        
        // Add decorators one by one
        processor = new TrimDecorator(processor);
        Console.WriteLine($"\nWith Trim: '{processor.Process(sampleText)}'");
        Console.WriteLine($"Description: {processor.GetDescription()}");
        Console.WriteLine($"Cost: {processor.GetProcessingCost()}");
        
        processor = new UpperCaseDecorator(processor);
        Console.WriteLine($"\nWith UpperCase: '{processor.Process(sampleText)}'");
        Console.WriteLine($"Description: {processor.GetDescription()}");
        Console.WriteLine($"Cost: {processor.GetProcessingCost()}");
        
        processor = new ReverseDecorator(processor);
        Console.WriteLine($"\nWith Reverse: '{processor.Process(sampleText)}'");
        Console.WriteLine($"Description: {processor.GetDescription()}");
        Console.WriteLine($"Cost: {processor.GetProcessingCost()}");
        
        // Complex decoration chain
        Console.WriteLine("\n=== Complex Text Processing ===");
        
        var complexProcessor = new CompressDecorator(
            new EncryptDecorator(
                new UpperCaseDecorator(
                    new TrimDecorator(
                        new BasicTextProcessor())), "MySecretKey"));
        
        string sensitiveText = "   This is sensitive information!   ";
        Console.WriteLine($"Original: '{sensitiveText}'");
        Console.WriteLine($"Processed: '{complexProcessor.Process(sensitiveText)}'");
        Console.WriteLine($"Description: {complexProcessor.GetDescription()}");
        Console.WriteLine($"Total Cost: {complexProcessor.GetProcessingCost()}");
        
        // Using factory methods
        Console.WriteLine("\n=== Factory-Created Processors ===");
        
        var standardProcessor = DecoratorFactory.CreateStandardTextProcessor(new BasicTextProcessor());
        Console.WriteLine($"Standard: '{standardProcessor.Process(sampleText)}'");
        Console.WriteLine($"Description: {standardProcessor.GetDescription()}");
        
        var secureProcessor = DecoratorFactory.CreateSecureTextProcessor(new BasicTextProcessor(), "SecureKey123");
        Console.WriteLine($"Secure: '{secureProcessor.Process(sensitiveText)}'");
        Console.WriteLine($"Description: {secureProcessor.GetDescription()}");
        
        Console.WriteLine("\n=== Coffee Shop Decorators ===");
        
        // Basic beverages
        IBeverage espresso = new Espresso("Large");
        Console.WriteLine($"{espresso.GetDescription()}: ${espresso.GetCost():F2} ({espresso.GetCalories()} cal)");
        
        IBeverage houseBlend = new HouseBlend("Medium");
        Console.WriteLine($"{houseBlend.GetDescription()}: ${houseBlend.GetCost():F2} ({houseBlend.GetCalories()} cal)");
        
        // Decorated beverages
        IBeverage mochaEspresso = new Mocha(new Milk(espresso));
        Console.WriteLine($"\n{mochaEspresso.GetDescription()}: ${mochaEspresso.GetCost():F2} ({mochaEspresso.GetCalories()} cal)");
        
        IBeverage fancyCoffee = new Whip(
            new Mocha(
                new Mocha(
                    new Soy(
                        new DarkRoast("Large")))));
        
        Console.WriteLine($"{fancyCoffee.GetDescription()}: ${fancyCoffee.GetCost():F2} ({fancyCoffee.GetCalories()} cal)");
        
        // Factory-created beverages
        Console.WriteLine("\n=== Specialty Drinks ===");
        
        var mochaLatte = DecoratorFactory.CreateMochaLatte("Large");
        Console.WriteLine($"{mochaLatte.GetDescription()}: ${mochaLatte.GetCost():F2} ({mochaLatte.GetCalories()} cal)");
        
        var customCoffee = DecoratorFactory.CreateCustomCoffee(
            new HouseBlend("Medium"), 
            "milk", "extra shot", "whip", "mocha");
        Console.WriteLine($"{customCoffee.GetDescription()}: ${customCoffee.GetCost():F2} ({customCoffee.GetCalories()} cal)");
        
        Console.WriteLine("\n=== Data Processing Pipeline ===");
        
        // Create sample data
        byte[] sampleData = Encoding.UTF8.GetBytes("This is important data that needs to be processed securely.");
        Console.WriteLine($"Original data size: {sampleData.Length} bytes");
        
        // Basic processing
        IDataProcessor dataProcessor = new RawDataProcessor();
        Console.WriteLine($"Basic processing: {dataProcessor.GetProcessingSteps()}");
        Console.WriteLine($"Estimated time: {dataProcessor.GetEstimatedTime(sampleData.Length).TotalMilliseconds:F2}ms");
        
        // Add validation
        dataProcessor = new ValidationDecorator(dataProcessor);
        Console.WriteLine($"\nWith validation: {dataProcessor.GetProcessingSteps()}");
        Console.WriteLine($"Estimated time: {dataProcessor.GetEstimatedTime(sampleData.Length).TotalMilliseconds:F2}ms");
        
        // Complete secure pipeline
        var securePipeline = DecoratorFactory.CreateSecureDataPipeline(new RawDataProcessor());
        Console.WriteLine($"\nSecure pipeline: {securePipeline.GetProcessingSteps()}");
        Console.WriteLine($"Estimated time: {securePipeline.GetEstimatedTime(sampleData.Length).TotalMilliseconds:F2}ms");
        
        // Process the data
        Console.WriteLine("\n--- Processing Data ---");
        var processedData = securePipeline.ProcessData(sampleData);
        Console.WriteLine($"Processed data size: {processedData.Length} bytes");
        
        Console.WriteLine("\n=== Decorator Pattern Benefits ===");
        Console.WriteLine("✅ Add responsibilities to objects dynamically");
        Console.WriteLine("✅ More flexible than static inheritance");
        Console.WriteLine("✅ Components can be wrapped in multiple decorators");
        Console.WriteLine("✅ Decorators can be combined in any order");
        Console.WriteLine("✅ Single Responsibility Principle - each decorator has one job");
        Console.WriteLine("✅ Open/Closed Principle - open for extension, closed for modification");
        
        Console.WriteLine("\n=== Real-world Applications ===");
        Console.WriteLine("• Stream processing (compression, encryption)");
        Console.WriteLine("• Text formatting (bold, italic, underline)");
        Console.WriteLine("• Web request/response processing");
        Console.WriteLine("• GUI component enhancement");
        Console.WriteLine("• Data validation pipelines");
        Console.WriteLine("• Caching layers");
    }
}
```

**Notes**:

- Adds behavior to objects dynamically without altering their structure
- More flexible than inheritance - decorators can be combined in any order
- Follows Open/Closed Principle - open for extension, closed for modification
- Each decorator has a single responsibility, making code maintainable
- Can result in many small objects that are difficult to debug
- Decorators must implement the same interface as the component they decorate
- Order of decoration matters - different orders can produce different results
- Useful for cross-cutting concerns like logging, security, caching
- Related patterns: [Composite](composite.md), [Strategy](strategy.md), [Proxy](proxy.md)

**Prerequisites**:

- .NET Framework 2.0+ or .NET Core
- Understanding of interfaces and composition
- Knowledge of Single Responsibility Principle
- Familiarity with wrapper/adapter concepts

**References**:

- Gang of Four Design Patterns book
- [Microsoft Docs: Decorator Pattern](https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/decorator)

---

*Created: 2025-10-31*  
*Last Updated: 2025-10-31*  
*Tags: #decorator #structural #wrapper #dynamic*
