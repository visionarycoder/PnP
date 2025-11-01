namespace Snippets.DesignPatterns.Behavioral.Interpreter;

public class VariableExpression(string name) : Expression
{
    public string Name { get; } = name ?? throw new ArgumentNullException(nameof(name));

    public override double Interpret(ExpressionContext context)
    {
        return context.GetVariable(Name);
    }

    public override string ToString()
    {
        return Name;
    }

    public override TResult Accept<TResult>(IExpressionVisitor<TResult> visitor)
    {
        return visitor.Visit(this);
    }
}