namespace Snippets.DesignPatterns.Behavioral.TemplateMethod;

public class GameAction
{
    public string Name { get; set; } = "";
    public ActionType Type { get; set; }
    public int Priority { get; set; }
    public double Score { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
}