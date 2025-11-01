namespace Snippets.DesignPatterns.Structural.Flyweight;

public class SpriteFactory
{
    private readonly Dictionary<string, ISpriteFlyweight> sprites = new();

    public ISpriteFlyweight GetSprite(string spriteName, int width = 32, int height = 32)
    {
        var key = $"{spriteName}_{width}x{height}";

        if (!sprites.ContainsKey(key))
        {
            sprites[key] = new SpriteFlyweight(spriteName, width, height);
        }

        return sprites[key];
    }

    public int GetLoadedSpritesCount() => sprites.Count;

    public int GetTotalMemoryUsage() => sprites.Values.Sum(s => s.GetMemoryUsage());
}