namespace Snippets.DesignPatterns.Behavioral.Memento;

/// <summary>
/// Game state that can create and restore from mementos
/// </summary>
public class GameState : IOriginator<GameStateMemento>
{
    private List<Item> inventory = [];
    private Dictionary<string, object> gameData = new();

    public int Level { get; private set; } = 1;

    public int Score { get; private set; } = 0;

    public int Lives { get; private set; } = 3;

    public PlayerPosition Position { get; private set; } = new(0, 0, "StartArea");

    public IReadOnlyList<Item> Inventory => inventory.AsReadOnly();

    public void AddScore(int points)
    {
        Score += points;
        Console.WriteLine($"Score increased by {points}. Total: {Score}");
    }

    public void LoseLife()
    {
        if (Lives > 0)
        {
            Lives--;
            Console.WriteLine($"Life lost! Remaining lives: {Lives}");
        }
    }

    public void GainLife()
    {
        Lives++;
        Console.WriteLine($"Extra life gained! Lives: {Lives}");
    }

    public void LevelUp()
    {
        Level++;
        Console.WriteLine($"Level up! Now at level {Level}");
    }

    public void MovePlayer(int x, int y, string area)
    {
        Position = new PlayerPosition(x, y, area);
        Console.WriteLine($"Player moved to {area} ({x}, {y})");
    }

    public void AddItem(Item item)
    {
        var existingItem = inventory.FirstOrDefault(i => i.Name == item.Name);
        if (existingItem != null)
        {
            var newQuantity = existingItem.Quantity + item.Quantity;
            inventory.Remove(existingItem);
            inventory.Add(existingItem with { Quantity = newQuantity });
        }
        else
        {
            inventory.Add(item);
        }

        Console.WriteLine($"Added {item.Quantity}x {item.Name} to inventory");
    }

    public bool UseItem(string itemName, int quantity = 1)
    {
        var item = inventory.FirstOrDefault(i => i.Name == itemName);
        if (item != null && item.Quantity >= quantity)
        {
            var newQuantity = item.Quantity - quantity;
            inventory.Remove(item);

            if (newQuantity > 0)
            {
                inventory.Add(item with { Quantity = newQuantity });
            }

            Console.WriteLine($"Used {quantity}x {itemName}");
            return true;
        }

        Console.WriteLine($"Not enough {itemName} in inventory");
        return false;
    }

    public void SetGameData(string key, object value)
    {
        gameData[key] = value;
        Console.WriteLine($"Game data set: {key} = {value}");
    }

    public GameStateMemento CreateMemento()
    {
        var description = $"Level {Level} - Score: {Score} - Lives: {Lives}";
        return new GameStateMemento(
            Level,
            Score,
            Lives,
            Position,
            inventory,
            gameData,
            description);
    }

    public void RestoreFromMemento(GameStateMemento memento)
    {
        Level = memento.Level;
        Score = memento.Score;
        Lives = memento.Lives;
        Position = memento.Position;
        inventory = new List<Item>(memento.Inventory);
        gameData = new Dictionary<string, object>(memento.GameData);

        Console.WriteLine($"Game state restored: {memento.Description}");
    }

    public void PrintStatus()
    {
        Console.WriteLine($"Level: {Level}, Score: {Score}, Lives: {Lives}");
        Console.WriteLine($"Position: {Position.Area} ({Position.X}, {Position.Y})");
        Console.WriteLine($"Inventory: {inventory.Count} items");
        foreach (var item in inventory)
        {
            Console.WriteLine($"  - {item.Name} x{item.Quantity}");
        }
    }
}