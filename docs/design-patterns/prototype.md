# Prototype Pattern

**Description**: Creates objects by cloning existing instances rather than creating new ones from scratch. Useful when object creation is expensive or when you need copies of complex objects.

**Language/Technology**: C# / .NET

**Code**:

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.Json;

/// <summary>
/// Abstract prototype interface
/// </summary>
public interface IPrototype<T>
{
    T Clone();
}

/// <summary>
/// Deep cloneable base class using serialization
/// </summary>
[Serializable]
public abstract class DeepCloneableBase<T> : IPrototype<T> where T : class
{
    /// <summary>
    /// Creates a deep copy using JSON serialization (recommended for .NET Core/.NET 5+)
    /// </summary>
    public virtual T Clone()
    {
        var json = JsonSerializer.Serialize(this, GetType());
        return JsonSerializer.Deserialize(json, GetType()) as T;
    }
    
    /// <summary>
    /// Creates a deep copy using binary serialization (legacy method)
    /// </summary>
    public virtual T DeepCloneBinary()
    {
        using var memoryStream = new MemoryStream();
        var formatter = new BinaryFormatter();
        formatter.Serialize(memoryStream, this);
        memoryStream.Position = 0;
        return formatter.Deserialize(memoryStream) as T;
    }
}

/// <summary>
/// Example complex object that can be expensive to create
/// </summary>
[Serializable]
public class Document : DeepCloneableBase<Document>
{
    public string Title { get; set; }
    public string Content { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
    public DocumentMetadata Metadata { get; set; }
    public List<string> Tags { get; set; } = new();
    public List<DocumentSection> Sections { get; set; } = new();
    
    public Document()
    {
        CreatedDate = DateTime.Now;
        ModifiedDate = DateTime.Now;
        Metadata = new DocumentMetadata();
    }
    
    public Document(string title, string content) : this()
    {
        Title = title;
        Content = content;
    }
    
    /// <summary>
    /// Custom clone method with specific behavior
    /// </summary>
    public override Document Clone()
    {
        // Use base JSON serialization for deep copy
        var cloned = base.Clone();
        
        // Update specific properties for the clone
        cloned.CreatedDate = DateTime.Now;
        cloned.ModifiedDate = DateTime.Now;
        cloned.Title = $"Copy of {cloned.Title}";
        
        return cloned;
    }
    
    /// <summary>
    /// Shallow clone method (implements ICloneable)
    /// </summary>
    public Document ShallowClone()
    {
        return (Document)this.MemberwiseClone();
    }
    
    public void AddSection(string title, string content)
    {
        Sections.Add(new DocumentSection { Title = title, Content = content });
        ModifiedDate = DateTime.Now;
    }
    
    public override string ToString()
    {
        return $"Document: {Title} (Created: {CreatedDate:yyyy-MM-dd}, Sections: {Sections.Count}, Tags: {Tags.Count})";
    }
}

[Serializable]
public class DocumentMetadata
{
    public string Author { get; set; } = "Unknown";
    public string Department { get; set; } = "General";
    public string Version { get; set; } = "1.0";
    public Dictionary<string, string> CustomProperties { get; set; } = new();
}

[Serializable]
public class DocumentSection
{
    public string Title { get; set; }
    public string Content { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;
}

/// <summary>
/// Prototype registry for managing prototype instances
/// </summary>
public class DocumentPrototypeRegistry
{
    private readonly Dictionary<string, Document> _prototypes = new();
    
    public void RegisterPrototype(string key, Document prototype)
    {
        _prototypes[key] = prototype;
    }
    
    public Document CreateDocument(string prototypeKey)
    {
        if (_prototypes.TryGetValue(prototypeKey, out var prototype))
        {
            return prototype.Clone();
        }
        throw new ArgumentException($"No prototype found for key: {prototypeKey}");
    }
    
    public IEnumerable<string> GetAvailablePrototypes()
    {
        return _prototypes.Keys;
    }
    
    public void RemovePrototype(string key)
    {
        _prototypes.Remove(key);
    }
}

/// <summary>
/// Factory that uses prototype pattern
/// </summary>
public class DocumentFactory
{
    private readonly DocumentPrototypeRegistry _registry;
    
    public DocumentFactory()
    {
        _registry = new DocumentPrototypeRegistry();
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
        reportTemplate.Tags.AddRange(new[] { "report", "monthly", "template" });
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
        memoTemplate.Tags.AddRange(new[] { "memo", "internal", "template" });
        memoTemplate.AddSection("Purpose", "[Purpose of the memo]");
        memoTemplate.AddSection("Details", "[Detailed information]");
        
        var proposalTemplate = new Document("Project Proposal", "Standard project proposal template.")
        {
            Metadata = new DocumentMetadata
            {
                Author = "System",
                Department = "Projects",
                Version = "3.0"
            }
        };
        proposalTemplate.Tags.AddRange(new[] { "proposal", "project", "template" });
        proposalTemplate.AddSection("Overview", "[Project overview]");
        proposalTemplate.AddSection("Scope", "[Project scope and deliverables]");
        proposalTemplate.AddSection("Timeline", "[Project timeline]");
        proposalTemplate.AddSection("Budget", "[Budget estimation]");
        
        // Register prototypes
        _registry.RegisterPrototype("report", reportTemplate);
        _registry.RegisterPrototype("memo", memoTemplate);
        _registry.RegisterPrototype("proposal", proposalTemplate);
    }
    
    public Document CreateDocument(string type)
    {
        return _registry.CreateDocument(type);
    }
    
    public Document CreateCustomDocument(string title, string content)
    {
        return new Document(title, content);
    }
    
    public IEnumerable<string> GetAvailableTemplates()
    {
        return _registry.GetAvailablePrototypes();
    }
}
```

**Usage**:

```csharp
using System;

class Program
{
    static void Main()
    {
        // Direct prototype usage
        Console.WriteLine("=== Direct Prototype Cloning ===");
        
        var originalDoc = new Document("Original Document", "This is the original content.");
        originalDoc.Tags.AddRange(new[] { "original", "test" });
        originalDoc.AddSection("Introduction", "This is the introduction.");
        originalDoc.Metadata.Author = "John Doe";
        
        Console.WriteLine($"Original: {originalDoc}");
        
        // Deep clone
        var clonedDoc = originalDoc.Clone();
        clonedDoc.Tags.Add("cloned");
        clonedDoc.AddSection("Cloned Section", "This section was added to the clone.");
        
        Console.WriteLine($"Cloned: {clonedDoc}");
        Console.WriteLine($"Original sections count: {originalDoc.Sections.Count}");
        Console.WriteLine($"Cloned sections count: {clonedDoc.Sections.Count}");
        
        // Shallow clone demonstration
        var shallowClone = originalDoc.ShallowClone();
        shallowClone.Title = "Shallow Clone";
        shallowClone.Tags.Add("shallow"); // This affects original too!
        
        Console.WriteLine($"After shallow clone modification:");
        Console.WriteLine($"Original tags: [{string.Join(", ", originalDoc.Tags)}]");
        Console.WriteLine($"Shallow clone tags: [{string.Join(", ", shallowClone.Tags)}]");
        
        // Factory with prototype registry
        Console.WriteLine("\n=== Factory with Prototype Registry ===");
        
        var factory = new DocumentFactory();
        
        Console.WriteLine("Available templates:");
        foreach (var template in factory.GetAvailableTemplates())
        {
            Console.WriteLine($"  - {template}");
        }
        
        // Create documents from templates
        var monthlyReport = factory.CreateDocument("report");
        monthlyReport.Title = "October 2025 Sales Report";
        monthlyReport.Metadata.Author = "Sales Team";
        
        var teamMemo = factory.CreateDocument("memo");
        teamMemo.Title = "Team Building Event Announcement";
        teamMemo.Metadata.Author = "HR Manager";
        
        var projectProposal = factory.CreateDocument("proposal");
        projectProposal.Title = "Mobile App Development Proposal";
        projectProposal.Metadata.Author = "Development Team";
        
        Console.WriteLine($"\nCreated from templates:");
        Console.WriteLine(monthlyReport);
        Console.WriteLine(teamMemo);
        Console.WriteLine(projectProposal);
        
        // Performance comparison
        Console.WriteLine("\n=== Performance Comparison ===");
        
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        // Create 1000 documents using prototype
        for (int i = 0; i < 1000; i++)
        {
            var doc = factory.CreateDocument("report");
            doc.Title = $"Report #{i}";
        }
        stopwatch.Stop();
        Console.WriteLine($"Creating 1000 documents using prototype: {stopwatch.ElapsedMilliseconds}ms");
        
        stopwatch.Restart();
        
        // Create 1000 documents from scratch
        for (int i = 0; i < 1000; i++)
        {
            var doc = factory.CreateCustomDocument($"Custom Report #{i}", "Custom content");
            doc.Tags.AddRange(new[] { "custom", "test" });
            doc.AddSection("Section 1", "Content 1");
            doc.AddSection("Section 2", "Content 2");
        }
        stopwatch.Stop();
        Console.WriteLine($"Creating 1000 documents from scratch: {stopwatch.ElapsedMilliseconds}ms");
    }
}
```

**Notes**:

- Useful when object creation is expensive (database loading, complex calculations)
- Provides deep and shallow cloning options
- JSON serialization recommended over binary serialization in modern .NET
- Registry pattern manages multiple prototype instances
- Custom clone behavior can be implemented per class
- Performance benefits when creating many similar objects
- Avoids subclassing for object creation
- Can reduce coupling between client and concrete classes
- Related patterns: [Factory Method](factory-method.md), [Singleton](singleton.md)

**Prerequisites**:

- .NET Framework 2.0+ or .NET Core
- System.Text.Json package for JSON serialization (.NET Core/.NET 5+)
- Understanding of serialization concepts

**References**:

- Gang of Four Design Patterns book
- [Microsoft Docs: Object.MemberwiseClone](https://docs.microsoft.com/en-us/dotnet/api/system.object.memberwiseclone)

---

*Created: 2025-10-31*  
*Last Updated: 2025-10-31*  
*Tags: #prototype #creational #cloning #performance*
