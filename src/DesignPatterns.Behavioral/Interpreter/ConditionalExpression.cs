namespace Snippets.DesignPatterns.Behavioral.Interpreter;

public class ConditionalExpression(Expression condition, Expression trueExpression, Expression falseExpression)
    : Expression
{
    public Expression Condition { get; } = condition ?? throw new ArgumentNullException(nameof(condition));

    public Expression TrueExpression { get; } = trueExpression ?? throw new ArgumentNullException(nameof(trueExpression));

    public Expression FalseExpression { get; } = falseExpression ?? throw new ArgumentNullException(nameof(falseExpression));

    public override double Interpret(ExpressionContext context)
    {
        var conditionValue = Condition.Interpret(context);
        return Math.Abs(conditionValue) > double.Epsilon
            ? TrueExpression.Interpret(context)
            : FalseExpression.Interpret(context);
    }

    public override string ToString()
    {
        return $"({Condition} ? {TrueExpression} : {FalseExpression})";
    }

    public override TResult Accept<TResult>(IExpressionVisitor<TResult> visitor)
    {
        return visitor.Visit(this);
    }
}