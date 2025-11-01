namespace Snippets.DesignPatterns.Behavioral.Interpreter;

public class ExpressionAnalyzer : IExpressionVisitor<ExpressionInfo>
{
    public ExpressionInfo Visit(NumberExpression expression)
    {
        return new ExpressionInfo
        {
            Type = "Number",
            Value = expression.Value.ToString("G"),
            Variables = [],
            Functions = [],
            Depth = 1
        };
    }

    public ExpressionInfo Visit(VariableExpression expression)
    {
        return new ExpressionInfo
        {
            Type = "Variable",
            Value = expression.Name,
            Variables = [expression.Name],
            Functions = [],
            Depth = 1
        };
    }

    public ExpressionInfo Visit(AddExpression expression)
    {
        return VisitBinaryExpression("Add", expression);
    }

    public ExpressionInfo Visit(SubtractExpression expression)
    {
        return VisitBinaryExpression("Subtract", expression);
    }

    public ExpressionInfo Visit(MultiplyExpression expression)
    {
        return VisitBinaryExpression("Multiply", expression);
    }

    public ExpressionInfo Visit(DivideExpression expression)
    {
        return VisitBinaryExpression("Divide", expression);
    }

    public ExpressionInfo Visit(PowerExpression expression)
    {
        return VisitBinaryExpression("Power", expression);
    }

    public ExpressionInfo Visit(ModuloExpression expression)
    {
        return VisitBinaryExpression("Modulo", expression);
    }

    public ExpressionInfo Visit(NegateExpression expression)
    {
        var operandInfo = expression.Operand.Accept(this);
        return new ExpressionInfo
        {
            Type = "Negate",
            Value = $"-{operandInfo.Value}",
            Variables = operandInfo.Variables,
            Functions = operandInfo.Functions,
            Depth = operandInfo.Depth + 1
        };
    }

    public ExpressionInfo Visit(FunctionExpression expression)
    {
        var functions = new HashSet<string> { expression.FunctionName };
        var variables = new HashSet<string>();
        int maxDepth = 0;

        foreach (var arg in expression.Arguments)
        {
            var argInfo = arg.Accept(this);
            functions.UnionWith(argInfo.Functions);
            variables.UnionWith(argInfo.Variables);
            maxDepth = Math.Max(maxDepth, argInfo.Depth);
        }

        return new ExpressionInfo
        {
            Type = "Function",
            Value = $"{expression.FunctionName}({expression.Arguments.Count} args)",
            Variables = variables,
            Functions = functions,
            Depth = maxDepth + 1
        };
    }

    public ExpressionInfo Visit(ConditionalExpression expression)
    {
        var conditionInfo = expression.Condition.Accept(this);
        var trueInfo = expression.TrueExpression.Accept(this);
        var falseInfo = expression.FalseExpression.Accept(this);

        var variables = new HashSet<string>();
        variables.UnionWith(conditionInfo.Variables);
        variables.UnionWith(trueInfo.Variables);
        variables.UnionWith(falseInfo.Variables);

        var functions = new HashSet<string>();
        functions.UnionWith(conditionInfo.Functions);
        functions.UnionWith(trueInfo.Functions);
        functions.UnionWith(falseInfo.Functions);

        return new ExpressionInfo
        {
            Type = "Conditional",
            Value = "? :",
            Variables = variables,
            Functions = functions,
            Depth = Math.Max(Math.Max(conditionInfo.Depth, trueInfo.Depth), falseInfo.Depth) + 1
        };
    }

    private ExpressionInfo VisitBinaryExpression(string type, BinaryExpression expression)
    {
        var leftInfo = expression.Left.Accept(this);
        var rightInfo = expression.Right.Accept(this);

        var variables = new HashSet<string>();
        variables.UnionWith(leftInfo.Variables);
        variables.UnionWith(rightInfo.Variables);

        var functions = new HashSet<string>();
        functions.UnionWith(leftInfo.Functions);
        functions.UnionWith(rightInfo.Functions);

        return new ExpressionInfo
        {
            Type = type,
            Value = $"{leftInfo.Value} {type.ToLower()} {rightInfo.Value}",
            Variables = variables,
            Functions = functions,
            Depth = Math.Max(leftInfo.Depth, rightInfo.Depth) + 1
        };
    }
}