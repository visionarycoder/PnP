namespace Snippets.DesignPatterns.Behavioral.Interpreter;

/// <summary>
/// Interpreter Pattern Implementation
/// Defines a representation for a language's grammar along with an interpreter 
/// that uses the representation to interpret sentences in the language.
/// </summary>

// Expression context for variable storage
public class ExpressionContext
{
    private readonly Dictionary<string, double> variables = new();
    private readonly Dictionary<string, Func<double[], double>> functions = new();

    public ExpressionContext()
    {
        // Built-in mathematical functions
        functions["sin"] = args => Math.Sin(args[0]);
        functions["cos"] = args => Math.Cos(args[0]);
        functions["tan"] = args => Math.Tan(args[0]);
        functions["sqrt"] = args => Math.Sqrt(args[0]);
        functions["abs"] = args => Math.Abs(args[0]);
        functions["log"] = args => Math.Log(args[0]);
        functions["ln"] = args => Math.Log(args[0]);
        functions["exp"] = args => Math.Exp(args[0]);
        functions["pow"] = args => Math.Pow(args[0], args[1]);
        functions["min"] = args => args.Min();
        functions["max"] = args => args.Max();
        functions["sum"] = args => args.Sum();
        functions["avg"] = args => args.Average();
    }

    public void SetVariable(string name, double value)
    {
        variables[name] = value;
    }

    public double GetVariable(string name)
    {
        if (variables.TryGetValue(name, out var value))
        {
            return value;
        }

        throw new InvalidOperationException($"Variable '{name}' not found");
    }

    public bool HasVariable(string name)
    {
        return variables.ContainsKey(name);
    }

    public void SetFunction(string name, Func<double[], double> function)
    {
        functions[name] = function;
    }

    public double CallFunction(string name, params double[] arguments)
    {
        if (functions.TryGetValue(name, out var function))
        {
            return function(arguments);
        }

        throw new InvalidOperationException($"Function '{name}' not found");
    }

    public bool HasFunction(string name)
    {
        return functions.ContainsKey(name);
    }

    public IReadOnlyDictionary<string, double> Variables => variables;
    public IReadOnlyCollection<string> FunctionNames => functions.Keys;

    public ExpressionContext Clone()
    {
        var clone = new ExpressionContext();
        foreach (var (name, value) in variables)
        {
            clone.SetVariable(name, value);
        }

        foreach (var (name, func) in functions)
        {
            clone.SetFunction(name, func);
        }

        return clone;
    }
}

// Abstract expression interface

// Terminal expressions (leaf nodes)

// Non-terminal expressions (internal nodes)

// Unary expressions

// Function call expression

// Conditional expression

// Expression visitor interface for analysis/transformation

// Token types for lexical analysis

// Lexical analyzer (tokenizer)

// Recursive descent parser  

// Expression evaluator with parsing

// Expression analyzer visitor