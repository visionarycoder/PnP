namespace Snippets.DesignPatterns.Behavioral.Interpreter;

public interface IExpressionVisitor<TResult>
{
    TResult Visit(NumberExpression expression);
    TResult Visit(VariableExpression expression);
    TResult Visit(AddExpression expression);
    TResult Visit(SubtractExpression expression);
    TResult Visit(MultiplyExpression expression);
    TResult Visit(DivideExpression expression);
    TResult Visit(PowerExpression expression);
    TResult Visit(ModuloExpression expression);
    TResult Visit(NegateExpression expression);
    TResult Visit(FunctionExpression expression);
    TResult Visit(ConditionalExpression expression);
}