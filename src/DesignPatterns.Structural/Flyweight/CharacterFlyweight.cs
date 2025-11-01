using System.Drawing;

namespace Snippets.DesignPatterns.Structural.Flyweight;

// Flyweight interface - declares methods through which flyweights can receive and act on extrinsic state

// Concrete Flyweight - implements Flyweight interface and stores intrinsic state
public class CharacterFlyweight : ICharacterFlyweight
{
    private readonly char character; // Intrinsic state - shared among all instances
    private readonly byte[] bitmap; // Intrinsic state - character shape data
    private readonly int baseWidth;
    private readonly int baseHeight;

    public CharacterFlyweight(char character)
    {
        this.character = character;

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
        // Intrinsic state: _character, _bitmap, _baseWidth, _baseHeight

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

// Context - contains extrinsic state and maintains references to flyweights

// Text Editor that uses flyweight pattern

// Advanced Flyweight Example: Game Sprites