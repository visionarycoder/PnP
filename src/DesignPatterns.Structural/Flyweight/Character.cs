using System.Drawing;

namespace Snippets.DesignPatterns.Structural.Flyweight;

public class Character(char character, int x, int y, Color color, int fontSize = 12, string fontFamily = "Arial")
{
    private readonly ICharacterFlyweight flyweight = CharacterFlyweightFactory.Instance.GetCharacter(character); // Reference to flyweight

    // Extrinsic state
    public int X { get; set; } = x;
    public int Y { get; set; } = y;
    public Color Color { get; set; } = color;
    public int FontSize { get; set; } = fontSize;
    public string FontFamily { get; set; } = fontFamily;

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
}