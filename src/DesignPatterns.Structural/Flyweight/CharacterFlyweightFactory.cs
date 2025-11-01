namespace Snippets.DesignPatterns.Structural.Flyweight;

public class CharacterFlyweightFactory
{
    private readonly Dictionary<char, ICharacterFlyweight> flyweights;
    private static CharacterFlyweightFactory? _instance;
    private static readonly object Lock = new object();

    private CharacterFlyweightFactory()
    {
        flyweights = new Dictionary<char, ICharacterFlyweight>();
    }

    public static CharacterFlyweightFactory Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (Lock)
                {
                    _instance ??= new CharacterFlyweightFactory();
                }
            }

            return _instance;
        }
    }

    public ICharacterFlyweight GetCharacter(char character)
    {
        if (!flyweights.ContainsKey(character))
        {
            flyweights[character] = new CharacterFlyweight(character);
        }

        return flyweights[character];
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