using System.Runtime.InteropServices;

namespace DesignPatterns.AbstractFactory;

// Abstract Products - Define interfaces for UI elements
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

// Windows UI Products - Windows-specific implementations
public class WindowsButton : IButton
{
    public void Render()
    {
        Console.WriteLine("Rendering Windows-style button with Win32 API styling");
    }
    
    public void Click()
    {
        Console.WriteLine("Windows button clicked - showing dialog with system font");
    }
}

public class WindowsCheckbox : ICheckbox
{
    public void Render()
    {
        Console.WriteLine("Rendering Windows-style checkbox with Fluent Design");
    }
    
    public void Toggle()
    {
        Console.WriteLine("Windows checkbox toggled with visual feedback");
    }
}

public class WindowsTextField : ITextField
{
    public void Render()
    {
        Console.WriteLine("Rendering Windows-style text field with rounded corners");
    }
    
    public void SetText(string text)
    {
        Console.WriteLine($"Windows text field set to: '{text}' (with spell check)");
    }
}

// MacOS UI Products - Apple-specific implementations
public class MacOSButton : IButton
{
    public void Render()
    {
        Console.WriteLine("Rendering macOS-style button with Aqua interface");
    }
    
    public void Click()
    {
        Console.WriteLine("macOS button clicked - smooth animation with haptic feedback");
    }
}

public class MacOSCheckbox : ICheckbox
{
    public void Render()
    {
        Console.WriteLine("Rendering macOS-style checkbox with system accent color");
    }
    
    public void Toggle()
    {
        Console.WriteLine("macOS checkbox toggled with Core Animation transition");
    }
}

public class MacOSTextField : ITextField
{
    public void Render()
    {
        Console.WriteLine("Rendering macOS-style text field with SF Pro font");
    }
    
    public void SetText(string text)
    {
        Console.WriteLine($"macOS text field set to: '{text}' (with Smart Quotes)");
    }
}

// Linux UI Products - GTK/Qt-specific implementations
public class LinuxButton : IButton
{
    public void Render()
    {
        Console.WriteLine("Rendering Linux GTK-style button with Adwaita theme");
    }
    
    public void Click()
    {
        Console.WriteLine("Linux button clicked - desktop environment integration");
    }
}

public class LinuxCheckbox : ICheckbox
{
    public void Render()
    {
        Console.WriteLine("Rendering Linux GTK-style checkbox with theme support");
    }
    
    public void Toggle()
    {
        Console.WriteLine("Linux checkbox toggled following GNOME HIG guidelines");
    }
}

public class LinuxTextField : ITextField
{
    public void Render()
    {
        Console.WriteLine("Rendering Linux GTK-style text field with system theme");
    }
    
    public void SetText(string text)
    {
        Console.WriteLine($"Linux text field set to: '{text}' (with input method support)");
    }
}

// Abstract Factory Interface - Defines creation methods for product families
public interface IUIFactory
{
    IButton CreateButton();
    ICheckbox CreateCheckbox();
    ITextField CreateTextField();
}

// Concrete Factories - Platform-specific implementations
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

// Client Application - Uses factory without knowing specific implementations
public class Application
{
    private readonly IUIFactory uiFactory;
    private readonly List<IButton> buttons = new();
    private readonly List<ICheckbox> checkboxes = new();
    private readonly List<ITextField> textFields = new();
    
    public Application(IUIFactory uiFactory)
    {
        this.uiFactory = uiFactory ?? throw new ArgumentNullException(nameof(uiFactory));
    }
    
    public void CreateUI()
    {
        // Create UI elements using the factory - ensures all elements belong to same family
        var button = uiFactory.CreateButton();
        var checkbox = uiFactory.CreateCheckbox();
        var textField = uiFactory.CreateTextField();
        
        buttons.Add(button);
        checkboxes.Add(checkbox);
        textFields.Add(textField);
        
        Console.WriteLine($"✅ UI Created with factory: {uiFactory.GetType().Name}");
    }
    
    public void RenderUI()
    {
        Console.WriteLine("🎨 Rendering UI components...");
        
        foreach (var button in buttons)
            button.Render();
            
        foreach (var checkbox in checkboxes)
            checkbox.Render();
            
        foreach (var textField in textFields)
            textField.Render();
            
        Console.WriteLine("   Rendering complete!");
    }
    
    public void InteractWithUI()
    {
        Console.WriteLine("🖱️ Simulating user interactions...");
        
        if (buttons.Count > 0)
            buttons[0].Click();
            
        if (checkboxes.Count > 0)
            checkboxes[0].Toggle();
            
        if (textFields.Count > 0)
            textFields[0].SetText("Hello Cross-Platform World!");
    }
    
    public void ShowUIStats()
    {
        Console.WriteLine($"📊 UI Statistics:");
        Console.WriteLine($"   Buttons: {buttons.Count}");
        Console.WriteLine($"   Checkboxes: {checkboxes.Count}");
        Console.WriteLine($"   Text Fields: {textFields.Count}");
        Console.WriteLine($"   Factory Type: {uiFactory.GetType().Name}");
    }
}

// Factory Provider - Handles factory selection and OS detection
public static class UIFactoryProvider
{
    public static IUIFactory GetFactory(OSType osType)
    {
        return osType switch
        {
            OSType.Windows => new WindowsUIFactory(),
            OSType.MacOS => new MacOSUIFactory(),
            OSType.Linux => new LinuxUIFactory(),
            _ => throw new ArgumentException($"Unsupported OS type: {osType}", nameof(osType))
        };
    }
    
    public static IUIFactory GetFactory()
    {
        // Auto-detect OS using Runtime Information
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return new WindowsUIFactory();
            
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return new MacOSUIFactory();
            
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return new LinuxUIFactory();
            
        // Fallback to Windows if detection fails
        Console.WriteLine("⚠️ OS detection failed, defaulting to Windows UI");
        return new WindowsUIFactory();
    }
    
    public static string GetCurrentOSName()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return "Windows";
            
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return "macOS";
            
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return "Linux";
            
        return "Unknown";
    }
    
    public static IEnumerable<OSType> GetSupportedPlatforms()
    {
        return Enum.GetValues<OSType>();
    }
}

public enum OSType
{
    Windows,
    MacOS,
    Linux
}

// Demo Service to showcase the Abstract Factory pattern
public class AbstractFactoryDemoService
{
    public void RunCompleteDemo()
    {
        Console.WriteLine("Abstract Factory Pattern - Cross-Platform UI Demo");
        Console.WriteLine("=================================================");
        Console.WriteLine();
        
        ShowPatternOverview();
        DemonstrateAllPlatforms();
        DemonstrateAutoDetection();
        DemonstrateRuntimeSelection();
        ShowPatternBenefits();
    }
    
    private static void ShowPatternOverview()
    {
        Console.WriteLine("Pattern Overview:");
        Console.WriteLine("• Creates families of related objects (UI controls for specific platforms)");
        Console.WriteLine("• Ensures all created objects are compatible with each other");
        Console.WriteLine("• Client code doesn't depend on concrete implementations");
        Console.WriteLine($"• Current OS detected as: {UIFactoryProvider.GetCurrentOSName()}");
        Console.WriteLine();
    }
    
    private static void DemonstrateAllPlatforms()
    {
        Console.WriteLine("1. Demonstrating All Platform UI Factories");
        Console.WriteLine("==========================================");
        
        var platformFactories = new[]
        {
            (OSType.Windows, "Windows 11 Fluent Design"),
            (OSType.MacOS, "macOS Big Sur+ Design"),
            (OSType.Linux, "GTK4/Adwaita Design")
        };
        
        foreach (var (osType, description) in platformFactories)
        {
            Console.WriteLine($"\n--- {osType} Platform ({description}) ---");
            var factory = UIFactoryProvider.GetFactory(osType);
            var app = new Application(factory);
            DemoApplication(app);
        }
    }
    
    private static void DemonstrateAutoDetection()
    {
        Console.WriteLine("\n\n2. Auto-Detected OS UI Factory");
        Console.WriteLine("==============================");
        
        var autoFactory = UIFactoryProvider.GetFactory();
        var autoApp = new Application(autoFactory);
        
        Console.WriteLine($"🔍 Auto-detected OS: {UIFactoryProvider.GetCurrentOSName()}");
        Console.WriteLine($"🏭 Selected Factory: {autoFactory.GetType().Name}");
        
        DemoApplication(autoApp);
    }
    
    private static void DemonstrateRuntimeSelection()
    {
        Console.WriteLine("\n\n3. Runtime Platform Selection");
        Console.WriteLine("=============================");
        
        Console.WriteLine("Supported platforms:");
        foreach (var platform in UIFactoryProvider.GetSupportedPlatforms())
        {
            Console.WriteLine($"  • {platform}");
        }
        
        // Simulate user choosing macOS
        var selectedPlatform = OSType.MacOS;
        Console.WriteLine($"\n👤 User selected: {selectedPlatform}");
        
        var selectedFactory = UIFactoryProvider.GetFactory(selectedPlatform);
        var selectedApp = new Application(selectedFactory);
        
        DemoApplication(selectedApp);
    }
    
    private static void DemoApplication(Application app)
    {
        app.CreateUI();
        app.RenderUI();
        app.InteractWithUI();
        app.ShowUIStats();
    }
    
    private static void ShowPatternBenefits()
    {
        Console.WriteLine("\n\n4. Abstract Factory Pattern Benefits");
        Console.WriteLine("===================================");
        
        var benefits = new[]
        {
            "✅ Ensures product family consistency",
            "✅ Easy to add new product families",
            "✅ Isolates concrete classes from client",
            "✅ Supports runtime factory switching",
            "✅ Follows Open/Closed Principle",
            "✅ Promotes code reusability",
            "✅ Facilitates testing with mock factories"
        };
        
        foreach (var benefit in benefits)
        {
            Console.WriteLine($"   {benefit}");
        }
        
        Console.WriteLine("\n🏗️ Related Patterns:");
        Console.WriteLine("   • Factory Method Pattern");
        Console.WriteLine("   • Builder Pattern");
        Console.WriteLine("   • Singleton Pattern (for factory instances)");
        
        Console.WriteLine("\n🎯 Use Cases:");
        Console.WriteLine("   • Cross-platform UI frameworks");
        Console.WriteLine("   • Database provider abstraction");
        Console.WriteLine("   • Theme/skin systems");
        Console.WriteLine("   • Multi-vendor API clients");
    }
}

/// <summary>
/// Demonstrates the Abstract Factory pattern with a cross-platform UI example.
/// Creates families of related UI controls (Button, Checkbox, TextField) for different
/// operating systems (Windows, macOS, Linux) while ensuring compatibility within each family.
/// </summary>
public static class Program
{
    public static void Main()
    {
        var demoService = new AbstractFactoryDemoService();
        demoService.RunCompleteDemo();
    }
}
