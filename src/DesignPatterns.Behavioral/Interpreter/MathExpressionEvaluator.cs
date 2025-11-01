namespace Snippets.DesignPatterns.Behavioral.Interpreter;

public class MathExpressionEvaluator(ExpressionContext? context = null)
{
    public ExpressionContext Context { get; } = context ?? new ExpressionContext();

    public double Evaluate(string expression)
    {
        var lexer = new ExpressionLexer(expression);
        var tokens = lexer.Tokenize();
        var parser = new ExpressionParser(tokens);
        var ast = parser.Parse();
        return ast.Interpret(Context);
    }

    public Expression Parse(string expression)
    {
        var lexer = new ExpressionLexer(expression);
        var tokens = lexer.Tokenize();
        var parser = new ExpressionParser(tokens);
        return parser.Parse();
    }
}