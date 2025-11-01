namespace Snippets.DesignPatterns.Behavioral.Interpreter;

public class ExpressionInfo
{
    public string Type { get; set; } = "";
    public string Value { get; set; } = "";
    public HashSet<string> Variables { get; set; } = [];
    public HashSet<string> Functions { get; set; } = [];
    public int Depth { get; set; }
}