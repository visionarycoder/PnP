namespace Snippets.DesignPatterns.Behavioral.Visitor;

/// <summary>
/// Visitor for evaluating AST expressions
/// </summary>
public class EvaluationVisitor : IAstVisitor<object>
{
    private readonly Dictionary<string, object> variables = new();
    private readonly Dictionary<string, Func<object[], object>> functions = new();

    public EvaluationVisitor()
    {
        // Built-in mathematical functions
        functions["sin"] = args => Math.Sin(Convert.ToDouble(args[0]));
        functions["cos"] = args => Math.Cos(Convert.ToDouble(args[0]));
        functions["sqrt"] = args => Math.Sqrt(Convert.ToDouble(args[0]));
        functions["abs"] = args => Math.Abs(Convert.ToDouble(args[0]));
        functions["pow"] = args => Math.Pow(Convert.ToDouble(args[0]), Convert.ToDouble(args[1]));
        functions["max"] = args => Math.Max(Convert.ToDouble(args[0]), Convert.ToDouble(args[1]));
        functions["min"] = args => Math.Min(Convert.ToDouble(args[0]), Convert.ToDouble(args[1]));
        functions["log"] = args => Math.Log(Convert.ToDouble(args[0]));
        functions["exp"] = args => Math.Exp(Convert.ToDouble(args[0]));

        // Built-in constants
        variables["pi"] = Math.PI;
        variables["e"] = Math.E;
    }

    public void SetVariable(string name, object value)
    {
        variables[name] = value;
    }

    public void SetFunction(string name, Func<object[], object> function)
    {
        functions[name] = function;
    }

    public object Visit(LiteralNode node)
    {
        return node.Value;
    }

    public object Visit(VariableNode node)
    {
        if (variables.TryGetValue(node.Name, out var value))
        {
            return value;
        }

        throw new InvalidOperationException($"Undefined variable: {node.Name}");
    }

    public object Visit(BinaryOperationNode node)
    {
        var left = node.Left.Accept(this);
        var right = node.Right.Accept(this);

        return node.Operator switch
        {
            "+" => AddValues(left, right),
            "-" => SubtractValues(left, right),
            "*" => MultiplyValues(left, right),
            "/" => DivideValues(left, right),
            "%" => ModuloValues(left, right),
            "^" => Math.Pow(Convert.ToDouble(left), Convert.ToDouble(right)),
            "==" => Equals(left, right),
            "!=" => !Equals(left, right),
            "<" => CompareValues(left, right) < 0,
            "<=" => CompareValues(left, right) <= 0,
            ">" => CompareValues(left, right) > 0,
            ">=" => CompareValues(left, right) >= 0,
            "&&" => Convert.ToBoolean(left) && Convert.ToBoolean(right),
            "||" => Convert.ToBoolean(left) || Convert.ToBoolean(right),
            _ => throw new NotSupportedException($"Operator {node.Operator} not supported")
        };
    }

    public object Visit(UnaryOperationNode node)
    {
        var operand = node.Operand.Accept(this);

        return node.Operator switch
        {
            "-" => NegateValue(operand),
            "!" => !Convert.ToBoolean(operand),
            "+" => operand,
            _ => throw new NotSupportedException($"Unary operator {node.Operator} not supported")
        };
    }

    public object Visit(FunctionCallNode node)
    {
        if (functions.TryGetValue(node.FunctionName, out var function))
        {
            var args = node.Arguments.Select(arg => arg.Accept(this)).ToArray();
            return function(args);
        }

        throw new InvalidOperationException($"Undefined function: {node.FunctionName}");
    }

    public object Visit(AssignmentNode node)
    {
        var value = node.Value.Accept(this);
        variables[node.VariableName] = value;
        return value;
    }

    public object Visit(BlockNode node)
    {
        object result = null!;
        foreach (var statement in node.Statements)
        {
            result = statement.Accept(this);
        }

        return result;
    }

    public object Visit(ElementA element) => throw new NotSupportedException();
    public object Visit(ElementB element) => throw new NotSupportedException();
    public object Visit(ElementC element) => throw new NotSupportedException();

    // Helper methods for arithmetic operations
    private object AddValues(object left, object right)
    {
        if (left is string || right is string)
            return left.ToString() + right.ToString();

        return Convert.ToDouble(left) + Convert.ToDouble(right);
    }

    private object SubtractValues(object left, object right)
    {
        return Convert.ToDouble(left) - Convert.ToDouble(right);
    }

    private object MultiplyValues(object left, object right)
    {
        return Convert.ToDouble(left) * Convert.ToDouble(right);
    }

    private object DivideValues(object left, object right)
    {
        var rightVal = Convert.ToDouble(right);
        if (Math.Abs(rightVal) < double.Epsilon)
            throw new DivideByZeroException("Division by zero");

        return Convert.ToDouble(left) / rightVal;
    }

    private object ModuloValues(object left, object right)
    {
        return Convert.ToDouble(left) % Convert.ToDouble(right);
    }

    private object NegateValue(object value)
    {
        return -Convert.ToDouble(value);
    }

    private int CompareValues(object left, object right)
    {
        if (left is IComparable leftComparable && right is IComparable)
        {
            return leftComparable.CompareTo(right);
        }

        var leftDouble = Convert.ToDouble(left);
        var rightDouble = Convert.ToDouble(right);
        return leftDouble.CompareTo(rightDouble);
    }
}