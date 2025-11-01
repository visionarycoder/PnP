namespace Snippets.DesignPatterns.Behavioral.Interpreter;

public class FunctionExpression(string functionName, params Expression[] arguments) : Expression
{
    private readonly List<Expression> arguments = [..arguments ?? []];

    public string FunctionName { get; } = functionName ?? throw new ArgumentNullException(nameof(functionName));

    public IReadOnlyList<Expression> Arguments => arguments;

    public override double Interpret(ExpressionContext context)
    {
        var args = arguments.Select(arg => arg.Interpret(context)).ToArray();
        return context.CallFunction(FunctionName, args);
    }

    public override string ToString()
    {
        var args = string.Join(", ", arguments.Select(arg => arg.ToString()));
        return $"{FunctionName}({args})";
    }

    public override TResult Accept<TResult>(IExpressionVisitor<TResult> visitor)
    {
        return visitor.Visit(this);
    }
}