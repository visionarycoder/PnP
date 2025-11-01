using System.Drawing;

namespace Snippets.DesignPatterns.Structural.Flyweight;

public interface ICharacterFlyweight
{
    void Render(int x, int y, Color color, int fontSize, string fontFamily);
    char GetCharacter();
    int GetIntrinsicMemoryUsage();
}