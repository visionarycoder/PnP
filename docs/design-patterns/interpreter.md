# Interpreter Pattern

**Description**: Defines a representation for a language's grammar along with an interpreter that uses the representation to interpret sentences in the language. The pattern is useful for implementing domain-specific languages, query engines, configuration parsers, and mathematical expression evaluators.

**Language/Technology**: C#

**Code**:

## 1. Mathematical Expression Interpreter

```csharp
// Expression context for variable storage
public class ExpressionContext
{
    private readonly Dictionary<string, double> _variables = new();
    private readonly Dictionary<string, Func<double[], double>> _functions = new();
    
    public ExpressionContext()
    {
        // Built-in mathematical functions
        _functions["sin"] = args => Math.Sin(args[0]);
        _functions["cos"] = args => Math.Cos(args[0]);
        _functions["tan"] = args => Math.Tan(args[0]);
        _functions["sqrt"] = args => Math.Sqrt(args[0]);
        _functions["abs"] = args => Math.Abs(args[0]);
        _functions["log"] = args => Math.Log(args[0]);
        _functions["ln"] = args => Math.Log(args[0]);
        _functions["exp"] = args => Math.Exp(args[0]);
        _functions["pow"] = args => Math.Pow(args[0], args[1]);
        _functions["min"] = args => args.Min();
        _functions["max"] = args => args.Max();
        _functions["sum"] = args => args.Sum();
        _functions["avg"] = args => args.Average();
    }
    
    public void SetVariable(string name, double value)
    {
        _variables[name] = value;
    }
    
    public double GetVariable(string name)
    {
        if (_variables.TryGetValue(name, out var value))
        {
            return value;
        }
        throw new InvalidOperationException($"Variable '{name}' not found");
    }
    
    public bool HasVariable(string name)
    {
        return _variables.ContainsKey(name);
    }
    
    public void SetFunction(string name, Func<double[], double> function)
    {
        _functions[name] = function;
    }
    
    public double CallFunction(string name, params double[] arguments)
    {
        if (_functions.TryGetValue(name, out var function))
        {
            return function(arguments);
        }
        throw new InvalidOperationException($"Function '{name}' not found");
    }
    
    public bool HasFunction(string name)
    {
        return _functions.ContainsKey(name);
    }
    
    public IReadOnlyDictionary<string, double> Variables => _variables;
    public IReadOnlyCollection<string> FunctionNames => _functions.Keys;
    
    public ExpressionContext Clone()
    {
        var clone = new ExpressionContext();
        foreach (var (name, value) in _variables)
        {
            clone.SetVariable(name, value);
        }
        foreach (var (name, func) in _functions)
        {
            clone.SetFunction(name, func);
        }
        return clone;
    }
}

// Abstract expression interface
public abstract class Expression
{
    public abstract double Interpret(ExpressionContext context);
    public abstract string ToString();
    
    // Visitor pattern support for expression analysis
    public abstract TResult Accept<TResult>(IExpressionVisitor<TResult> visitor);
}

// Terminal expressions (leaf nodes)
public class NumberExpression : Expression
{
    private readonly double _value;
    
    public NumberExpression(double value)
    {
        _value = value;
    }
    
    public double Value => _value;
    
    public override double Interpret(ExpressionContext context)
    {
        return _value;
    }
    
    public override string ToString()
    {
        return _value.ToString("G");
    }
    
    public override TResult Accept<TResult>(IExpressionVisitor<TResult> visitor)
    {
        return visitor.Visit(this);
    }
}

public class VariableExpression : Expression
{
    private readonly string _name;
    
    public VariableExpression(string name)
    {
        _name = name ?? throw new ArgumentNullException(nameof(name));
    }
    
    public string Name => _name;
    
    public override double Interpret(ExpressionContext context)
    {
        return context.GetVariable(_name);
    }
    
    public override string ToString()
    {
        return _name;
    }
    
    public override TResult Accept<TResult>(IExpressionVisitor<TResult> visitor)
    {
        return visitor.Visit(this);
    }
}

// Non-terminal expressions (internal nodes)
public abstract class BinaryExpression : Expression
{
    protected readonly Expression _left;
    protected readonly Expression _right;
    
    protected BinaryExpression(Expression left, Expression right)
    {
        _left = left ?? throw new ArgumentNullException(nameof(left));
        _right = right ?? throw new ArgumentNullException(nameof(right));
    }
    
    public Expression Left => _left;
    public Expression Right => _right;
    
    protected abstract string OperatorSymbol { get; }
    
    public override string ToString()
    {
        return $"({_left} {OperatorSymbol} {_right})";
    }
}

public class AddExpression : BinaryExpression
{
    public AddExpression(Expression left, Expression right) : base(left, right) { }
    
    protected override string OperatorSymbol => "+";
    
    public override double Interpret(ExpressionContext context)
    {
        return _left.Interpret(context) + _right.Interpret(context);
    }
    
    public override TResult Accept<TResult>(IExpressionVisitor<TResult> visitor)
    {
        return visitor.Visit(this);
    }
}

public class SubtractExpression : BinaryExpression
{
    public SubtractExpression(Expression left, Expression right) : base(left, right) { }
    
    protected override string OperatorSymbol => "-";
    
    public override double Interpret(ExpressionContext context)
    {
        return _left.Interpret(context) - _right.Interpret(context);
    }
    
    public override TResult Accept<TResult>(IExpressionVisitor<TResult> visitor)
    {
        return visitor.Visit(this);
    }
}

public class MultiplyExpression : BinaryExpression
{
    public MultiplyExpression(Expression left, Expression right) : base(left, right) { }
    
    protected override string OperatorSymbol => "*";
    
    public override double Interpret(ExpressionContext context)
    {
        return _left.Interpret(context) * _right.Interpret(context);
    }
    
    public override TResult Accept<TResult>(IExpressionVisitor<TResult> visitor)
    {
        return visitor.Visit(this);
    }
}

public class DivideExpression : BinaryExpression
{
    public DivideExpression(Expression left, Expression right) : base(left, right) { }
    
    protected override string OperatorSymbol => "/";
    
    public override double Interpret(ExpressionContext context)
    {
        var rightValue = _right.Interpret(context);
        if (Math.Abs(rightValue) < double.Epsilon)
        {
            throw new DivideByZeroException("Division by zero");
        }
        return _left.Interpret(context) / rightValue;
    }
    
    public override TResult Accept<TResult>(IExpressionVisitor<TResult> visitor)
    {
        return visitor.Visit(this);
    }
}

public class PowerExpression : BinaryExpression
{
    public PowerExpression(Expression left, Expression right) : base(left, right) { }
    
    protected override string OperatorSymbol => "^";
    
    public override double Interpret(ExpressionContext context)
    {
        return Math.Pow(_left.Interpret(context), _right.Interpret(context));
    }
    
    public override TResult Accept<TResult>(IExpressionVisitor<TResult> visitor)
    {
        return visitor.Visit(this);
    }
}

public class ModuloExpression : BinaryExpression
{
    public ModuloExpression(Expression left, Expression right) : base(left, right) { }
    
    protected override string OperatorSymbol => "%";
    
    public override double Interpret(ExpressionContext context)
    {
        return _left.Interpret(context) % _right.Interpret(context);
    }
    
    public override TResult Accept<TResult>(IExpressionVisitor<TResult> visitor)
    {
        return visitor.Visit(this);
    }
}

// Unary expressions
public abstract class UnaryExpression : Expression
{
    protected readonly Expression _operand;
    
    protected UnaryExpression(Expression operand)
    {
        _operand = operand ?? throw new ArgumentNullException(nameof(operand));
    }
    
    public Expression Operand => _operand;
    
    protected abstract string OperatorSymbol { get; }
    
    public override string ToString()
    {
        return $"{OperatorSymbol}({_operand})";
    }
}

public class NegateExpression : UnaryExpression
{
    public NegateExpression(Expression operand) : base(operand) { }
    
    protected override string OperatorSymbol => "-";
    
    public override double Interpret(ExpressionContext context)
    {
        return -_operand.Interpret(context);
    }
    
    public override TResult Accept<TResult>(IExpressionVisitor<TResult> visitor)
    {
        return visitor.Visit(this);
    }
}

// Function call expression
public class FunctionExpression : Expression
{
    private readonly string _functionName;
    private readonly List<Expression> _arguments;
    
    public FunctionExpression(string functionName, params Expression[] arguments)
    {
        _functionName = functionName ?? throw new ArgumentNullException(nameof(functionName));
        _arguments = new List<Expression>(arguments ?? Array.Empty<Expression>());
    }
    
    public string FunctionName => _functionName;
    public IReadOnlyList<Expression> Arguments => _arguments;
    
    public override double Interpret(ExpressionContext context)
    {
        var args = _arguments.Select(arg => arg.Interpret(context)).ToArray();
        return context.CallFunction(_functionName, args);
    }
    
    public override string ToString()
    {
        var args = string.Join(", ", _arguments.Select(arg => arg.ToString()));
        return $"{_functionName}({args})";
    }
    
    public override TResult Accept<TResult>(IExpressionVisitor<TResult> visitor)
    {
        return visitor.Visit(this);
    }
}

// Conditional expression
public class ConditionalExpression : Expression
{
    private readonly Expression _condition;
    private readonly Expression _trueExpression;
    private readonly Expression _falseExpression;
    
    public ConditionalExpression(Expression condition, Expression trueExpression, Expression falseExpression)
    {
        _condition = condition ?? throw new ArgumentNullException(nameof(condition));
        _trueExpression = trueExpression ?? throw new ArgumentNullException(nameof(trueExpression));
        _falseExpression = falseExpression ?? throw new ArgumentNullException(nameof(falseExpression));
    }
    
    public Expression Condition => _condition;
    public Expression TrueExpression => _trueExpression;
    public Expression FalseExpression => _falseExpression;
    
    public override double Interpret(ExpressionContext context)
    {
        var conditionValue = _condition.Interpret(context);
        return Math.Abs(conditionValue) > double.Epsilon 
            ? _trueExpression.Interpret(context) 
            : _falseExpression.Interpret(context);
    }
    
    public override string ToString()
    {
        return $"({_condition} ? {_trueExpression} : {_falseExpression})";
    }
    
    public override TResult Accept<TResult>(IExpressionVisitor<TResult> visitor)
    {
        return visitor.Visit(this);
    }
}

// Expression visitor interface for analysis/transformation
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
```

## 2. Expression Parser

```csharp
// Token types for lexical analysis
public enum TokenType
{
    Number,
    Variable,
    Function,
    Plus,
    Minus,
    Multiply,
    Divide,
    Power,
    Modulo,
    LeftParen,
    RightParen,
    Comma,
    Question,
    Colon,
    EOF
}

public class Token
{
    public TokenType Type { get; set; }
    public string Value { get; set; } = "";
    public int Position { get; set; }
    
    public override string ToString()
    {
        return $"{Type}({Value}) at {Position}";
    }
}

// Lexical analyzer (tokenizer)
public class ExpressionLexer
{
    private readonly string _input;
    private int _position = 0;
    
    public ExpressionLexer(string input)
    {
        _input = input ?? throw new ArgumentNullException(nameof(input));
    }
    
    public List<Token> Tokenize()
    {
        var tokens = new List<Token>();
        
        while (_position < _input.Length)
        {
            SkipWhitespace();
            
            if (_position >= _input.Length)
                break;
            
            var token = ReadNextToken();
            if (token != null)
            {
                tokens.Add(token);
            }
        }
        
        tokens.Add(new Token { Type = TokenType.EOF, Position = _position });
        return tokens;
    }
    
    private Token? ReadNextToken()
    {
        var startPosition = _position;
        var currentChar = _input[_position];
        
        if (char.IsDigit(currentChar) || currentChar == '.')
        {
            return ReadNumber(startPosition);
        }
        
        if (char.IsLetter(currentChar) || currentChar == '_')
        {
            return ReadIdentifier(startPosition);
        }
        
        _position++;
        
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
        
        while (_position < _input.Length)
        {
            var ch = _input[_position];
            
            if (char.IsDigit(ch))
            {
                value += ch;
                _position++;
            }
            else if (ch == '.' && !hasDecimalPoint)
            {
                hasDecimalPoint = true;
                value += ch;
                _position++;
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
        
        while (_position < _input.Length && (char.IsLetterOrDigit(_input[_position]) || _input[_position] == '_'))
        {
            value += _input[_position];
            _position++;
        }
        
        // Check if it's followed by '(' to determine if it's a function
        SkipWhitespace();
        var tokenType = _position < _input.Length && _input[_position] == '(' 
            ? TokenType.Function 
            : TokenType.Variable;
        
        return new Token { Type = tokenType, Value = value, Position = startPosition };
    }
    
    private void SkipWhitespace()
    {
        while (_position < _input.Length && char.IsWhiteSpace(_input[_position]))
        {
            _position++;
        }
    }
}

// Recursive descent parser
public class ExpressionParser
{
    private readonly List<Token> _tokens;
    private int _position = 0;
    
    public ExpressionParser(List<Token> tokens)
    {
        _tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
    }
    
    public Expression Parse()
    {
        var expression = ParseConditional();
        
        if (_position < _tokens.Count - 1) // -1 for EOF token
        {
            throw new ArgumentException($"Unexpected token {CurrentToken} at position {_position}");
        }
        
        return expression;
    }
    
    // Grammar: conditional -> logical ('?' logical ':' logical)?
    private Expression ParseConditional()
    {
        var expression = ParseLogical();
        
        if (CurrentToken.Type == TokenType.Question)
        {
            Consume(TokenType.Question);
            var trueExpr = ParseLogical();
            Consume(TokenType.Colon);
            var falseExpr = ParseLogical();
            
            return new ConditionalExpression(expression, trueExpr, falseExpr);
        }
        
        return expression;
    }
    
    // Grammar: logical -> additive (('+'|'-') additive)*
    private Expression ParseLogical()
    {
        return ParseAdditive(); // Simplified for this example
    }
    
    // Grammar: additive -> multiplicative (('+'|'-') multiplicative)*
    private Expression ParseAdditive()
    {
        var left = ParseMultiplicative();
        
        while (CurrentToken.Type == TokenType.Plus || CurrentToken.Type == TokenType.Minus)
        {
            var op = CurrentToken.Type;
            _position++;
            var right = ParseMultiplicative();
            
            left = op == TokenType.Plus 
                ? new AddExpression(left, right)
                : new SubtractExpression(left, right);
        }
        
        return left;
    }
    
    // Grammar: multiplicative -> power (('*'|'/'|'%') power)*
    private Expression ParseMultiplicative()
    {
        var left = ParsePower();
        
        while (CurrentToken.Type == TokenType.Multiply || 
               CurrentToken.Type == TokenType.Divide || 
               CurrentToken.Type == TokenType.Modulo)
        {
            var op = CurrentToken.Type;
            _position++;
            var right = ParsePower();
            
            left = op switch
            {
                TokenType.Multiply => new MultiplyExpression(left, right),
                TokenType.Divide => new DivideExpression(left, right),
                TokenType.Modulo => new ModuloExpression(left, right),
                _ => throw new InvalidOperationException($"Unexpected operator: {op}")
            };
        }
        
        return left;
    }
    
    // Grammar: power -> unary ('^' unary)*
    private Expression ParsePower()
    {
        var left = ParseUnary();
        
        // Right associative
        if (CurrentToken.Type == TokenType.Power)
        {
            _position++;
            var right = ParsePower(); // Right associative recursion
            return new PowerExpression(left, right);
        }
        
        return left;
    }
    
    // Grammar: unary -> ('-')? primary
    private Expression ParseUnary()
    {
        if (CurrentToken.Type == TokenType.Minus)
        {
            _position++;
            var operand = ParseUnary();
            return new NegateExpression(operand);
        }
        
        return ParsePrimary();
    }
    
    // Grammar: primary -> number | variable | function | '(' expression ')'
    private Expression ParsePrimary()
    {
        var token = CurrentToken;
        
        switch (token.Type)
        {
            case TokenType.Number:
                _position++;
                if (double.TryParse(token.Value, out var number))
                {
                    return new NumberExpression(number);
                }
                throw new ArgumentException($"Invalid number format: {token.Value}");
            
            case TokenType.Variable:
                _position++;
                return new VariableExpression(token.Value);
            
            case TokenType.Function:
                return ParseFunction();
            
            case TokenType.LeftParen:
                _position++;
                var expression = ParseConditional();
                Consume(TokenType.RightParen);
                return expression;
            
            default:
                throw new ArgumentException($"Unexpected token: {token}");
        }
    }
    
    private Expression ParseFunction()
    {
        var functionName = CurrentToken.Value;
        _position++;
        
        Consume(TokenType.LeftParen);
        
        var arguments = new List<Expression>();
        
        if (CurrentToken.Type != TokenType.RightParen)
        {
            arguments.Add(ParseConditional());
            
            while (CurrentToken.Type == TokenType.Comma)
            {
                _position++;
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
            throw new ArgumentException($"Expected {expectedType}, got {CurrentToken.Type} at position {_position}");
        }
        _position++;
    }
    
    private Token CurrentToken => _position < _tokens.Count ? _tokens[_position] : _tokens[^1];
}

// Expression evaluator that combines lexer and parser
public class ExpressionEvaluator
{
    private readonly ExpressionContext _context;
    
    public ExpressionEvaluator(ExpressionContext? context = null)
    {
        _context = context ?? new ExpressionContext();
    }
    
    public double Evaluate(string expression)
    {
        var lexer = new ExpressionLexer(expression);
        var tokens = lexer.Tokenize();
        
        var parser = new ExpressionParser(tokens);
        var expressionTree = parser.Parse();
        
        return expressionTree.Interpret(_context);
    }
    
    public Expression Parse(string expression)
    {
        var lexer = new ExpressionLexer(expression);
        var tokens = lexer.Tokenize();
        
        var parser = new ExpressionParser(tokens);
        return parser.Parse();
    }
    
    public ExpressionContext Context => _context;
}
```

## 3. Rule Engine Interpreter

```csharp
// Rule engine for business logic
public interface IRule
{
    string Name { get; }
    bool Evaluate(RuleContext context);
    void Execute(RuleContext context);
}

public class RuleContext
{
    private readonly Dictionary<string, object> _facts = new();
    private readonly List<string> _executedActions = new();
    
    public void SetFact(string name, object value)
    {
        _facts[name] = value;
    }
    
    public T GetFact<T>(string name)
    {
        if (_facts.TryGetValue(name, out var value))
        {
            if (value is T typedValue)
            {
                return typedValue;
            }
            
            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                throw new InvalidCastException($"Cannot convert fact '{name}' from {value?.GetType()} to {typeof(T)}");
            }
        }
        throw new KeyNotFoundException($"Fact '{name}' not found");
    }
    
    public bool HasFact(string name)
    {
        return _facts.ContainsKey(name);
    }
    
    public void AddExecutedAction(string action)
    {
        _executedActions.Add(action);
    }
    
    public IReadOnlyList<string> ExecutedActions => _executedActions;
    public IReadOnlyDictionary<string, object> Facts => _facts;
    
    public override string ToString()
    {
        var facts = string.Join(", ", _facts.Select(kv => $"{kv.Key}={kv.Value}"));
        var actions = string.Join(", ", _executedActions);
        return $"RuleContext(Facts: [{facts}], Actions: [{actions}])";
    }
}

// Abstract rule base class
public abstract class Rule : IRule
{
    public string Name { get; protected set; } = "";
    
    public abstract bool Evaluate(RuleContext context);
    public abstract void Execute(RuleContext context);
    
    public override string ToString() => Name;
}

// Composite rule for combining multiple rules
public abstract class CompositeRule : Rule
{
    protected readonly List<IRule> _rules = new();
    
    public void AddRule(IRule rule)
    {
        _rules.Add(rule);
    }
    
    public IReadOnlyList<IRule> Rules => _rules;
}

public class AndRule : CompositeRule
{
    public AndRule(string name, params IRule[] rules)
    {
        Name = name;
        _rules.AddRange(rules);
    }
    
    public override bool Evaluate(RuleContext context)
    {
        return _rules.All(rule => rule.Evaluate(context));
    }
    
    public override void Execute(RuleContext context)
    {
        if (Evaluate(context))
        {
            foreach (var rule in _rules)
            {
                rule.Execute(context);
            }
            context.AddExecutedAction($"Executed AND rule: {Name}");
        }
    }
}

public class OrRule : CompositeRule
{
    public OrRule(string name, params IRule[] rules)
    {
        Name = name;
        _rules.AddRange(rules);
    }
    
    public override bool Evaluate(RuleContext context)
    {
        return _rules.Any(rule => rule.Evaluate(context));
    }
    
    public override void Execute(RuleContext context)
    {
        foreach (var rule in _rules.Where(r => r.Evaluate(context)))
        {
            rule.Execute(context);
        }
        
        if (_rules.Any(r => r.Evaluate(context)))
        {
            context.AddExecutedAction($"Executed OR rule: {Name}");
        }
    }
}

public class NotRule : Rule
{
    private readonly IRule _rule;
    
    public NotRule(string name, IRule rule)
    {
        Name = name;
        _rule = rule;
    }
    
    public override bool Evaluate(RuleContext context)
    {
        return !_rule.Evaluate(context);
    }
    
    public override void Execute(RuleContext context)
    {
        if (Evaluate(context))
        {
            context.AddExecutedAction($"Executed NOT rule: {Name}");
        }
    }
}

// Conditional rule
public class ConditionalRule : Rule
{
    private readonly Func<RuleContext, bool> _condition;
    private readonly Action<RuleContext> _action;
    
    public ConditionalRule(string name, Func<RuleContext, bool> condition, Action<RuleContext> action)
    {
        Name = name;
        _condition = condition;
        _action = action;
    }
    
    public override bool Evaluate(RuleContext context)
    {
        return _condition(context);
    }
    
    public override void Execute(RuleContext context)
    {
        if (Evaluate(context))
        {
            _action(context);
            context.AddExecutedAction($"Executed conditional rule: {Name}");
        }
    }
}

// Rule engine
public class RuleEngine
{
    private readonly List<IRule> _rules = new();
    
    public void AddRule(IRule rule)
    {
        _rules.Add(rule);
    }
    
    public void RemoveRule(string name)
    {
        _rules.RemoveAll(r => r.Name == name);
    }
    
    public void ExecuteRules(RuleContext context)
    {
        foreach (var rule in _rules)
        {
            try
            {
                rule.Execute(context);
            }
            catch (Exception ex)
            {
                context.AddExecutedAction($"ERROR in rule '{rule.Name}': {ex.Message}");
            }
        }
    }
    
    public List<IRule> GetMatchingRules(RuleContext context)
    {
        return _rules.Where(rule => rule.Evaluate(context)).ToList();
    }
    
    public IReadOnlyList<IRule> Rules => _rules;
}
```

## 4. Query Language Interpreter

```csharp
// Simple query language for data filtering
public class Query
{
    public string Table { get; set; } = "";
    public List<string> SelectFields { get; set; } = new();
    public IQueryCondition? WhereCondition { get; set; }
    public List<string> OrderByFields { get; set; } = new();
    public bool OrderDescending { get; set; }
    public int? Limit { get; set; }
    
    public override string ToString()
    {
        var select = SelectFields.Any() ? string.Join(", ", SelectFields) : "*";
        var query = $"SELECT {select} FROM {Table}";
        
        if (WhereCondition != null)
        {
            query += $" WHERE {WhereCondition}";
        }
        
        if (OrderByFields.Any())
        {
            var order = string.Join(", ", OrderByFields);
            query += $" ORDER BY {order}";
            if (OrderDescending) query += " DESC";
        }
        
        if (Limit.HasValue)
        {
            query += $" LIMIT {Limit.Value}";
        }
        
        return query;
    }
}

public interface IQueryCondition
{
    bool Evaluate(Dictionary<string, object> record);
}

public class FieldCondition : IQueryCondition
{
    public string FieldName { get; set; } = "";
    public string Operator { get; set; } = "";
    public object Value { get; set; } = null!;
    
    public bool Evaluate(Dictionary<string, object> record)
    {
        if (!record.TryGetValue(FieldName, out var fieldValue))
        {
            return false;
        }
        
        return Operator switch
        {
            "=" => Equals(fieldValue, Value),
            "!=" => !Equals(fieldValue, Value),
            "<" => Comparer.Default.Compare(fieldValue, Value) < 0,
            "<=" => Comparer.Default.Compare(fieldValue, Value) <= 0,
            ">" => Comparer.Default.Compare(fieldValue, Value) > 0,
            ">=" => Comparer.Default.Compare(fieldValue, Value) >= 0,
            "LIKE" => fieldValue.ToString()!.Contains(Value.ToString()!, StringComparison.OrdinalIgnoreCase),
            "IN" => Value is IEnumerable<object> values && values.Contains(fieldValue),
            _ => throw new NotSupportedException($"Operator '{Operator}' not supported")
        };
    }
    
    public override string ToString()
    {
        return $"{FieldName} {Operator} {Value}";
    }
}

public class CompositeCondition : IQueryCondition
{
    public string LogicalOperator { get; set; } = "";
    public List<IQueryCondition> Conditions { get; set; } = new();
    
    public bool Evaluate(Dictionary<string, object> record)
    {
        return LogicalOperator.ToUpper() switch
        {
            "AND" => Conditions.All(c => c.Evaluate(record)),
            "OR" => Conditions.Any(c => c.Evaluate(record)),
            _ => throw new NotSupportedException($"Logical operator '{LogicalOperator}' not supported")
        };
    }
    
    public override string ToString()
    {
        return $"({string.Join($" {LogicalOperator} ", Conditions)})";
    }
}

public class QueryEngine
{
    private readonly Dictionary<string, List<Dictionary<string, object>>> _tables = new();
    
    public void CreateTable(string name, List<Dictionary<string, object>> data)
    {
        _tables[name] = new List<Dictionary<string, object>>(data);
    }
    
    public List<Dictionary<string, object>> ExecuteQuery(Query query)
    {
        if (!_tables.TryGetValue(query.Table, out var tableData))
        {
            throw new ArgumentException($"Table '{query.Table}' not found");
        }
        
        var results = tableData.AsEnumerable();
        
        // Apply WHERE condition
        if (query.WhereCondition != null)
        {
            results = results.Where(record => query.WhereCondition.Evaluate(record));
        }
        
        // Apply ORDER BY
        if (query.OrderByFields.Any())
        {
            var orderedResults = results.OrderBy(record => GetFieldValue(record, query.OrderByFields[0]));
            
            for (int i = 1; i < query.OrderByFields.Count; i++)
            {
                var field = query.OrderByFields[i];
                orderedResults = orderedResults.ThenBy(record => GetFieldValue(record, field));
            }
            
            results = query.OrderDescending ? orderedResults.Reverse() : orderedResults;
        }
        
        // Apply LIMIT
        if (query.Limit.HasValue)
        {
            results = results.Take(query.Limit.Value);
        }
        
        var resultList = results.ToList();
        
        // Apply SELECT (field filtering)
        if (query.SelectFields.Any())
        {
            resultList = resultList.Select(record => 
                query.SelectFields.ToDictionary(
                    field => field, 
                    field => record.TryGetValue(field, out var value) ? value : null!)
            ).ToList();
        }
        
        return resultList;
    }
    
    private object GetFieldValue(Dictionary<string, object> record, string fieldName)
    {
        return record.TryGetValue(fieldName, out var value) ? value : "";
    }
    
    public IReadOnlyDictionary<string, List<Dictionary<string, object>>> Tables => _tables;
}

// Simple query parser
public class QueryParser
{
    public Query Parse(string queryString)
    {
        var parts = queryString.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var query = new Query();
        
        for (int i = 0; i < parts.Length; i++)
        {
            var part = parts[i].ToUpper();
            
            switch (part)
            {
                case "SELECT":
                    i = ParseSelect(parts, i + 1, query);
                    break;
                
                case "FROM":
                    if (i + 1 < parts.Length)
                    {
                        query.Table = parts[i + 1];
                        i++;
                    }
                    break;
                
                case "WHERE":
                    i = ParseWhere(parts, i + 1, query);
                    break;
                
                case "ORDER":
                    if (i + 1 < parts.Length && parts[i + 1].ToUpper() == "BY")
                    {
                        i = ParseOrderBy(parts, i + 2, query);
                    }
                    break;
                
                case "LIMIT":
                    if (i + 1 < parts.Length && int.TryParse(parts[i + 1], out var limit))
                    {
                        query.Limit = limit;
                        i++;
                    }
                    break;
            }
        }
        
        return query;
    }
    
    private int ParseSelect(string[] parts, int startIndex, Query query)
    {
        int i = startIndex;
        
        while (i < parts.Length && parts[i].ToUpper() != "FROM")
        {
            var field = parts[i].TrimEnd(',');
            if (field != "*")
            {
                query.SelectFields.Add(field);
            }
            i++;
        }
        
        return i - 1;
    }
    
    private int ParseWhere(string[] parts, int startIndex, Query query)
    {
        // Simplified WHERE parsing - assumes single condition
        if (startIndex + 2 < parts.Length)
        {
            var field = parts[startIndex];
            var op = parts[startIndex + 1];
            var value = parts[startIndex + 2].Trim('\'', '"');
            
            // Try to parse as number
            if (double.TryParse(value, out var numValue))
            {
                query.WhereCondition = new FieldCondition { FieldName = field, Operator = op, Value = numValue };
            }
            else
            {
                query.WhereCondition = new FieldCondition { FieldName = field, Operator = op, Value = value };
            }
            
            return startIndex + 2;
        }
        
        return startIndex;
    }
    
    private int ParseOrderBy(string[] parts, int startIndex, Query query)
    {
        int i = startIndex;
        
        while (i < parts.Length)
        {
            var part = parts[i].ToUpper();
            
            if (part == "DESC")
            {
                query.OrderDescending = true;
                break;
            }
            else if (part == "LIMIT")
            {
                break;
            }
            else
            {
                query.OrderByFields.Add(parts[i].TrimEnd(','));
            }
            
            i++;
        }
        
        return i - 1;
    }
}
```

**Usage**:

```csharp
// 1. Mathematical Expression Evaluation
var evaluator = new ExpressionEvaluator();

// Set some variables
evaluator.Context.SetVariable("x", 10);
evaluator.Context.SetVariable("y", 5);
evaluator.Context.SetVariable("pi", Math.PI);

// Evaluate various expressions
var expressions = new[]
{
    "2 + 3 * 4",                    // Basic arithmetic
    "x * y + 10",                   // Variables
    "sqrt(x^2 + y^2)",             // Functions and power
    "sin(pi/2) + cos(0)",          // Trigonometric functions
    "x > y ? x : y",               // Conditional
    "abs(-15) + max(1,2,3,4,5)",   // Multiple arguments
    "(x + y) / 2"                  // Parentheses
};

Console.WriteLine("Mathematical Expression Evaluation:");
foreach (var expr in expressions)
{
    try
    {
        var result = evaluator.Evaluate(expr);
        Console.WriteLine($"{expr} = {result:F4}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"{expr} = ERROR: {ex.Message}");
    }
}

// 2. Rule Engine Example
var ruleEngine = new RuleEngine();
var ruleContext = new RuleContext();

// Set up facts
ruleContext.SetFact("age", 25);
ruleContext.SetFact("income", 50000);
ruleContext.SetFact("creditScore", 720);
ruleContext.SetFact("hasJob", true);

// Create rules
var ageRule = new ConditionalRule("Age Check", 
    ctx => ctx.GetFact<int>("age") >= 21,
    ctx => ctx.SetFact("ageEligible", true));

var incomeRule = new ConditionalRule("Income Check",
    ctx => ctx.GetFact<int>("income") >= 30000,
    ctx => ctx.SetFact("incomeEligible", true));

var creditRule = new ConditionalRule("Credit Check",
    ctx => ctx.GetFact<int>("creditScore") >= 650,
    ctx => ctx.SetFact("creditEligible", true));

var jobRule = new ConditionalRule("Employment Check",
    ctx => ctx.GetFact<bool>("hasJob"),
    ctx => ctx.SetFact("employmentEligible", true));

// Composite rule
var loanApprovalRule = new AndRule("Loan Approval",
    ageRule, incomeRule, creditRule, jobRule);

var approvalAction = new ConditionalRule("Final Approval",
    ctx => ctx.HasFact("ageEligible") && 
           ctx.HasFact("incomeEligible") && 
           ctx.HasFact("creditEligible") && 
           ctx.HasFact("employmentEligible"),
    ctx => ctx.SetFact("loanApproved", true));

ruleEngine.AddRule(loanApprovalRule);
ruleEngine.AddRule(approvalAction);

Console.WriteLine("\nRule Engine Execution:");
Console.WriteLine($"Initial context: {ruleContext}");
ruleEngine.ExecuteRules(ruleContext);
Console.WriteLine($"Final context: {ruleContext}");
Console.WriteLine($"Loan approved: {ruleContext.HasFact("loanApproved") && ruleContext.GetFact<bool>("loanApproved")}");

// 3. Query Language Example
var queryEngine = new QueryEngine();

// Sample data
var employees = new List<Dictionary<string, object>>
{
    new() { ["id"] = 1, ["name"] = "John Doe", ["age"] = 30, ["salary"] = 50000, ["department"] = "IT" },
    new() { ["id"] = 2, ["name"] = "Jane Smith", ["age"] = 25, ["salary"] = 45000, ["department"] = "HR" },
    new() { ["id"] = 3, ["name"] = "Bob Johnson", ["age"] = 35, ["salary"] = 60000, ["department"] = "IT" },
    new() { ["id"] = 4, ["name"] = "Alice Brown", ["age"] = 28, ["salary"] = 52000, ["department"] = "Finance" },
    new() { ["id"] = 5, ["name"] = "Charlie Wilson", ["age"] = 32, ["salary"] = 48000, ["department"] = "IT" }
};

queryEngine.CreateTable("employees", employees);

var queries = new[]
{
    "SELECT name, salary FROM employees WHERE department = IT",
    "SELECT * FROM employees WHERE age > 30 ORDER BY salary DESC",
    "SELECT name FROM employees WHERE salary >= 50000 LIMIT 2"
};

var parser = new QueryParser();

Console.WriteLine("\nQuery Language Execution:");
foreach (var queryString in queries)
{
    try
    {
        var query = parser.Parse(queryString);
        var results = queryEngine.ExecuteQuery(query);
        
        Console.WriteLine($"\nQuery: {queryString}");
        Console.WriteLine($"Parsed: {query}");
        Console.WriteLine($"Results ({results.Count} records):");
        
        foreach (var record in results)
        {
            var fields = string.Join(", ", record.Select(kv => $"{kv.Key}={kv.Value}"));
            Console.WriteLine($"  {fields}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Query error: {ex.Message}");
    }
}

// Expected output demonstrates:
// - Mathematical expressions parsed and evaluated correctly
// - Rule engine executing business logic with fact inference
// - Query language parsing and executing data filtering operations
// - Proper error handling for invalid expressions or queries
// - Extensible grammar that can be enhanced with new operators/functions
```

**Notes**:

- **Grammar Definition**: Interpreter pattern requires formal grammar definition using BNF or similar notation
- **Recursive Descent**: Parser uses recursive descent for handling operator precedence and associativity
- **Abstract Syntax Tree**: Expression trees represent parsed grammar in executable form
- **Context Management**: Interpreter context maintains state (variables, functions) during evaluation
- **Extensibility**: Easy to add new operators, functions, or language constructs
- **Performance**: Consider caching parsed expressions for repeated evaluation
- **Error Handling**: Provide meaningful error messages with position information
- **Type Safety**: Implement proper type checking and conversion in interpreters

**Prerequisites**:

- .NET 6.0 or later for modern C# features and pattern matching
- Understanding of formal grammars and parsing techniques
- Knowledge of compiler construction concepts (lexing, parsing, AST)
- Familiarity with recursive descent parsing algorithms

**Related Patterns**:

- **Composite**: AST nodes form Composite structure for expression trees
- **Visitor**: Expression trees often use Visitor pattern for analysis/transformation
- **Strategy**: Different interpretation strategies can be implemented as separate classes
- **Command**: Parsed commands can be encapsulated and executed using Command pattern
