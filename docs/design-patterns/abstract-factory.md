# Abstract Factory Pattern

**Description**: Provides an interface for creating families of related objects without specifying their concrete classes. Useful when you need to create multiple related products that work together.

**Language/Technology**: C# / .NET

**Code**:

```csharp
using System;
using System.Collections.Generic;

// Abstract Products
public interface IButton
{
    void Render();
    void Click();
}

public interface ICheckbox
{
    void Render();
    void Toggle();
}

public interface ITextField
{
    void Render();
    void SetText(string text);
}

// Windows UI Products
public class WindowsButton : IButton
{
    public void Render()
    {
        Console.WriteLine("Rendering Windows-style button");
    }
    
    public void Click()
    {
        Console.WriteLine("Windows button clicked - showing dialog");
    }
}

public class WindowsCheckbox : ICheckbox
{
    public void Render()
    {
        Console.WriteLine("Rendering Windows-style checkbox");
    }
    
    public void Toggle()
    {
        Console.WriteLine("Windows checkbox toggled");
    }
}

public class WindowsTextField : ITextField
{
    public void Render()
    {
        Console.WriteLine("Rendering Windows-style text field");
    }
    
    public void SetText(string text)
    {
        Console.WriteLine($"Windows text field set to: {text}");
    }
}

// MacOS UI Products
public class MacOSButton : IButton
{
    public void Render()
    {
        Console.WriteLine("Rendering macOS-style button");
    }
    
    public void Click()
    {
        Console.WriteLine("macOS button clicked - smooth animation");
    }
}

public class MacOSCheckbox : ICheckbox
{
    public void Render()
    {
        Console.WriteLine("Rendering macOS-style checkbox");
    }
    
    public void Toggle()
    {
        Console.WriteLine("macOS checkbox toggled with animation");
    }
}

public class MacOSTextField : ITextField
{
    public void Render()
    {
        Console.WriteLine("Rendering macOS-style text field");
    }
    
    public void SetText(string text)
    {
        Console.WriteLine($"macOS text field set to: {text}");
    }
}

// Linux UI Products
public class LinuxButton : IButton
{
    public void Render()
    {
        Console.WriteLine("Rendering Linux GTK-style button");
    }
    
    public void Click()
    {
        Console.WriteLine("Linux button clicked");
    }
}

public class LinuxCheckbox : ICheckbox
{
    public void Render()
    {
        Console.WriteLine("Rendering Linux GTK-style checkbox");
    }
    
    public void Toggle()
    {
        Console.WriteLine("Linux checkbox toggled");
    }
}

public class LinuxTextField : ITextField
{
    public void Render()
    {
        Console.WriteLine("Rendering Linux GTK-style text field");
    }
    
    public void SetText(string text)
    {
        Console.WriteLine($"Linux text field set to: {text}");
    }
}

// Abstract Factory Interface
public interface IUIFactory
{
    IButton CreateButton();
    ICheckbox CreateCheckbox();
    ITextField CreateTextField();
}

// Concrete Factories
public class WindowsUIFactory : IUIFactory
{
    public IButton CreateButton() => new WindowsButton();
    public ICheckbox CreateCheckbox() => new WindowsCheckbox();
    public ITextField CreateTextField() => new WindowsTextField();
}

public class MacOSUIFactory : IUIFactory
{
    public IButton CreateButton() => new MacOSButton();
    public ICheckbox CreateCheckbox() => new MacOSCheckbox();
    public ITextField CreateTextField() => new MacOSTextField();
}

public class LinuxUIFactory : IUIFactory
{
    public IButton CreateButton() => new LinuxButton();
    public ICheckbox CreateCheckbox() => new LinuxCheckbox();
    public ITextField CreateTextField() => new LinuxTextField();
}

// Client code that uses the factory
public class Application
{
    private readonly IUIFactory uiFactory;
    private readonly List<IButton> buttons = new();
    private readonly List<ICheckbox> checkboxes = new();
    private readonly List<ITextField> textFields = new();
    
    public Application(IUIFactory uiFactory)
    {
        uiFactory = uiFactory;
    }
    
    public void CreateUI()
    {
        // Create UI elements using the factory
        var button = uiFactory.CreateButton();
        var checkbox = uiFactory.CreateCheckbox();
        var textField = uiFactory.CreateTextField();
        
        buttons.Add(button);
        checkboxes.Add(checkbox);
        textFields.Add(textField);
        
        Console.WriteLine("UI Created with factory: " + uiFactory.GetType().Name);
    }
    
    public void RenderUI()
    {
        Console.WriteLine("Rendering UI...");
        foreach (var button in buttons)
            button.Render();
        foreach (var checkbox in checkboxes)
            checkbox.Render();
        foreach (var textField in textFields)
            textField.Render();
    }
    
    public void InteractWithUI()
    {
        if (buttons.Count > 0)
            _buttons[0].Click();
        if (checkboxes.Count > 0)
            _checkboxes[0].Toggle();
        if (textFields.Count > 0)
            _textFields[0].SetText("Hello World");
    }
}

// Factory Provider for runtime factory selection
public static class UIFactoryProvider
{
    public static IUIFactory GetFactory(OSType osType)
    {
        return osType switch
        {
            OSType.Windows => new WindowsUIFactory(),
            OSType.MacOS => new MacOSUIFactory(),
            OSType.Linux => new LinuxUIFactory(),
            _ => throw new ArgumentException($"Unsupported OS type: {osType}")
        };
    }
    
    public static IUIFactory GetFactory()
    {
        // Auto-detect OS (simplified)
        var os = Environment.OSVersion.Platform;
        return os switch
        {
            PlatformID.Win32NT => new WindowsUIFactory(),
            PlatformID.Unix => new LinuxUIFactory(),
            PlatformID.MacOSX => new MacOSUIFactory(),
            _ => new WindowsUIFactory() // Default fallback
        };
    }
}

public enum OSType
{
    Windows,
    MacOS,
    Linux
}
```

**Usage**:

```csharp
using System;

class Program
{
    static void Main()
    {
        // Create application with different UI factories
        Console.WriteLine("=== Windows UI ===");
        var windowsApp = new Application(new WindowsUIFactory());
        DemoApplication(windowsApp);
        
        Console.WriteLine("\n=== macOS UI ===");
        var macApp = new Application(new MacOSUIFactory());
        DemoApplication(macApp);
        
        Console.WriteLine("\n=== Linux UI ===");
        var linuxApp = new Application(new LinuxUIFactory());
        DemoApplication(linuxApp);
        
        Console.WriteLine("\n=== Auto-detected OS UI ===");
        var autoFactory = UIFactoryProvider.GetFactory();
        var autoApp = new Application(autoFactory);
        DemoApplication(autoApp);
        
        Console.WriteLine("\n=== Runtime OS Selection ===");
        var selectedFactory = UIFactoryProvider.GetFactory(OSType.MacOS);
        var selectedApp = new Application(selectedFactory);
        DemoApplication(selectedApp);
    }
    
    static void DemoApplication(Application app)
    {
        app.CreateUI();
        app.RenderUI();
        app.InteractWithUI();
    }
}
```

**Notes**:

- Creates families of related objects (UI controls for specific platforms)
- Ensures all created objects are compatible with each other
- Easy to add new product families (new OS support)
- Client code doesn't depend on concrete classes
- Factory provider pattern allows runtime factory selection
- Auto-detection capability for OS-specific factories
- Follows Open/Closed Principle - open for extension, closed for modification
- Related patterns: [Factory Method](factory-method.md), [Builder Pattern](builder.md)

**Prerequisites**:

- .NET Framework 2.0+ or .NET Core
- Understanding of interfaces and polymorphism

**References**:

- Gang of Four Design Patterns book
- [Microsoft Docs: Abstract Factory Pattern](https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/abstract-factory)

---

*Created: 2025-10-31*  
*Last Updated: 2025-10-31*  
*Tags: #abstract-factory #creational #families #polymorphism*
