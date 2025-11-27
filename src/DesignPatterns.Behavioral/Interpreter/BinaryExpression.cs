namespace Snippets.DesignPatterns.Behavioral.Interpreter;

public abstract class BinaryExpression(Expression left, Expression right) : Expression
{
    
    public Expression Left { get; } = left ?? throw new ArgumentNullException(nameof(left));
    public Expression Right { get; } = right ?? throw new ArgumentNullException(nameof(right));

    protected abstract string OperatorSymbol { get; }

    public override string ToString()
    {
        return $"({Left} {OperatorSymbol} {Right})";
    }
}