using System.Drawing;

namespace Snippets.DesignPatterns.Structural.Flyweight;

public class SpriteFlyweight : ISpriteFlyweight
{
    private readonly string spriteName;
    private readonly byte[] imageData;
    private readonly int width;
    private readonly int height;

    public SpriteFlyweight(string spriteName, int width, int height)
    {
        this.spriteName = spriteName;
        this.width = width;
        this.height = height;
        imageData = new byte[width * height * 4]; // RGBA

        // Simulate loading image data
        new Random(spriteName.GetHashCode()).NextBytes(imageData);
        Console.WriteLine($"🖼️ Loaded sprite: {spriteName} ({width}x{height})");
    }

    public string GetSpriteName() => spriteName;

    public int GetMemoryUsage() => imageData.Length + spriteName.Length * sizeof(char) + sizeof(int) * 2;

    public void Render(int x, int y, float scale, float rotation, Color tint)
    {
        var scaledWidth = (int)(width * scale);
        var scaledHeight = (int)(height * scale);

        Console.WriteLine($"🎮 Rendering {spriteName} at ({x},{y}) " +
                          $"size: {scaledWidth}x{scaledHeight}, rotation: {rotation:F1}°, tint: {tint.Name}");
    }
}