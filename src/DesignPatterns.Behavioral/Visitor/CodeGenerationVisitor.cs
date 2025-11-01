using System.Text;

namespace Snippets.DesignPatterns.Behavioral.Visitor;

/// <summary>
/// Visitor for generating code from AST
/// </summary>
public class CodeGenerationVisitor : IAstVisitor<string>
{
    private int indentLevel = 0;
    private readonly string indentString = "  ";

    public string Visit(LiteralNode node)
    {
        return node.Value switch
        {
            string s => $"\"{s.Replace("\"", "\\\"")}\"",
            bool b => b.ToString().ToLower(),
            _ => node.Value.ToString()!
        };
    }

    public string Visit(VariableNode node)
    {
        return node.Name;
    }

    public string Visit(BinaryOperationNode node)
    {
        var left = node.Left.Accept(this);
        var right = node.Right.Accept(this);
        return $"({left} {node.Operator} {right})";
    }

    public string Visit(UnaryOperationNode node)
    {
        var operand = node.Operand.Accept(this);
        return $"{node.Operator}{operand}";
    }

    public string Visit(FunctionCallNode node)
    {
        var args = string.Join(", ", node.Arguments.Select(arg => arg.Accept(this)));
        return $"{node.FunctionName}({args})";
    }

    public string Visit(AssignmentNode node)
    {
        var value = node.Value.Accept(this);
        return $"{GetIndent()}{node.VariableName} = {value};";
    }

    public string Visit(BlockNode node)
    {
        var code = new StringBuilder();
        code.AppendLine($"{GetIndent()}{{");

        indentLevel++;
        foreach (var statement in node.Statements)
        {
            code.AppendLine(statement.Accept(this));
        }

        indentLevel--;

        code.Append($"{GetIndent()}}}");
        return code.ToString();
    }

    public string Visit(ElementA element) => throw new NotSupportedException();
    public string Visit(ElementB element) => throw new NotSupportedException();
    public string Visit(ElementC element) => throw new NotSupportedException();

    private string GetIndent()
    {
        return new string(' ', indentLevel * indentString.Length);
    }
}