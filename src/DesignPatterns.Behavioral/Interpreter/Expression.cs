namespace Snippets.DesignPatterns.Behavioral.Interpreter;

public abstract class Expression
{
    public abstract double Interpret(ExpressionContext context);
    public override abstract string ToString();

    // Visitor pattern support for expression analysis
    public abstract TResult Accept<TResult>(IExpressionVisitor<TResult> visitor);
}