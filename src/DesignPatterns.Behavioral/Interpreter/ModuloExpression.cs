namespace Snippets.DesignPatterns.Behavioral.Interpreter;

public class ModuloExpression(Expression left, Expression right) 
    : BinaryExpression(left, right)
{
    protected override string OperatorSymbol => "%";

    public override double Interpret(ExpressionContext context)
    {
        return Left.Interpret(context) % Right.Interpret(context);
    }

    public override TResult Accept<TResult>(IExpressionVisitor<TResult> visitor)
    {
        return visitor.Visit(this);
    }
}