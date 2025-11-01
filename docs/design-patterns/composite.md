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
    protected string _name;
    protected DateTime _created;
    protected DateTime _modified;
    
    protected FileSystemComponent(string name)
    {
        _name = name ?? throw new ArgumentNullException(nameof(name));
        _created = DateTime.Now;
        _modified = DateTime.Now;
    }
    
    public string Name => _name;
    public DateTime Created => _created;
    public DateTime Modified => _modified;
    
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
        _modified = DateTime.Now;
    }
    
    protected string GetIndentation(int depth)
    {
        return new string(' ', depth * 2);
    }
}

// Leaf - represents leaf objects (files)
public class File : FileSystemComponent
{
    private byte[] _content;
    private readonly string _extension;
    
    public File(string name, byte[] content = null) : base(name)
    {
        _content = content ?? Array.Empty<byte>();
        _extension = System.IO.Path.GetExtension(name);
    }
    
    public File(string name, string textContent) : base(name)
    {
        _content = Encoding.UTF8.GetBytes(textContent ?? string.Empty);
        _extension = System.IO.Path.GetExtension(name);
    }
    
    public string Extension => _extension;
    public byte[] Content => _content;
    public string TextContent => Encoding.UTF8.GetString(_content);
    
    public override long GetSize() => _content.Length;
    
    public override int GetCount() => 1;
    
    public override void Display(int depth = 0)
    {
        var indent = GetIndentation(depth);
        var sizeStr = GetSize() < 1024 ? $"{GetSize()}B" : $"{GetSize() / 1024.0:F1}KB";
        Console.WriteLine($"{indent}📄 {_name} ({sizeStr}) [{_extension}]");
    }
    
    public override string GetPath()
    {
        // Files don't know their parent path in this simple implementation
        return _name;
    }
    
    public void WriteContent(string content)
    {
        _content = Encoding.UTF8.GetBytes(content ?? string.Empty);
        UpdateModified();
    }
    
    public void WriteContent(byte[] content)
    {
        _content = content ?? Array.Empty<byte>();
        UpdateModified();
    }
    
    public override FileSystemComponent Clone()
    {
        return new File(_name, (byte[])_content.Clone());
    }
}

// Composite - represents composite objects (directories)
public class Directory : FileSystemComponent
{
    private readonly List<FileSystemComponent> _children;
    private Directory _parent;
    
    public Directory(string name) : base(name)
    {
        _children = new List<FileSystemComponent>();
    }
    
    public Directory Parent => _parent;
    public IReadOnlyList<FileSystemComponent> Children => _children.AsReadOnly();
    
    public override long GetSize() => _children.Sum(child => child.GetSize());
    
    public override int GetCount() => _children.Sum(child => child.GetCount());
    
    public override void Display(int depth = 0)
    {
        var indent = GetIndentation(depth);
        var sizeStr = GetSize() < 1024 ? $"{GetSize()}B" : 
                     GetSize() < 1024 * 1024 ? $"{GetSize() / 1024.0:F1}KB" : 
                     $"{GetSize() / (1024.0 * 1024.0):F1}MB";
        
        Console.WriteLine($"{indent}📁 {_name}/ ({GetCount()} items, {sizeStr})");
        
        foreach (var child in _children)
        {
            child.Display(depth + 1);
        }
    }
    
    public override string GetPath()
    {
        if (_parent == null)
            return _name;
        
        return $"{_parent.GetPath()}/{_name}";
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
        
        _children.Add(component);
        
        if (component is Directory childDir)
        {
            childDir._parent = this;
        }
        
        UpdateModified();
    }
    
    public override void Remove(FileSystemComponent component)
    {
        if (_children.Remove(component))
        {
            if (component is Directory dir)
            {
                dir._parent = null;
            }
            UpdateModified();
        }
    }
    
    public override FileSystemComponent GetChild(int index)
    {
        if (index < 0 || index >= _children.Count)
            throw new ArgumentOutOfRangeException(nameof(index));
        
        return _children[index];
    }
    
    public override IEnumerable<FileSystemComponent> GetChildren()
    {
        return _children.AsEnumerable();
    }
    
    private bool IsAncestor(Directory potentialAncestor)
    {
        var current = _parent;
        while (current != null)
        {
            if (current == potentialAncestor)
                return true;
            current = current._parent;
        }
        return false;
    }
    
    public FileSystemComponent Find(string name)
    {
        if (_name.Equals(name, StringComparison.OrdinalIgnoreCase))
            return this;
        
        foreach (var child in _children)
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
        foreach (var child in _children)
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
        var clonedDir = new Directory(_name);
        
        foreach (var child in _children)
        {
            clonedDir.Add(child.Clone());
        }
        
        return clonedDir;
    }
}

// Advanced Composite: Organizational Chart
public abstract class OrganizationComponent
{
    protected string _name;
    protected string _position;
    protected decimal _salary;
    protected DateTime _hireDate;
    
    protected OrganizationComponent(string name, string position, decimal salary)
    {
        _name = name ?? throw new ArgumentNullException(nameof(name));
        _position = position ?? throw new ArgumentNullException(nameof(position));
        _salary = salary;
        _hireDate = DateTime.Now;
    }
    
    public string Name => _name;
    public string Position => _position;
    public decimal Salary => _salary;
    public DateTime HireDate => _hireDate;
    
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
    private readonly string _department;
    private readonly List<string> _skills;
    
    public Employee(string name, string position, decimal salary, string department) 
        : base(name, position, salary)
    {
        _department = department ?? "General";
        _skills = new List<string>();
    }
    
    public string Department => _department;
    public IReadOnlyList<string> Skills => _skills.AsReadOnly();
    
    public void AddSkill(string skill)
    {
        if (!string.IsNullOrWhiteSpace(skill) && !_skills.Contains(skill))
        {
            _skills.Add(skill);
        }
    }
    
    public override decimal GetTotalSalary() => _salary;
    
    public override int GetTeamSize() => 1;
    
    public override void DisplayHierarchy(int depth = 0)
    {
        var indent = GetIndentation(depth);
        Console.WriteLine($"{indent}👤 {_name} - {_position} ({_department})");
        Console.WriteLine($"{new string(' ', indent.Length)}   💰 ${_salary:N0}/year | Skills: {string.Join(", ", _skills)}");
    }
    
    public override void GiveRaise(decimal percentage)
    {
        _salary *= (1 + percentage / 100);
        Console.WriteLine($"💰 {_name} received a {percentage}% raise. New salary: ${_salary:N0}");
    }
}

public class Manager : OrganizationComponent
{
    private readonly List<OrganizationComponent> _subordinates;
    private readonly string _department;
    private readonly decimal _bonus;
    
    public Manager(string name, string position, decimal salary, string department, decimal bonus = 0) 
        : base(name, position, salary)
    {
        _subordinates = new List<OrganizationComponent>();
        _department = department ?? "Management";
        _bonus = bonus;
    }
    
    public string Department => _department;
    public decimal Bonus => _bonus;
    public IReadOnlyList<OrganizationComponent> Subordinates => _subordinates.AsReadOnly();
    
    public override decimal GetTotalSalary()
    {
        return _salary + _bonus + _subordinates.Sum(s => s.GetTotalSalary());
    }
    
    public override int GetTeamSize()
    {
        return 1 + _subordinates.Sum(s => s.GetTeamSize());
    }
    
    public override void DisplayHierarchy(int depth = 0)
    {
        var indent = GetIndentation(depth);
        var bonusText = _bonus > 0 ? $" (+ ${_bonus:N0} bonus)" : "";
        Console.WriteLine($"{indent}👨‍💼 {_name} - {_position} ({_department})");
        Console.WriteLine($"{new string(' ', indent.Length)}   💰 ${_salary:N0}/year{bonusText} | Team: {GetTeamSize()} people | Budget: ${GetTotalSalary():N0}");
        
        foreach (var subordinate in _subordinates)
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
        
        _subordinates.Add(component);
    }
    
    public override void Remove(OrganizationComponent component)
    {
        _subordinates.Remove(component);
    }
    
    public override IEnumerable<OrganizationComponent> GetSubordinates()
    {
        return _subordinates.AsEnumerable();
    }
    
    public override void GiveRaise(decimal percentage)
    {
        // Manager raises affect the entire team
        _salary *= (1 + percentage / 100);
        Console.WriteLine($"💰 {_name} (Manager) received a {percentage}% raise. New salary: ${_salary:N0}");
        
        // Give smaller raise to subordinates
        foreach (var subordinate in _subordinates)
        {
            subordinate.GiveRaise(percentage * 0.5m); // Half the manager's raise
        }
    }
}

// Composite Pattern with UI Components
public abstract class UIComponent
{
    protected string _name;
    protected bool _visible;
    protected (int X, int Y) _position;
    protected (int Width, int Height) _size;
    
    protected UIComponent(string name, int x = 0, int y = 0, int width = 100, int height = 20)
    {
        _name = name ?? throw new ArgumentNullException(nameof(name));
        _visible = true;
        _position = (x, y);
        _size = (width, height);
    }
    
    public string Name => _name;
    public bool Visible => _visible;
    public (int X, int Y) Position => _position;
    public (int Width, int Height) Size => _size;
    
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
        _position = (x, y);
    }
    
    public void Resize(int width, int height)
    {
        _size = (width, height);
    }
    
    protected string GetIndentation(int depth)
    {
        return new string('  ', depth);
    }
}

public class Button : UIComponent
{
    private readonly string _text;
    private readonly string _action;
    
    public Button(string name, string text, string action, int x = 0, int y = 0) 
        : base(name, x, y, text.Length * 8 + 20, 30)
    {
        _text = text ?? name;
        _action = action ?? "NoAction";
    }
    
    public string Text => _text;
    public string Action => _action;
    
    public override void Render(int depth = 0)
    {
        if (!_visible) return;
        
        var indent = GetIndentation(depth);
        Console.WriteLine($"{indent}🔘 Button '{_name}': \"{_text}\" @ ({_position.X},{_position.Y}) [{_size.Width}x{_size.Height}] -> {_action}");
    }
    
    public override void SetVisible(bool visible)
    {
        _visible = visible;
    }
    
    public override (int Width, int Height) GetTotalSize()
    {
        return _visible ? _size : (0, 0);
    }
}

public class TextBox : UIComponent
{
    private string _content;
    private readonly bool _multiline;
    
    public TextBox(string name, string placeholder = "", bool multiline = false, int x = 0, int y = 0) 
        : base(name, x, y, 200, multiline ? 100 : 25)
    {
        _content = placeholder ?? string.Empty;
        _multiline = multiline;
    }
    
    public string Content => _content;
    public bool Multiline => _multiline;
    
    public void SetContent(string content)
    {
        _content = content ?? string.Empty;
    }
    
    public override void Render(int depth = 0)
    {
        if (!_visible) return;
        
        var indent = GetIndentation(depth);
        var type = _multiline ? "TextArea" : "TextBox";
        Console.WriteLine($"{indent}📝 {type} '{_name}': \"{_content}\" @ ({_position.X},{_position.Y}) [{_size.Width}x{_size.Height}]");
    }
    
    public override void SetVisible(bool visible)
    {
        _visible = visible;
    }
    
    public override (int Width, int Height) GetTotalSize()
    {
        return _visible ? _size : (0, 0);
    }
}

public class Panel : UIComponent
{
    private readonly List<UIComponent> _children;
    private readonly string _backgroundColor;
    
    public Panel(string name, string backgroundColor = "#FFFFFF", int x = 0, int y = 0, int width = 400, int height = 300) 
        : base(name, x, y, width, height)
    {
        _children = new List<UIComponent>();
        _backgroundColor = backgroundColor;
    }
    
    public string BackgroundColor => _backgroundColor;
    public IReadOnlyList<UIComponent> Children => _children.AsReadOnly();
    
    public override void Render(int depth = 0)
    {
        if (!_visible) return;
        
        var indent = GetIndentation(depth);
        Console.WriteLine($"{indent}📋 Panel '{_name}' [{_backgroundColor}] @ ({_position.X},{_position.Y}) [{_size.Width}x{_size.Height}]");
        
        foreach (var child in _children)
        {
            child.Render(depth + 1);
        }
    }
    
    public override void SetVisible(bool visible)
    {
        _visible = visible;
        
        // Propagate visibility to children
        foreach (var child in _children)
        {
            child.SetVisible(visible);
        }
    }
    
    public override (int Width, int Height) GetTotalSize()
    {
        if (!_visible) return (0, 0);
        
        var maxWidth = _children.Count > 0 ? _children.Max(c => c.Position.X + c.GetTotalSize().Width) : 0;
        var maxHeight = _children.Count > 0 ? _children.Max(c => c.Position.Y + c.GetTotalSize().Height) : 0;
        
        return (Math.Max(_size.Width, maxWidth), Math.Max(_size.Height, maxHeight));
    }
    
    public override void Add(UIComponent component)
    {
        if (component == null)
            throw new ArgumentNullException(nameof(component));
        
        _children.Add(component);
    }
    
    public override void Remove(UIComponent component)
    {
        _children.Remove(component);
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
