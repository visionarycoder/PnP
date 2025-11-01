using System.Text;

namespace Snippets.DesignPatterns.Behavioral.Visitor;

/// <summary>
/// Analysis result for AST structures
/// </summary>
public class AnalysisResult
{
    public int LiteralCount { get; set; }
    public int VariableCount { get; set; }
    public int OperationCount { get; set; }
    public int FunctionCallCount { get; set; }
    public int AssignmentCount { get; set; }
    public int BlockCount { get; set; }

    public HashSet<string> Variables { get; set; } = [];
    public List<string> Operators { get; set; } = [];
    public HashSet<string> Functions { get; set; } = [];
    public Dictionary<string, int> NodesByType { get; set; } = new();

    public int TotalNodes => NodesByType.Values.Sum();
    public int MaxDepth { get; set; }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine("AST Analysis Results:");
        sb.AppendLine($"Total Nodes: {TotalNodes}");
        sb.AppendLine($"Max Depth: {MaxDepth}");
        sb.AppendLine($"Literals: {LiteralCount}");
        sb.AppendLine($"Variables: {VariableCount} (unique: {Variables.Count})");
        sb.AppendLine($"Operations: {OperationCount}");
        sb.AppendLine($"Function Calls: {FunctionCallCount} (unique: {Functions.Count})");
        sb.AppendLine($"Assignments: {AssignmentCount}");
        sb.AppendLine($"Blocks: {BlockCount}");

        if (Variables.Any())
        {
            sb.AppendLine($"Variable Names: [{string.Join(", ", Variables)}]");
        }

        if (Functions.Any())
        {
            sb.AppendLine($"Function Names: [{string.Join(", ", Functions)}]");
        }

        return sb.ToString();
    }
}