namespace Snippets.DesignPatterns.Behavioral.Interpreter;

public class NegateExpression(Expression operand) 
    : UnaryExpression(operand)
{

    protected override string OperatorSymbol => "-";

    public override double Interpret(ExpressionContext context)
    {
        return -Operand.Interpret(context);
    }

    public override TResult Accept<TResult>(IExpressionVisitor<TResult> visitor)
    {
        return visitor.Visit(this);
    }
}