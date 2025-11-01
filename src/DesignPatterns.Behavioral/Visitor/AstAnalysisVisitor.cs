namespace Snippets.DesignPatterns.Behavioral.Visitor;

/// <summary>
/// Visitor for analyzing AST structures
/// </summary>
public class AstAnalysisVisitor : IAstVisitor<AnalysisResult>
{
    private readonly AnalysisResult result = new();
    private int currentDepth = 0;

    public AnalysisResult GetResult() => result;

    public AnalysisResult Visit(LiteralNode node)
    {
        result.LiteralCount++;
        result.NodesByType.TryAdd("Literal", 0);
        result.NodesByType["Literal"]++;
        UpdateMaxDepth();
        return result;
    }

    public AnalysisResult Visit(VariableNode node)
    {
        result.VariableCount++;
        result.Variables.Add(node.Name);
        result.NodesByType.TryAdd("Variable", 0);
        result.NodesByType["Variable"]++;
        UpdateMaxDepth();
        return result;
    }

    public AnalysisResult Visit(BinaryOperationNode node)
    {
        result.OperationCount++;
        result.Operators.Add(node.Operator);
        result.NodesByType.TryAdd("BinaryOperation", 0);
        result.NodesByType["BinaryOperation"]++;
        UpdateMaxDepth();

        currentDepth++;
        node.Left.Accept(this);
        node.Right.Accept(this);
        currentDepth--;

        return result;
    }

    public AnalysisResult Visit(UnaryOperationNode node)
    {
        result.OperationCount++;
        result.Operators.Add(node.Operator);
        result.NodesByType.TryAdd("UnaryOperation", 0);
        result.NodesByType["UnaryOperation"]++;
        UpdateMaxDepth();

        currentDepth++;
        node.Operand.Accept(this);
        currentDepth--;

        return result;
    }

    public AnalysisResult Visit(FunctionCallNode node)
    {
        result.FunctionCallCount++;
        result.Functions.Add(node.FunctionName);
        result.NodesByType.TryAdd("FunctionCall", 0);
        result.NodesByType["FunctionCall"]++;
        UpdateMaxDepth();

        currentDepth++;
        foreach (var arg in node.Arguments)
        {
            arg.Accept(this);
        }

        currentDepth--;

        return result;
    }

    public AnalysisResult Visit(AssignmentNode node)
    {
        result.AssignmentCount++;
        result.Variables.Add(node.VariableName);
        result.NodesByType.TryAdd("Assignment", 0);
        result.NodesByType["Assignment"]++;
        UpdateMaxDepth();

        currentDepth++;
        node.Value.Accept(this);
        currentDepth--;

        return result;
    }

    public AnalysisResult Visit(BlockNode node)
    {
        result.BlockCount++;
        result.NodesByType.TryAdd("Block", 0);
        result.NodesByType["Block"]++;
        UpdateMaxDepth();

        currentDepth++;
        foreach (var statement in node.Statements)
        {
            statement.Accept(this);
        }

        currentDepth--;

        return result;
    }

    public AnalysisResult Visit(ElementA element) => throw new NotSupportedException();
    public AnalysisResult Visit(ElementB element) => throw new NotSupportedException();
    public AnalysisResult Visit(ElementC element) => throw new NotSupportedException();

    private void UpdateMaxDepth()
    {
        if (currentDepth > result.MaxDepth)
        {
            result.MaxDepth = currentDepth;
        }
    }
}