namespace Snippets.DesignPatterns.Behavioral.TemplateMethod;

public class GameContext
{
    public int PlayerHealth { get; set; } = 100;
    public int EnemyHealth { get; set; } = 100;
    public int EnemyDistance { get; set; } = 5;
    public List<string> Inventory { get; set; } = [];
    public Dictionary<string, object> GameData { get; set; } = new();
}