namespace Snippets.DesignPatterns.Behavioral.Interpreter;

public class ExpressionLexer(string input)
{
    private readonly string input = input ?? throw new ArgumentNullException(nameof(input));
    private int position = 0;

    public List<Token> Tokenize()
    {
        var tokens = new List<Token>();

        while (position < input.Length)
        {
            SkipWhitespace();

            if (position >= input.Length)
                break;

            var token = ReadNextToken();
            if (token != null)
            {
                tokens.Add(token);
            }
        }

        tokens.Add(new Token { Type = TokenType.Eof, Position = position });
        return tokens;
    }

    private Token? ReadNextToken()
    {
        var startPosition = position;
        var currentChar = input[position];

        if (char.IsDigit(currentChar) || currentChar == '.')
        {
            return ReadNumber(startPosition);
        }

        if (char.IsLetter(currentChar) || currentChar == '_')
        {
            return ReadIdentifier(startPosition);
        }

        position++;

        return currentChar switch
        {
            '+' => new Token { Type = TokenType.Plus, Value = "+", Position = startPosition },
            '-' => new Token { Type = TokenType.Minus, Value = "-", Position = startPosition },
            '*' => new Token { Type = TokenType.Multiply, Value = "*", Position = startPosition },
            '/' => new Token { Type = TokenType.Divide, Value = "/", Position = startPosition },
            '^' => new Token { Type = TokenType.Power, Value = "^", Position = startPosition },
            '%' => new Token { Type = TokenType.Modulo, Value = "%", Position = startPosition },
            '(' => new Token { Type = TokenType.LeftParen, Value = "(", Position = startPosition },
            ')' => new Token { Type = TokenType.RightParen, Value = ")", Position = startPosition },
            ',' => new Token { Type = TokenType.Comma, Value = ",", Position = startPosition },
            '?' => new Token { Type = TokenType.Question, Value = "?", Position = startPosition },
            ':' => new Token { Type = TokenType.Colon, Value = ":", Position = startPosition },
            _ => throw new ArgumentException($"Unexpected character '{currentChar}' at position {startPosition}")
        };
    }

    private Token ReadNumber(int startPosition)
    {
        var value = "";
        bool hasDecimalPoint = false;

        while (position < input.Length)
        {
            var ch = input[position];

            if (char.IsDigit(ch))
            {
                value += ch;
                position++;
            }
            else if (ch == '.' && !hasDecimalPoint)
            {
                hasDecimalPoint = true;
                value += ch;
                position++;
            }
            else
            {
                break;
            }
        }

        return new Token { Type = TokenType.Number, Value = value, Position = startPosition };
    }

    private Token ReadIdentifier(int startPosition)
    {
        var value = "";

        while (position < input.Length && (char.IsLetterOrDigit(input[position]) || input[position] == '_'))
        {
            value += input[position];
            position++;
        }

        // Check if it's followed by '(' to determine if it's a function
        SkipWhitespace();
        var tokenType = position < input.Length && input[position] == '('
            ? TokenType.Function
            : TokenType.Variable;

        return new Token { Type = tokenType, Value = value, Position = startPosition };
    }

    private void SkipWhitespace()
    {
        while (position < input.Length && char.IsWhiteSpace(input[position]))
        {
            position++;
        }
    }
}