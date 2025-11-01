using System.Drawing;

namespace Snippets.DesignPatterns.Structural.Flyweight;

public class TextDocument
{
    private readonly List<Character> characters = [];
    private int currentX = 0;
    private int currentY = 0;
    private Color currentColor = Color.Black;
    private int currentFontSize = 12;
    private string currentFontFamily = "Arial";

    public void SetFormatting(Color color, int fontSize, string fontFamily)
    {
        currentColor = color;
        currentFontSize = fontSize;
        currentFontFamily = fontFamily;
    }

    public void AddText(string text)
    {
        foreach (char c in text)
        {
            if (c == '\n')
            {
                currentX = 0;
                currentY += currentFontSize + 2;
            }
            else if (c == ' ')
            {
                currentX += currentFontSize / 2;
            }
            else
            {
                var character = new Character(c, currentX, currentY, currentColor, currentFontSize,
                    currentFontFamily);
                characters.Add(character);
                currentX += currentFontSize;
            }
        }
    }

    public void Render()
    {
        Console.WriteLine($"\n📄 Rendering document with {characters.Count} characters:");
        foreach (var character in characters)
        {
            character.Render();
        }
    }

    public void ShowDocumentStatistics()
    {
        Console.WriteLine($"\n📊 Document Statistics:");
        Console.WriteLine($"   Total characters in document: {characters.Count}");

        var uniqueChars = characters.Select(c => c.GetCharacter()).Distinct().Count();
        Console.WriteLine($"   Unique characters: {uniqueChars}");

        // Show memory savings
        var flyweightCount = CharacterFlyweightFactory.Instance.GetCreatedFlyweightsCount();
        var memoryWithoutFlyweight = characters.Count * 100; // Assume 100 bytes per character without flyweight
        var memoryWithFlyweight = CharacterFlyweightFactory.Instance.GetTotalIntrinsicMemoryUsage() +
                                  (characters.Count * 32); // 32 bytes for extrinsic state per character

        Console.WriteLine($"   Memory without flyweight: ~{memoryWithoutFlyweight} bytes");
        Console.WriteLine($"   Memory with flyweight: ~{memoryWithFlyweight} bytes");
        Console.WriteLine($"   Memory savings: ~{memoryWithoutFlyweight - memoryWithFlyweight} bytes " +
                          $"({(double)(memoryWithoutFlyweight - memoryWithFlyweight) / memoryWithoutFlyweight * 100:F1}%)");

        CharacterFlyweightFactory.Instance.ShowStatistics();
    }
}