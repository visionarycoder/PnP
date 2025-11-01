using System.Drawing;

namespace Snippets.DesignPatterns.Structural.Flyweight;

public interface ISpriteFlyweight
{
    void Render(int x, int y, float scale, float rotation, Color tint);
    string GetSpriteName();
    int GetMemoryUsage();
}