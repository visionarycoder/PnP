# Composite Pattern

**Description**: Composes objects into tree structures to represent part-whole hierarchies. Lets clients treat individual objects and compositions uniformly. Useful for building recursive tree structures like file systems, organizational charts, or UI components.

**Language/Technology**: C# / .NET

**Code**:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

// Component - declares interface for objects in the composition
public abstract class FileSystemComponent
{
    protected string name;
    protected DateTime created;
    protected DateTime modified;
    
    protected FileSystemComponent(string name)
    {
        name = name ?? throw new ArgumentNullException(nameof(name));
        created = DateTime.Now;
        modified = DateTime.Now;
    }
    
    public string Name => name;
    public DateTime Created => created;
    public DateTime Modified => modified;
    
    public abstract long GetSize();
    public abstract int GetCount();
    public abstract void Display(int depth = 0);
    public abstract string GetPath();
    public abstract FileSystemComponent Clone();
    
    // Default implementations for operations that may not apply to all components
    public virtual void Add(FileSystemComponent component)
    {
        throw new NotSupportedException($"Cannot add components to {GetType().Name}");
    }
    
    public virtual void Remove(FileSystemComponent component)
    {
        throw new NotSupportedException($"Cannot remove components from {GetType().Name}");
    }
    
    public virtual FileSystemComponent GetChild(int index)
    {
        throw new NotSupportedException($"Cannot get children from {GetType().Name}");
    }
    
    public virtual IEnumerable<FileSystemComponent> GetChildren()
    {
        return Enumerable.Empty<FileSystemComponent>();
    }
    
    protected void UpdateModified()
    {
        modified = DateTime.Now;
    }
    
    protected string GetIndentation(int depth)
    {
        return new string(' ', depth * 2);
    }
}

// Leaf - represents leaf objects (files)
public class File : FileSystemComponent
{
    private byte[] content;
    private readonly string extension;
    
    public File(string name, byte[] content = null) : base(name)
    {
        content = content ?? Array.Empty<byte>();
        extension = System.IO.Path.GetExtension(name);
    }
    
    public File(string name, string textContent) : base(name)
    {
        content = Encoding.UTF8.GetBytes(textContent ?? string.Empty);
        extension = System.IO.Path.GetExtension(name);
    }
    
    public string Extension => extension;
    public byte[] Content => content;
    public string TextContent => Encoding.UTF8.GetString(content);
    
    public override long GetSize() => content.Length;
    
    public override int GetCount() => 1;
    
    public override void Display(int depth = 0)
    {
        var indent = GetIndentation(depth);
        var sizeStr = GetSize() < 1024 ? $"{GetSize()}B" : $"{GetSize() / 1024.0:F1}KB";
        Console.WriteLine($"{indent}📄 {name} ({sizeStr}) [{extension}]");
    }
    
    public override string GetPath()
    {
        // Files don't know their parent path in this simple implementation
        return name;
    }
    
    public void WriteContent(string content)
    {
        content = Encoding.UTF8.GetBytes(content ?? string.Empty);
        UpdateModified();
    }
    
    public void WriteContent(byte[] content)
    {
        content = content ?? Array.Empty<byte>();
        UpdateModified();
    }
    
    public override FileSystemComponent Clone()
    {
        return new File(name, (byte[])content.Clone());
    }
}

// Composite - represents composite objects (directories)
public class Directory : FileSystemComponent
{
    private readonly List<FileSystemComponent> children;
    private Directory parent;
    
    public Directory(string name) : base(name)
    {
        children = new List<FileSystemComponent>();
    }
    
    public Directory Parent => parent;
    public IReadOnlyList<FileSystemComponent> Children => children.AsReadOnly();
    
    public override long GetSize() => children.Sum(child => child.GetSize());
    
    public override int GetCount() => children.Sum(child => child.GetCount());
    
    public override void Display(int depth = 0)
    {
        var indent = GetIndentation(depth);
        var sizeStr = GetSize() < 1024 ? $"{GetSize()}B" : 
                     GetSize() < 1024 * 1024 ? $"{GetSize() / 1024.0:F1}KB" : 
                     $"{GetSize() / (1024.0 * 1024.0):F1}MB";
        
        Console.WriteLine($"{indent}📁 {name}/ ({GetCount()} items, {sizeStr})");
        
        foreach (var child in children)
        {
            child.Display(depth + 1);
        }
    }
    
    public override string GetPath()
    {
        if (parent == null)
            return name;
        
        return $"{parent.GetPath()}/{name}";
    }
    
    public override void Add(FileSystemComponent component)
    {
        if (component == null)
            throw new ArgumentNullException(nameof(component));
        
        if (component == this)
            throw new ArgumentException("Cannot add directory to itself");
        
        // Check for circular reference
        if (component is Directory dir && IsAncestor(dir))
            throw new ArgumentException("Cannot create circular reference");
        
        children.Add(component);
        
        if (component is Directory childDir)
        {
            childDir.parent = this;
        }
        
        UpdateModified();
    }
    
    public override void Remove(FileSystemComponent component)
    {
        if (children.Remove(component))
        {
            if (component is Directory dir)
            {
                dir.parent = null;
            }
            UpdateModified();
        }
    }
    
    public override FileSystemComponent GetChild(int index)
    {
        if (index < 0 || index >= children.Count)
            throw new ArgumentOutOfRangeException(nameof(index));
        
        return _children[index];
    }
    
    public override IEnumerable<FileSystemComponent> GetChildren()
    {
        return children.AsEnumerable();
    }
    
    private bool IsAncestor(Directory potentialAncestor)
    {
        var current = parent;
        while (current != null)
        {
            if (current == potentialAncestor)
                return true;
            current = current.parent;
        }
        return false;
    }
    
    public FileSystemComponent Find(string name)
    {
        if (name.Equals(name, StringComparison.OrdinalIgnoreCase))
            return this;
        
        foreach (var child in children)
        {
            if (child.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                return child;
            
            if (child is Directory dir)
            {
                var found = dir.Find(name);
                if (found != null)
                    return found;
            }
        }
        
        return null;
    }
    
    public IEnumerable<File> GetAllFiles()
    {
        foreach (var child in children)
        {
            if (child is File file)
            {
                yield return file;
            }
            else if (child is Directory dir)
            {
                foreach (var nestedFile in dir.GetAllFiles())
                {
                    yield return nestedFile;
                }
            }
        }
    }
    
    public override FileSystemComponent Clone()
    {
        var clonedDir = new Directory(name);
        
        foreach (var child in children)
        {
            clonedDir.Add(child.Clone());
        }
        
        return clonedDir;
    }
}

// Advanced Composite: Organizational Chart
public abstract class OrganizationComponent
{
    protected string name;
    protected string position;
    protected decimal salary;
    protected DateTime hireDate;
    
    protected OrganizationComponent(string name, string position, decimal salary)
    {
        name = name ?? throw new ArgumentNullException(nameof(name));
        position = position ?? throw new ArgumentNullException(nameof(position));
        salary = salary;
        hireDate = DateTime.Now;
    }
    
    public string Name => name;
    public string Position => position;
    public decimal Salary => salary;
    public DateTime HireDate => hireDate;
    
    public abstract decimal GetTotalSalary();
    public abstract int GetTeamSize();
    public abstract void DisplayHierarchy(int depth = 0);
    public abstract void GiveRaise(decimal percentage);
    
    public virtual void Add(OrganizationComponent component)
    {
        throw new NotSupportedException($"Cannot add subordinates to {GetType().Name}");
    }
    
    public virtual void Remove(OrganizationComponent component)
    {
        throw new NotSupportedException($"Cannot remove subordinates from {GetType().Name}");
    }
    
    public virtual IEnumerable<OrganizationComponent> GetSubordinates()
    {
        return Enumerable.Empty<OrganizationComponent>();
    }
    
    protected string GetIndentation(int depth)
    {
        return new string('│', depth) + (depth > 0 ? "├─ " : "");
    }
}

public class Employee : OrganizationComponent
{
    private readonly string department;
    private readonly List<string> skills;
    
    public Employee(string name, string position, decimal salary, string department) 
        : base(name, position, salary)
    {
        department = department ?? "General";
        skills = new List<string>();
    }
    
    public string Department => department;
    public IReadOnlyList<string> Skills => skills.AsReadOnly();
    
    public void AddSkill(string skill)
    {
        if (!string.IsNullOrWhiteSpace(skill) && !skills.Contains(skill))
        {
            skills.Add(skill);
        }
    }
    
    public override decimal GetTotalSalary() => salary;
    
    public override int GetTeamSize() => 1;
    
    public override void DisplayHierarchy(int depth = 0)
    {
        var indent = GetIndentation(depth);
        Console.WriteLine($"{indent}👤 {name} - {position} ({department})");
        Console.WriteLine($"{new string(' ', indent.Length)}   💰 ${salary:N0}/year | Skills: {string.Join(", ", skills)}");
    }
    
    public override void GiveRaise(decimal percentage)
    {
        salary *= (1 + percentage / 100);
        Console.WriteLine($"💰 {name} received a {percentage}% raise. New salary: ${salary:N0}");
    }
}

public class Manager : OrganizationComponent
{
    private readonly List<OrganizationComponent> subordinates;
    private readonly string department;
    private readonly decimal bonus;
    
    public Manager(string name, string position, decimal salary, string department, decimal bonus = 0) 
        : base(name, position, salary)
    {
        subordinates = new List<OrganizationComponent>();
        department = department ?? "Management";
        bonus = bonus;
    }
    
    public string Department => department;
    public decimal Bonus => bonus;
    public IReadOnlyList<OrganizationComponent> Subordinates => subordinates.AsReadOnly();
    
    public override decimal GetTotalSalary()
    {
        return salary + bonus + subordinates.Sum(s => s.GetTotalSalary());
    }
    
    public override int GetTeamSize()
    {
        return 1 + subordinates.Sum(s => s.GetTeamSize());
    }
    
    public override void DisplayHierarchy(int depth = 0)
    {
        var indent = GetIndentation(depth);
        var bonusText = bonus > 0 ? $" (+ ${bonus:N0} bonus)" : "";
        Console.WriteLine($"{indent}👨‍💼 {name} - {position} ({department})");
        Console.WriteLine($"{new string(' ', indent.Length)}   💰 ${salary:N0}/year{bonusText} | Team: {GetTeamSize()} people | Budget: ${GetTotalSalary():N0}");
        
        foreach (var subordinate in subordinates)
        {
            subordinate.DisplayHierarchy(depth + 1);
        }
    }
    
    public override void Add(OrganizationComponent component)
    {
        if (component == null)
            throw new ArgumentNullException(nameof(component));
        
        if (component == this)
            throw new ArgumentException("Cannot add manager to themselves");
        
        subordinates.Add(component);
    }
    
    public override void Remove(OrganizationComponent component)
    {
        subordinates.Remove(component);
    }
    
    public override IEnumerable<OrganizationComponent> GetSubordinates()
    {
        return subordinates.AsEnumerable();
    }
    
    public override void GiveRaise(decimal percentage)
    {
        // Manager raises affect the entire team
        salary *= (1 + percentage / 100);
        Console.WriteLine($"💰 {name} (Manager) received a {percentage}% raise. New salary: ${salary:N0}");
        
        // Give smaller raise to subordinates
        foreach (var subordinate in subordinates)
        {
            subordinate.GiveRaise(percentage * 0.5m); // Half the manager's raise
        }
    }
}

// Composite Pattern with UI Components
public abstract class UIComponent
{
    protected string name;
    protected bool visible;
    protected (int X, int Y) position;
    protected (int Width, int Height) size;
    
    protected UIComponent(string name, int x = 0, int y = 0, int width = 100, int height = 20)
    {
        name = name ?? throw new ArgumentNullException(nameof(name));
        visible = true;
        position = (x, y);
        size = (width, height);
    }
    
    public string Name => name;
    public bool Visible => visible;
    public (int X, int Y) Position => position;
    public (int Width, int Height) Size => size;
    
    public abstract void Render(int depth = 0);
    public abstract void SetVisible(bool visible);
    public abstract (int Width, int Height) GetTotalSize();
    
    public virtual void Add(UIComponent component)
    {
        throw new NotSupportedException($"Cannot add components to {GetType().Name}");
    }
    
    public virtual void Remove(UIComponent component)
    {
        throw new NotSupportedException($"Cannot remove components from {GetType().Name}");
    }
    
    public void Move(int x, int y)
    {
        position = (x, y);
    }
    
    public void Resize(int width, int height)
    {
        size = (width, height);
    }
    
    protected string GetIndentation(int depth)
    {
        return new string('  ', depth);
    }
}

public class Button : UIComponent
{
    private readonly string text;
    private readonly string action;
    
    public Button(string name, string text, string action, int x = 0, int y = 0) 
        : base(name, x, y, text.Length * 8 + 20, 30)
    {
        text = text ?? name;
        action = action ?? "NoAction";
    }
    
    public string Text => text;
    public string Action => action;
    
    public override void Render(int depth = 0)
    {
        if (!visible) return;
        
        var indent = GetIndentation(depth);
        Console.WriteLine($"{indent}🔘 Button '{name}': \"{text}\" @ ({position.X},{position.Y}) [{size.Width}x{size.Height}] -> {action}");
    }
    
    public override void SetVisible(bool visible)
    {
        visible = visible;
    }
    
    public override (int Width, int Height) GetTotalSize()
    {
        return visible ? size : (0, 0);
    }
}

public class TextBox : UIComponent
{
    private string content;
    private readonly bool multiline;
    
    public TextBox(string name, string placeholder = "", bool multiline = false, int x = 0, int y = 0) 
        : base(name, x, y, 200, multiline ? 100 : 25)
    {
        content = placeholder ?? string.Empty;
        multiline = multiline;
    }
    
    public string Content => content;
    public bool Multiline => multiline;
    
    public void SetContent(string content)
    {
        content = content ?? string.Empty;
    }
    
    public override void Render(int depth = 0)
    {
        if (!visible) return;
        
        var indent = GetIndentation(depth);
        var type = multiline ? "TextArea" : "TextBox";
        Console.WriteLine($"{indent}📝 {type} '{name}': \"{content}\" @ ({position.X},{position.Y}) [{size.Width}x{size.Height}]");
    }
    
    public override void SetVisible(bool visible)
    {
        visible = visible;
    }
    
    public override (int Width, int Height) GetTotalSize()
    {
        return visible ? size : (0, 0);
    }
}

public class Panel : UIComponent
{
    private readonly List<UIComponent> children;
    private readonly string backgroundColor;
    
    public Panel(string name, string backgroundColor = "#FFFFFF", int x = 0, int y = 0, int width = 400, int height = 300) 
        : base(name, x, y, width, height)
    {
        children = new List<UIComponent>();
        backgroundColor = backgroundColor;
    }
    
    public string BackgroundColor => backgroundColor;
    public IReadOnlyList<UIComponent> Children => children.AsReadOnly();
    
    public override void Render(int depth = 0)
    {
        if (!visible) return;
        
        var indent = GetIndentation(depth);
        Console.WriteLine($"{indent}📋 Panel '{name}' [{backgroundColor}] @ ({position.X},{position.Y}) [{size.Width}x{size.Height}]");
        
        foreach (var child in children)
        {
            child.Render(depth + 1);
        }
    }
    
    public override void SetVisible(bool visible)
    {
        visible = visible;
        
        // Propagate visibility to children
        foreach (var child in children)
        {
            child.SetVisible(visible);
        }
    }
    
    public override (int Width, int Height) GetTotalSize()
    {
        if (!visible) return (0, 0);
        
        var maxWidth = children.Count > 0 ? children.Max(c => c.Position.X + c.GetTotalSize().Width) : 0;
        var maxHeight = children.Count > 0 ? children.Max(c => c.Position.Y + c.GetTotalSize().Height) : 0;
        
        return (Math.Max(size.Width, maxWidth), Math.Max(size.Height, maxHeight));
    }
    
    public override void Add(UIComponent component)
    {
        if (component == null)
            throw new ArgumentNullException(nameof(component));
        
        children.Add(component);
    }
    
    public override void Remove(UIComponent component)
    {
        children.Remove(component);
    }
}
```

**Usage**:

```csharp
using System;
using System.Linq;

class Program
{
    static void Main()
    {
        Console.WriteLine("=== File System Composite ===");
        
        // Create root directory
        var root = new Directory("root");
        
        // Create documents directory
        var documents = new Directory("Documents");
        documents.Add(new File("resume.pdf", "John Doe's Resume Content"));
        documents.Add(new File("cover-letter.doc", "Dear Hiring Manager..."));
        
        // Create projects directory with subdirectories
        var projects = new Directory("Projects");
        var webProject = new Directory("WebApp");
        webProject.Add(new File("index.html", "<html><body>Hello World</body></html>"));
        webProject.Add(new File("style.css", "body { font-family: Arial; }"));
        webProject.Add(new File("script.js", "console.log('Hello from JS');"));
        
        var mobileProject = new Directory("MobileApp");
        mobileProject.Add(new File("MainActivity.java", "public class MainActivity extends Activity { }"));
        mobileProject.Add(new File("AndroidManifest.xml", "<manifest xmlns:android=..."));
        
        projects.Add(webProject);
        projects.Add(mobileProject);
        
        // Add everything to root
        root.Add(documents);
        root.Add(projects);
        root.Add(new File("readme.txt", "Welcome to my file system!"));
        
        // Display entire structure
        root.Display();
        
        Console.WriteLine($"\nTotal Size: {root.GetSize()} bytes");
        Console.WriteLine($"Total Files: {root.GetCount()}");
        
        // Find operations
        Console.WriteLine("\n=== Search Operations ===");
        var found = root.Find("style.css");
        Console.WriteLine($"Found: {found?.Name} at path: {found?.GetPath()}");
        
        // Get all files
        var allFiles = ((Directory)root).GetAllFiles().ToList();
        Console.WriteLine($"\nAll files in system: {string.Join(", ", allFiles.Select(f => f.Name))}");
        
        Console.WriteLine("\n=== Organization Chart Composite ===");
        
        // Create CEO
        var ceo = new Manager("Alice Johnson", "CEO", 200000, "Executive", 50000);
        
        // Create VPs
        var vpEngineering = new Manager("Bob Smith", "VP Engineering", 150000, "Engineering", 30000);
        var vpSales = new Manager("Carol Davis", "VP Sales", 140000, "Sales", 25000);
        
        // Create engineering team
        var techLead = new Manager("David Wilson", "Tech Lead", 120000, "Engineering", 15000);
        var seniorDev1 = new Employee("Eve Brown", "Senior Developer", 95000, "Engineering");
        seniorDev1.AddSkill("C#");
        seniorDev1.AddSkill("React");
        seniorDev1.AddSkill("Azure");
        
        var seniorDev2 = new Employee("Frank Miller", "Senior Developer", 92000, "Engineering");
        seniorDev2.AddSkill("Python");
        seniorDev2.AddSkill("Django");
        seniorDev2.AddSkill("AWS");
        
        var juniorDev = new Employee("Grace Lee", "Junior Developer", 65000, "Engineering");
        juniorDev.AddSkill("JavaScript");
        juniorDev.AddSkill("Node.js");
        
        techLead.Add(seniorDev1);
        techLead.Add(seniorDev2);
        techLead.Add(juniorDev);
        
        vpEngineering.Add(techLead);
        
        // Create sales team
        var salesManager = new Manager("Henry Taylor", "Sales Manager", 90000, "Sales", 10000);
        var salesRep1 = new Employee("Ivy Chen", "Sales Representative", 60000, "Sales");
        var salesRep2 = new Employee("Jack Roberts", "Sales Representative", 58000, "Sales");
        
        salesManager.Add(salesRep1);
        salesManager.Add(salesRep2);
        vpSales.Add(salesManager);
        
        // Build organization
        ceo.Add(vpEngineering);
        ceo.Add(vpSales);
        
        // Display organization
        ceo.DisplayHierarchy();
        
        Console.WriteLine($"\nCompany Stats:");
        Console.WriteLine($"Total Team Size: {ceo.GetTeamSize()}");
        Console.WriteLine($"Total Payroll: ${ceo.GetTotalSalary():N0}/year");
        
        // Give raises
        Console.WriteLine("\n=== Annual Reviews ===");
        vpEngineering.GiveRaise(8); // VP gets 8%, team gets 4%
        
        Console.WriteLine("\n=== UI Component Composite ===");
        
        // Create main window
        var mainWindow = new Panel("MainWindow", "#F0F0F0", 0, 0, 800, 600);
        
        // Create header panel
        var header = new Panel("Header", "#3498DB", 0, 0, 800, 80);
        header.Add(new Button("LoginBtn", "Login", "ShowLoginDialog", 650, 20));
        header.Add(new Button("SignupBtn", "Sign Up", "ShowSignupDialog", 720, 20));
        
        // Create content panel
        var content = new Panel("Content", "#FFFFFF", 0, 80, 800, 440);
        
        // Create sidebar
        var sidebar = new Panel("Sidebar", "#34495E", 0, 0, 200, 440);
        sidebar.Add(new Button("HomeBtn", "Home", "NavigateHome", 10, 20));
        sidebar.Add(new Button("ProfileBtn", "Profile", "NavigateProfile", 10, 60));
        sidebar.Add(new Button("SettingsBtn", "Settings", "NavigateSettings", 10, 100));
        
        // Create main area
        var mainArea = new Panel("MainArea", "#FFFFFF", 200, 0, 600, 440);
        mainArea.Add(new TextBox("SearchBox", "Search...", false, 20, 20));
        mainArea.Add(new Button("SearchBtn", "Search", "PerformSearch", 240, 20));
        mainArea.Add(new TextBox("ContentArea", "Main content goes here...", true, 20, 60));
        
        content.Add(sidebar);
        content.Add(mainArea);
        
        // Create footer
        var footer = new Panel("Footer", "#95A5A6", 0, 520, 800, 80);
        footer.Add(new Button("HelpBtn", "Help", "ShowHelp", 20, 20));
        footer.Add(new Button("AboutBtn", "About", "ShowAbout", 80, 20));
        
        // Assemble UI
        mainWindow.Add(header);
        mainWindow.Add(content);
        mainWindow.Add(footer);
        
        // Render UI
        mainWindow.Render();
        
        var totalSize = mainWindow.GetTotalSize();
        Console.WriteLine($"\nTotal UI Size: {totalSize.Width} x {totalSize.Height}");
        
        // Test visibility
        Console.WriteLine("\n=== Hiding Sidebar ===");
        sidebar.SetVisible(false);
        content.Render();
        
        Console.WriteLine("\n=== Composite Pattern Benefits ===");
        Console.WriteLine("✅ Uniform treatment of individual and composite objects");
        Console.WriteLine("✅ Easy to add new component types");
        Console.WriteLine("✅ Recursive structure handling");
        Console.WriteLine("✅ Client simplification - same interface for all components");
        Console.WriteLine("✅ Flexible tree structures");
    }
}
```

**Notes**:

- Treats individual objects (Leaf) and compositions (Composite) uniformly
- Recursive structure makes it easy to work with tree hierarchies
- Client code doesn't need to distinguish between leaves and composites
- Easy to add new component types without changing existing code
- Operations can be applied to entire structures with simple recursive calls
- Useful for building GUI frameworks, file systems, organizational structures
- Can implement operations like copy, move, delete uniformly across the structure
- May make design overly general if you only need simple structures
- Related patterns: [Decorator](decorator.md), [Visitor](visitor.md), [Iterator](iterator.md)

**Prerequisites**:

- .NET Framework 2.0+ or .NET Core
- Understanding of recursive data structures
- Knowledge of interfaces and inheritance
- Familiarity with tree traversal concepts

**References**:

- Gang of Four Design Patterns book
- [Microsoft Docs: Composite Pattern](https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/composite)

---

*Created: 2025-10-31*  
*Last Updated: 2025-10-31*  
*Tags: #composite #structural #tree #hierarchy*
