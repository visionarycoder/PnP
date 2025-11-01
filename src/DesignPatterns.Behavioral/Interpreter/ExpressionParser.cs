namespace Snippets.DesignPatterns.Behavioral.Interpreter;

public class ExpressionParser(List<Token> tokens)
{
    private readonly List<Token> tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
    private int position = 0;

    private Token CurrentToken => position < tokens.Count ? tokens[position] : tokens[^1];

    public Expression Parse()
    {
        var expression = ParseConditional();

        if (position < tokens.Count - 1) // -1 for EOF token
        {
            throw new ArgumentException($"Unexpected token {CurrentToken} at position {position}");
        }

        return expression;
    }

    // Grammar: conditional -> additive ('?' additive ':' additive)?
    private Expression ParseConditional()
    {
        var expression = ParseAdditive();

        if (CurrentToken.Type == TokenType.Question)
        {
            Consume(TokenType.Question);
            var trueExpr = ParseAdditive();
            Consume(TokenType.Colon);
            var falseExpr = ParseAdditive();
            return new ConditionalExpression(expression, trueExpr, falseExpr);
        }

        return expression;
    }

    // Grammar: additive -> multiplicative (('+'|'-') multiplicative)*
    private Expression ParseAdditive()
    {
        var left = ParseMultiplicative();

        while (CurrentToken.Type is TokenType.Plus or TokenType.Minus)
        {
            var operatorToken = CurrentToken;
            position++;
            var right = ParseMultiplicative();

            left = operatorToken.Type switch
            {
                TokenType.Plus => new AddExpression(left, right),
                TokenType.Minus => new SubtractExpression(left, right),
                _ => throw new InvalidOperationException($"Unexpected operator: {operatorToken.Type}")
            };
        }

        return left;
    }

    // Grammar: multiplicative -> power (('*'|'/'|'%') power)*
    private Expression ParseMultiplicative()
    {
        var left = ParsePower();

        while (CurrentToken.Type is TokenType.Multiply or TokenType.Divide or TokenType.Modulo)
        {
            var operatorToken = CurrentToken;
            position++;
            var right = ParsePower();

            left = operatorToken.Type switch
            {
                TokenType.Multiply => new MultiplyExpression(left, right),
                TokenType.Divide => new DivideExpression(left, right),
                TokenType.Modulo => new ModuloExpression(left, right),
                _ => throw new InvalidOperationException($"Unexpected operator: {operatorToken.Type}")
            };
        }

        return left;
    }

    // Grammar: power -> unary ('^' unary)*
    private Expression ParsePower()
    {
        var left = ParseUnary();

        while (CurrentToken.Type == TokenType.Power)
        {
            position++;
            var right = ParseUnary();
            left = new PowerExpression(left, right);
        }

        return left;
    }

    // Grammar: unary -> ('-')? primary
    private Expression ParseUnary()
    {
        if (CurrentToken.Type == TokenType.Minus)
        {
            position++;
            var operand = ParsePrimary();
            return new NegateExpression(operand);
        }

        return ParsePrimary();
    }

    // Grammar: primary -> number | variable | function | '(' expression ')'
    private Expression ParsePrimary()
    {
        switch (CurrentToken.Type)
        {
            case TokenType.Number:
                var numberValue = double.Parse(CurrentToken.Value);
                position++;
                return new NumberExpression(numberValue);

            case TokenType.Variable:
                var variableName = CurrentToken.Value;
                position++;
                return new VariableExpression(variableName);

            case TokenType.Function:
                return ParseFunction();

            case TokenType.LeftParen:
                position++;
                var expression = ParseConditional();
                Consume(TokenType.RightParen);
                return expression;

            default:
                throw new ArgumentException($"Unexpected token: {CurrentToken}");
        }
    }

    private Expression ParseFunction()
    {
        var functionName = CurrentToken.Value;
        position++;

        Consume(TokenType.LeftParen);

        var arguments = new List<Expression>();

        if (CurrentToken.Type != TokenType.RightParen)
        {
            arguments.Add(ParseConditional());

            while (CurrentToken.Type == TokenType.Comma)
            {
                position++;
                arguments.Add(ParseConditional());
            }
        }

        Consume(TokenType.RightParen);

        return new FunctionExpression(functionName, arguments.ToArray());
    }

    private void Consume(TokenType expectedType)
    {
        if (CurrentToken.Type != expectedType)
        {
            throw new ArgumentException(
                $"Expected {expectedType} but found {CurrentToken.Type} at position {position}");
        }

        position++;
    }
}