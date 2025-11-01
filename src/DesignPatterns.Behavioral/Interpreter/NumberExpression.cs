namespace Snippets.DesignPatterns.Behavioral.Interpreter;

public class NumberExpression(double value) : Expression
{
    public double Value => value;

    public override double Interpret(ExpressionContext context)
    {
        return value;
    }

    public override string ToString()
    {
        return value.ToString("G");
    }

    public override TResult Accept<TResult>(IExpressionVisitor<TResult> visitor)
    {
        return visitor.Visit(this);
    }
}