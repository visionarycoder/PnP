namespace Snippets.DesignPatterns.Behavioral.Interpreter;

public abstract class UnaryExpression(Expression operand) : Expression
{
    
    public Expression Operand { get; } = operand ?? throw new ArgumentNullException(nameof(operand));

    protected abstract string OperatorSymbol { get; }

    public override string ToString()
    {
        return $"{OperatorSymbol}({Operand})";
    }

}