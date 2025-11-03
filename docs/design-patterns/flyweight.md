# Flyweight Pattern

**Description**: Uses sharing to support large numbers of fine-grained objects efficiently. Separates intrinsic state (shared) from extrinsic state (context-specific) to minimize memory usage. Useful for scenarios with many similar objects like text editors, game sprites, or UI icons.

**Language/Technology**: C# / .NET

**Code**:

```csharp
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

// Flyweight interface - declares methods through which flyweights can receive and act on extrinsic state
public interface ICharacterFlyweight
{
    void Render(int x, int y, Color color, int fontSize, string fontFamily);
    char GetCharacter();
    int GetIntrinsicMemoryUsage();
}

// Concrete Flyweight - implements Flyweight interface and stores intrinsic state
public class CharacterFlyweight : ICharacterFlyweight
{
    private readonly char character; // Intrinsic state - shared among all instances
    private readonly byte[] bitmap; // Intrinsic state - character shape data
    private readonly int baseWidth;
    private readonly int baseHeight;
    
    public CharacterFlyweight(char character)
    {
        character = character;
        
        // Simulate creating bitmap data for the character
        baseWidth = 8;
        baseHeight = 12;
        bitmap = GenerateCharacterBitmap(character);
        
        Console.WriteLine($"🎨 Created flyweight for character: '{character}'");
    }
    
    public char GetCharacter() => character;
    
    public int GetIntrinsicMemoryUsage()
    {
        return sizeof(char) + bitmap.Length + sizeof(int) * 2;
    }
    
    // Operation that uses both intrinsic and extrinsic state
    public void Render(int x, int y, Color color, int fontSize, string fontFamily)
    {
        // Extrinsic state: x, y, color, fontSize, fontFamily
        // Intrinsic state: character, bitmap, baseWidth, baseHeight
        
        var scaleFactor = fontSize / 12.0; // Base font size is 12
        var width = (int)(baseWidth * scaleFactor);
        var height = (int)(baseHeight * scaleFactor);
        
        Console.WriteLine($"📝 Rendering '{character}' at ({x},{y}) " +
                         $"[{width}x{height}] in {fontFamily} font, color: {color.Name}");
    }
    
    private byte[] GenerateCharacterBitmap(char character)
    {
        // Simulate bitmap generation - in reality this would be complex font rendering
        var random = new Random(character);
        var bitmap = new byte[96]; // 8x12 bitmap
        random.NextBytes(bitmap);
        return bitmap;
    }
}

// Flyweight Factory - creates and manages flyweight objects
public class CharacterFlyweightFactory
{
    private readonly Dictionary<char, ICharacterFlyweight> flyweights;
    private static CharacterFlyweightFactory instance;
    private static readonly object lock = new object();
    
    private CharacterFlyweightFactory()
    {
        flyweights = new Dictionary<char, ICharacterFlyweight>();
    }
    
    public static CharacterFlyweightFactory Instance
    {
        get
        {
            if (instance == null)
            {
                lock (lock)
                {
                    instance ??= new CharacterFlyweightFactory();
                }
            }
            return instance;
        }
    }
    
    public ICharacterFlyweight GetCharacter(char character)
    {
        if (!flyweights.ContainsKey(character))
        {
            _flyweights[character] = new CharacterFlyweight(character);
        }
        
        return _flyweights[character];
    }
    
    public int GetCreatedFlyweightsCount() => flyweights.Count;
    
    public int GetTotalIntrinsicMemoryUsage()
    {
        return flyweights.Values.Sum(fw => fw.GetIntrinsicMemoryUsage());
    }
    
    public void ShowStatistics()
    {
        Console.WriteLine($"\n📊 Flyweight Factory Statistics:");
        Console.WriteLine($"   Unique characters (flyweights): {GetCreatedFlyweightsCount()}");
        Console.WriteLine($"   Total intrinsic memory usage: {GetTotalIntrinsicMemoryUsage()} bytes");
        Console.WriteLine($"   Characters created: {string.Join("", flyweights.Keys.OrderBy(c => c))}");
    }
}

// Context - contains extrinsic state and maintains references to flyweights
public class Character
{
    private readonly ICharacterFlyweight flyweight; // Reference to flyweight
    
    // Extrinsic state
    public int X { get; set; }
    public int Y { get; set; }
    public Color Color { get; set; }
    public int FontSize { get; set; }
    public string FontFamily { get; set; }
    
    public Character(char character, int x, int y, Color color, int fontSize = 12, string fontFamily = "Arial")
    {
        flyweight = CharacterFlyweightFactory.Instance.GetCharacter(character);
        X = x;
        Y = y;
        Color = color;
        FontSize = fontSize;
        FontFamily = fontFamily;
    }
    
    public char GetCharacter() => flyweight.GetCharacter();
    
    public void Render()
    {
        flyweight.Render(X, Y, Color, FontSize, FontFamily);
    }
    
    public void MoveTo(int x, int y)
    {
        X = x;
        Y = y;
    }
    
    public int GetExtrinsicMemoryUsage()
    {
        return sizeof(int) * 3 + // X, Y, FontSize
               Color.Name.Length * sizeof(char) + // Color (approximate)
               FontFamily.Length * sizeof(char) + // FontFamily
               IntPtr.Size; // Flyweight reference
    }
}

// Client - maintains collection of characters (text document)
public class TextDocument
{
    private readonly List<Character> characters;
    private readonly string title;
    
    public TextDocument(string title)
    {
        title = title;
        characters = new List<Character>();
    }
    
    public void AddCharacter(char character, int x, int y, Color color, int fontSize = 12, string fontFamily = "Arial")
    {
        characters.Add(new Character(character, x, y, color, fontSize, fontFamily));
    }
    
    public void AddText(string text, int startX, int startY, Color color, int fontSize = 12, string fontFamily = "Arial")
    {
        var charSpacing = fontSize / 2; // Approximate character spacing
        
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] != ' ') // Skip spaces for visual clarity
            {
                AddCharacter(text[i], startX + (i * charSpacing), startY, color, fontSize, fontFamily);
            }
        }
    }
    
    public void Render()
    {
        Console.WriteLine($"\n📄 Rendering document: '{title}'");
        Console.WriteLine(new string('=', 50));
        
        foreach (var character in characters)
        {
            character.Render();
        }
        
        ShowMemoryUsage();
    }
    
    public void ShowMemoryUsage()
    {
        var totalCharacters = characters.Count;
        var uniqueCharacters = CharacterFlyweightFactory.Instance.GetCreatedFlyweightsCount();
        var intrinsicMemory = CharacterFlyweightFactory.Instance.GetTotalIntrinsicMemoryUsage();
        var extrinsicMemory = characters.Sum(c => c.GetExtrinsicMemoryUsage());
        var totalMemory = intrinsicMemory + extrinsicMemory;
        
        // Calculate memory without flyweight pattern
        var memoryWithoutFlyweight = totalCharacters * (intrinsicMemory / Math.Max(uniqueCharacters, 1) + 
                                                        extrinsicMemory / Math.Max(totalCharacters, 1));
        
        Console.WriteLine($"\n💾 Memory Usage Analysis:");
        Console.WriteLine($"   Total characters: {totalCharacters}");
        Console.WriteLine($"   Unique characters (flyweights): {uniqueCharacters}");
        Console.WriteLine($"   Intrinsic memory (shared): {intrinsicMemory} bytes");
        Console.WriteLine($"   Extrinsic memory (context): {extrinsicMemory} bytes");
        Console.WriteLine($"   Total memory with flyweight: {totalMemory} bytes");
        Console.WriteLine($"   Estimated memory without flyweight: {memoryWithoutFlyweight} bytes");
        Console.WriteLine($"   Memory saved: {memoryWithoutFlyweight - totalMemory} bytes ({(1.0 - (double)totalMemory / memoryWithoutFlyweight) * 100:F1}%)");
    }
    
    public int GetTotalCharacters() => characters.Count;
    
    public void ChangeFormatting(int startIndex, int length, Color newColor, int newFontSize)
    {
        var endIndex = Math.Min(startIndex + length, characters.Count);
        
        for (int i = startIndex; i < endIndex; i++)
        {
            _characters[i].Color = newColor;
            _characters[i].FontSize = newFontSize;
        }
        
        Console.WriteLine($"🎨 Changed formatting for characters {startIndex}-{endIndex - 1}");
    }
}

// Game Example: Tree Forest using Flyweight
public interface ITreeFlyweight
{
    void Render(double x, double y, double scale, Color seasonColor);
    string GetTreeType();
    int GetModelComplexity();
}

public class TreeFlyweight : ITreeFlyweight
{
    private readonly string treeType; // Intrinsic state
    private readonly int polygonCount; // Intrinsic state - 3D model complexity
    private readonly byte[] textureData; // Intrinsic state - tree texture
    
    public TreeFlyweight(string treeType)
    {
        treeType = treeType;
        
        // Different tree types have different complexity
        polygonCount = treeType switch
        {
            "Oak" => 1500,
            "Pine" => 1200,
            "Birch" => 800,
            "Maple" => 1800,
            _ => 1000
        };
        
        // Simulate texture data
        textureData = new byte[polygonCount / 10]; // Simplified texture size
        
        Console.WriteLine($"🌳 Created {treeType} tree flyweight ({polygonCount} polygons)");
    }
    
    public string GetTreeType() => treeType;
    
    public int GetModelComplexity() => polygonCount;
    
    public void Render(double x, double y, double scale, Color seasonColor)
    {
        Console.WriteLine($"🎋 Rendering {treeType} at ({x:F1}, {y:F1}) " +
                         $"scale: {scale:F2} season: {seasonColor.Name}");
    }
}

public class TreeFlyweightFactory
{
    private readonly Dictionary<string, ITreeFlyweight> treeTypes;
    private static TreeFlyweightFactory instance;
    
    private TreeFlyweightFactory()
    {
        treeTypes = new Dictionary<string, ITreeFlyweight>();
    }
    
    public static TreeFlyweightFactory Instance => instance ??= new TreeFlyweightFactory();
    
    public ITreeFlyweight GetTreeType(string treeType)
    {
        if (!treeTypes.ContainsKey(treeType))
        {
            _treeTypes[treeType] = new TreeFlyweight(treeType);
        }
        
        return _treeTypes[treeType];
    }
    
    public void ShowStatistics()
    {
        var totalPolygons = treeTypes.Values.Sum(t => t.GetModelComplexity());
        Console.WriteLine($"\n🏞️ Forest Statistics:");
        Console.WriteLine($"   Tree types loaded: {treeTypes.Count}");
        Console.WriteLine($"   Total polygons in flyweights: {totalPolygons}");
        Console.WriteLine($"   Tree types: {string.Join(", ", treeTypes.Keys)}");
    }
}

public class Tree
{
    private readonly ITreeFlyweight flyweight;
    
    // Extrinsic state
    public double X { get; set; }
    public double Y { get; set; }
    public double Scale { get; set; }
    public Color SeasonColor { get; set; }
    
    public Tree(string treeType, double x, double y, double scale, Color seasonColor)
    {
        flyweight = TreeFlyweightFactory.Instance.GetTreeType(treeType);
        X = x;
        Y = y;
        Scale = scale;
        SeasonColor = seasonColor;
    }
    
    public void Render()
    {
        flyweight.Render(X, Y, Scale, SeasonColor);
    }
    
    public string GetTreeType() => flyweight.GetTreeType();
}

public class Forest
{
    private readonly List<Tree> trees;
    
    public Forest()
    {
        trees = new List<Tree>();
    }
    
    public void PlantTree(string treeType, double x, double y, double scale, Color seasonColor)
    {
        trees.Add(new Tree(treeType, x, y, scale, seasonColor));
    }
    
    public void GenerateRandomForest(int treeCount)
    {
        var random = new Random();
        var treeTypes = new[] { "Oak", "Pine", "Birch", "Maple" };
        var seasonColors = new[] { Color.Green, Color.Yellow, Color.Orange, Color.Brown };
        
        Console.WriteLine($"🌲 Generating forest with {treeCount} trees...");
        
        for (int i = 0; i < treeCount; i++)
        {
            var treeType = treeTypes[random.Next(treeTypes.Length)];
            var x = random.NextDouble() * 1000;
            var y = random.NextDouble() * 1000;
            var scale = 0.5 + random.NextDouble() * 1.5; // Scale between 0.5 and 2.0
            var seasonColor = seasonColors[random.Next(seasonColors.Length)];
            
            PlantTree(treeType, x, y, scale, seasonColor);
        }
        
        Console.WriteLine($"✅ Forest generated with {trees.Count} trees");
    }
    
    public void RenderForest()
    {
        Console.WriteLine($"\n🏞️ Rendering forest with {trees.Count} trees:");
        
        // Group by tree type for better visualization
        var treeGroups = trees.GroupBy(t => t.GetTreeType()).ToList();
        
        foreach (var group in treeGroups)
        {
            Console.WriteLine($"\n--- {group.Key} Trees ({group.Count()}) ---");
            
            foreach (var tree in group.Take(5)) // Show first 5 of each type
            {
                tree.Render();
            }
            
            if (group.Count() > 5)
            {
                Console.WriteLine($"   ... and {group.Count() - 5} more {group.Key} trees");
            }
        }
        
        ShowMemoryEfficiency();
    }
    
    private void ShowMemoryEfficiency()
    {
        var totalTrees = trees.Count;
        var uniqueTreeTypes = trees.Select(t => t.GetTreeType()).Distinct().Count();
        
        // Approximate memory calculation
        const int flyweightMemoryPerType = 2000; // Approximate bytes per tree type flyweight
        const int extrinsicMemoryPerTree = 50; // Approximate bytes for position, scale, color per tree
        
        var memoryWithFlyweight = (uniqueTreeTypes * flyweightMemoryPerType) + (totalTrees * extrinsicMemoryPerTree);
        var memoryWithoutFlyweight = totalTrees * flyweightMemoryPerType; // Each tree would have its own copy
        
        Console.WriteLine($"\n💾 Forest Memory Analysis:");
        Console.WriteLine($"   Total trees: {totalTrees}");
        Console.WriteLine($"   Unique tree types: {uniqueTreeTypes}");
        Console.WriteLine($"   Memory with flyweight: ~{memoryWithFlyweight} bytes");
        Console.WriteLine($"   Memory without flyweight: ~{memoryWithoutFlyweight} bytes");
        Console.WriteLine($"   Memory saved: ~{memoryWithoutFlyweight - memoryWithFlyweight} bytes");
        Console.WriteLine($"   Efficiency gain: {(1.0 - (double)memoryWithFlyweight / memoryWithoutFlyweight) * 100:F1}%");
    }
}

// Web Icon Example
public interface IIconFlyweight
{
    void Render(int x, int y, int size, Color tintColor);
    string GetIconName();
}

public class IconFlyweight : IIconFlyweight
{
    private readonly string iconName;
    private readonly byte[] svgData; // Vector graphics data
    
    public IconFlyweight(string iconName)
    {
        iconName = iconName;
        svgData = GenerateIconSvgData(iconName);
        Console.WriteLine($"🎨 Created icon flyweight: {iconName}");
    }
    
    public string GetIconName() => iconName;
    
    public void Render(int x, int y, int size, Color tintColor)
    {
        Console.WriteLine($"🖼️ Rendering {iconName} icon at ({x},{y}) size:{size}px tint:{tintColor.Name}");
    }
    
    private byte[] GenerateIconSvgData(string iconName)
    {
        // Simulate SVG data generation
        return Encoding.UTF8.GetBytes($"<svg>{iconName}_vector_data</svg>");
    }
}

public class IconFactory
{
    private readonly Dictionary<string, IIconFlyweight> icons = new Dictionary<string, IIconFlyweight>();
    
    public IIconFlyweight GetIcon(string iconName)
    {
        if (!icons.ContainsKey(iconName))
        {
            _icons[iconName] = new IconFlyweight(iconName);
        }
        
        return _icons[iconName];
    }
    
    public int GetLoadedIconsCount() => icons.Count;
}

public class WebPage
{
    private readonly IconFactory iconFactory;
    private readonly List<(IIconFlyweight icon, int x, int y, int size, Color tint)> iconInstances;
    
    public WebPage()
    {
        iconFactory = new IconFactory();
        iconInstances = new List<(IIconFlyweight, int, int, int, Color)>();
    }
    
    public void AddIcon(string iconName, int x, int y, int size = 24, Color? tint = null)
    {
        var icon = iconFactory.GetIcon(iconName);
        iconInstances.Add((icon, x, y, size, tint ?? Color.Black));
    }
    
    public void RenderPage()
    {
        Console.WriteLine($"\n🌐 Rendering web page with {iconInstances.Count} icons:");
        Console.WriteLine($"   Unique icon types loaded: {iconFactory.GetLoadedIconsCount()}");
        
        foreach (var (icon, x, y, size, tint) in iconInstances)
        {
            icon.Render(x, y, size, tint);
        }
    }
}
```

**Usage**:

```csharp
using System;
using System.Drawing;

class Program
{
    static void Main()
    {
        Console.WriteLine("=== Text Editor Flyweight Example ===");
        
        // Create a text document
        var document = new TextDocument("Sample Document");
        
        // Add text with different formatting
        document.AddText("Hello", 10, 10, Color.Black, 12, "Arial");
        document.AddText("World", 80, 10, Color.Red, 12, "Arial");
        document.AddText("Flyweight", 140, 10, Color.Blue, 14, "Times New Roman");
        document.AddText("Pattern", 250, 10, Color.Green, 16, "Helvetica");
        
        // Add more text to demonstrate sharing
        document.AddText("Hello", 10, 30, Color.Purple, 18, "Arial");
        document.AddText("Again", 90, 30, Color.Orange, 12, "Arial");
        
        // Render the document
        document.Render();
        
        // Show flyweight factory statistics
        CharacterFlyweightFactory.Instance.ShowStatistics();
        
        // Demonstrate formatting changes
        Console.WriteLine("\n=== Changing Formatting ===");
        document.ChangeFormatting(0, 5, Color.Magenta, 20);
        
        Console.WriteLine("\n=== Forest Flyweight Example ===");
        
        // Create a forest with many trees
        var forest = new Forest();
        
        // Generate a large forest
        forest.GenerateRandomForest(1000);
        
        // Render the forest (showing sample)
        forest.RenderForest();
        
        // Show tree flyweight statistics
        TreeFlyweightFactory.Instance.ShowStatistics();
        
        Console.WriteLine("\n=== Web Icons Flyweight Example ===");
        
        // Create a web page with many icons
        var webPage = new WebPage();
        
        // Add various icons (many instances of same icon types)
        webPage.AddIcon("home", 10, 10, 24, Color.Blue);
        webPage.AddIcon("user", 50, 10, 20, Color.Gray);
        webPage.AddIcon("settings", 90, 10, 22, Color.DarkGray);
        webPage.AddIcon("home", 10, 50, 32, Color.Green); // Same flyweight as first home
        webPage.AddIcon("user", 60, 50, 28, Color.Purple); // Same flyweight as first user
        webPage.AddIcon("search", 110, 50, 24, Color.Orange);
        webPage.AddIcon("home", 10, 90, 16, Color.Red); // Third instance of home flyweight
        webPage.AddIcon("notification", 50, 90, 18, Color.Yellow);
        
        // Render the web page
        webPage.RenderPage();
        
        Console.WriteLine("\n=== Flyweight vs Non-Flyweight Comparison ===");
        
        // Demonstrate memory efficiency with large dataset
        var largeDocument = new TextDocument("Large Document Test");
        var text = "The quick brown fox jumps over the lazy dog. ";
        
        // Add the same text multiple times
        for (int i = 0; i < 20; i++)
        {
            largeDocument.AddText(text, 10, 10 + (i * 15), Color.Black, 12, "Arial");
        }
        
        Console.WriteLine($"\n📊 Large Document Analysis:");
        Console.WriteLine($"Text repeated 20 times: \"{text}\"");
        Console.WriteLine($"Total characters: {largeDocument.GetTotalCharacters()}");
        
        largeDocument.ShowMemoryUsage();
        CharacterFlyweightFactory.Instance.ShowStatistics();
        
        Console.WriteLine("\n=== Flyweight Pattern Benefits ===");
        Console.WriteLine("✅ Significant memory reduction for large numbers of similar objects");
        Console.WriteLine("✅ Intrinsic state is shared among all instances");
        Console.WriteLine("✅ Extrinsic state can vary per object instance");
        Console.WriteLine("✅ Reduces object creation overhead");
        Console.WriteLine("✅ Improves performance in memory-constrained scenarios");
        
        Console.WriteLine("\n=== Real-world Applications ===");
        Console.WriteLine("• Text editors (character formatting)");
        Console.WriteLine("• Game engines (sprites, particles, terrain)");
        Console.WriteLine("• Web browsers (icons, fonts, UI elements)");
        Console.WriteLine("• Document processors (styles, formatting)");
        Console.WriteLine("• Graphics applications (brushes, patterns)");
        Console.WriteLine("• CAD systems (standard components)");
        
        Console.WriteLine("\n=== When to Use Flyweight ===");
        Console.WriteLine("• Application uses large numbers of objects");
        Console.WriteLine("• Storage costs are high due to object quantity");
        Console.WriteLine("• Most object state can be made extrinsic");
        Console.WriteLine("• Groups of objects can be replaced by few shared objects");
        Console.WriteLine("• Application doesn't depend on object identity");
        
        Console.WriteLine("\n=== Flyweight Considerations ===");
        Console.WriteLine("⚠️ May introduce runtime costs due to extrinsic state transfer");
        Console.WriteLine("⚠️ Complexity increases due to state separation");
        Console.WriteLine("⚠️ Client must manage extrinsic state");
        Console.WriteLine("⚠️ Thread safety considerations for shared flyweights");
        Console.WriteLine("⚠️ Factory pattern adds additional complexity");
    }
}
```

**Notes**:

- Minimizes memory usage by sharing efficiently among similar objects
- Separates intrinsic state (shared) from extrinsic state (context-specific)
- Factory pattern is typically used to manage and cache flyweight instances
- Most effective when dealing with large numbers of similar objects
- Extrinsic state must be passed to flyweight methods, increasing method call overhead
- Flyweights should be immutable to be safely shared
- Thread safety must be considered for shared flyweight objects
- Can make application logic more complex due to state separation
- Related patterns: [Singleton](singleton.md), [Factory Method](factory-method.md), [Composite](composite.md)

**Prerequisites**:

- .NET Framework 2.0+ or .NET Core
- Understanding of object-oriented principles
- Knowledge of memory optimization concepts
- Familiarity with Factory pattern

**References**:

- Gang of Four Design Patterns book
- [Microsoft Docs: Flyweight Pattern](https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/flyweight)

---

*Created: 2025-10-31*  
*Last Updated: 2025-10-31*  
*Tags: #flyweight #structural #memory #sharing*
