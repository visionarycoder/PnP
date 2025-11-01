namespace Snippets.DesignPatterns.Behavioral.Memento;

/// <summary>
/// Game state memento storing complete game state
/// </summary>
public class GameStateMemento(
    int level,
    int score,
    int lives,
    PlayerPosition position,
    List<Item> inventory,
    Dictionary<string, object> gameData,
    string description)
    : IMemento
{
    public int Level { get; } = level;
    public int Score { get; } = score;
    public int Lives { get; } = lives;
    public PlayerPosition Position { get; } = position;
    public List<Item> Inventory { get; } = [..inventory]; // Deep copy
    public Dictionary<string, object> GameData { get; } = new(gameData); // Deep copy
    public DateTime CreatedAt { get; } = DateTime.UtcNow;
    public string Description { get; } = description;
}