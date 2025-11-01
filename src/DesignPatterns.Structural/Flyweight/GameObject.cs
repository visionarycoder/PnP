using System.Drawing;

namespace Snippets.DesignPatterns.Structural.Flyweight;

public class GameObject(
    ISpriteFlyweight sprite,
    int x,
    int y,
    float scale = 1.0f,
    float rotation = 0f,
    Color? tint = null)
{
    // Extrinsic state
    public int X { get; set; } = x;
    public int Y { get; set; } = y;
    public float Scale { get; set; } = scale;
    public float Rotation { get; set; } = rotation;
    public Color Tint { get; set; } = tint ?? Color.White;

    public void Render()
    {
        sprite.Render(X, Y, Scale, Rotation, Tint);
    }

    public string GetSpriteName() => sprite.GetSpriteName();
}