namespace Snippets.DesignPatterns.Behavioral.Interpreter;

public class DivideExpression(Expression left, Expression right) 
    : BinaryExpression(left, right)
{
    protected override string OperatorSymbol => "/";

    public override double Interpret(ExpressionContext context)
    {
        var rightValue = Right.Interpret(context);
        if (Math.Abs(rightValue) < double.Epsilon)
        {
            throw new DivideByZeroException("Division by zero");
        }

        return Left.Interpret(context) / rightValue;
    }

    public override TResult Accept<TResult>(IExpressionVisitor<TResult> visitor)
    {
        return visitor.Visit(this);
    }
}