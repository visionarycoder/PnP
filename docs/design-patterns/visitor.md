# Visitor Pattern

**Description**: Separates algorithms from the objects they operate on by defining a family of operations in separate visitor classes. The pattern uses double dispatch to determine both the visitor type and the element type at runtime, allowing new operations to be added without modifying existing object structures.

**Language/Technology**: C#

**Code**:

## 1. Basic Visitor Structure

```csharp
// Visitor interface
public interface IVisitor<TResult>
{
    TResult Visit(ElementA element);
    TResult Visit(ElementB element);
    TResult Visit(ElementC element);
}

// Element interface
public interface IVisitable
{
    TResult Accept<TResult>(IVisitor<TResult> visitor);
}

// Generic visitor base class
public abstract class Visitor<TResult> : IVisitor<TResult>
{
    public abstract TResult Visit(ElementA element);
    public abstract TResult Visit(ElementB element);
    public abstract TResult Visit(ElementC element);
    
    protected virtual TResult DefaultVisit(IVisitable element)
    {
        return default(TResult)!;
    }
}

// Concrete elements
public class ElementA : IVisitable
{
    public string DataA { get; set; } = "";
    public int ValueA { get; set; }
    
    public TResult Accept<TResult>(IVisitor<TResult> visitor)
    {
        return visitor.Visit(this);
    }
    
    public override string ToString() => $"ElementA(DataA='{DataA}', ValueA={ValueA})";
}

public class ElementB : IVisitable
{
    public double DataB { get; set; }
    public bool FlagB { get; set; }
    
    public TResult Accept<TResult>(IVisitor<TResult> visitor)
    {
        return visitor.Visit(this);
    }
    
    public override string ToString() => $"ElementB(DataB={DataB}, FlagB={FlagB})";
}

public class ElementC : IVisitable
{
    public List<string> DataC { get; set; } = new();
    public DateTime TimeC { get; set; }
    
    public TResult Accept<TResult>(IVisitor<TResult> visitor)
    {
        return visitor.Visit(this);
    }
    
    public override string ToString() => $"ElementC(DataC=[{string.Join(", ", DataC)}], TimeC={TimeC:yyyy-MM-dd})";
}
```

## 2. Document Processing Example

```csharp
// Document element interfaces
public interface IDocumentElement : IVisitable
{
    string Id { get; }
    string Content { get; set; }
}

public interface IDocumentVisitor<TResult> : IVisitor<TResult>
{
    TResult Visit(Paragraph paragraph);
    TResult Visit(Header header);
    TResult Visit(Image image);
    TResult Visit(Table table);
    TResult Visit(List list);
}

// Abstract document element
public abstract class DocumentElement : IDocumentElement
{
    public string Id { get; } = Guid.NewGuid().ToString("N")[..8];
    public string Content { get; set; } = "";
    
    public abstract TResult Accept<TResult>(IVisitor<TResult> visitor);
    
    protected TResult AcceptDocumentVisitor<TResult>(IDocumentVisitor<TResult> visitor)
    {
        return this switch
        {
            Paragraph p => visitor.Visit(p),
            Header h => visitor.Visit(h),
            Image img => visitor.Visit(img),
            Table t => visitor.Visit(t),
            List l => visitor.Visit(l),
            _ => throw new NotSupportedException($"Element type {GetType().Name} not supported")
        };
    }
}

// Concrete document elements
public class Paragraph : DocumentElement
{
    public string FontFamily { get; set; } = "Arial";
    public int FontSize { get; set; } = 12;
    public bool IsJustified { get; set; } = false;
    
    public override TResult Accept<TResult>(IVisitor<TResult> visitor)
    {
        if (visitor is IDocumentVisitor<TResult> docVisitor)
        {
            return docVisitor.Visit(this);
        }
        throw new ArgumentException("Visitor must implement IDocumentVisitor<TResult>");
    }
    
    public override string ToString() => $"Paragraph({Content[..Math.Min(30, Content.Length)]}{(Content.Length > 30 ? "..." : "")})";
}

public class Header : DocumentElement
{
    public int Level { get; set; } = 1; // H1, H2, H3, etc.
    public string Anchor { get; set; } = "";
    
    public override TResult Accept<TResult>(IVisitor<TResult> visitor)
    {
        if (visitor is IDocumentVisitor<TResult> docVisitor)
        {
            return docVisitor.Visit(this);
        }
        throw new ArgumentException("Visitor must implement IDocumentVisitor<TResult>");
    }
    
    public override string ToString() => $"Header(Level={Level}, Content='{Content}')";
}

public class Image : DocumentElement
{
    public string Source { get; set; } = "";
    public string AltText { get; set; } = "";
    public int Width { get; set; } = 0;
    public int Height { get; set; } = 0;
    
    public override TResult Accept<TResult>(IVisitor<TResult> visitor)
    {
        if (visitor is IDocumentVisitor<TResult> docVisitor)
        {
            return docVisitor.Visit(this);
        }
        throw new ArgumentException("Visitor must implement IDocumentVisitor<TResult>");
    }
    
    public override string ToString() => $"Image(Source='{Source}', {Width}x{Height})";
}

public class Table : DocumentElement
{
    public int Rows { get; set; }
    public int Columns { get; set; }
    public List<List<string>> Data { get; set; } = new();
    public List<string> Headers { get; set; } = new();
    
    public override TResult Accept<TResult>(IVisitor<TResult> visitor)
    {
        if (visitor is IDocumentVisitor<TResult> docVisitor)
        {
            return docVisitor.Visit(this);
        }
        throw new ArgumentException("Visitor must implement IDocumentVisitor<TResult>");
    }
    
    public override string ToString() => $"Table({Rows}x{Columns} with {Data.Count} data rows)";
}

public class List : DocumentElement
{
    public ListType Type { get; set; } = ListType.Unordered;
    public List<string> Items { get; set; } = new();
    public int IndentLevel { get; set; } = 0;
    
    public override TResult Accept<TResult>(IVisitor<TResult> visitor)
    {
        if (visitor is IDocumentVisitor<TResult> docVisitor)
        {
            return docVisitor.Visit(this);
        }
        throw new ArgumentException("Visitor must implement IDocumentVisitor<TResult>");
    }
    
    public override string ToString() => $"List({Type}, {Items.Count} items, indent={IndentLevel})";
}

public enum ListType
{
    Ordered,
    Unordered,
    Definition
}

// Document container
public class Document
{
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public List<IDocumentElement> Elements { get; set; } = new();
    
    public void AddElement(IDocumentElement element)
    {
        Elements.Add(element);
    }
    
    public TResult Accept<TResult>(IDocumentVisitor<TResult> visitor)
    {
        var results = Elements.Select(element => element.Accept(visitor)).ToList();
        
        // Combine results if visitor supports it
        if (visitor is ICombiningVisitor<TResult> combiningVisitor)
        {
            return combiningVisitor.CombineResults(results);
        }
        
        return results.LastOrDefault() ?? default(TResult)!;
    }
    
    public override string ToString() => $"Document('{Title}', {Elements.Count} elements)";
}

// Combining visitor interface for document-level operations
public interface ICombiningVisitor<TResult>
{
    TResult CombineResults(List<TResult> results);
}

// Concrete visitors
public class HtmlExportVisitor : IDocumentVisitor<string>, ICombiningVisitor<string>
{
    private readonly StringBuilder html = new();
    private readonly HtmlExportOptions options;
    
    public HtmlExportVisitor(HtmlExportOptions? options = null)
    {
        options = options ?? new HtmlExportOptions();
    }
    
    public string Visit(Paragraph paragraph)
    {
        var style = options.IncludeInlineStyles 
            ? $" style=\"font-family: {paragraph.FontFamily}; font-size: {paragraph.FontSize}px;{(paragraph.IsJustified ? " text-align: justify;" : "")}\"" 
            : "";
        
        return $"<p{style}>{EscapeHtml(paragraph.Content)}</p>";
    }
    
    public string Visit(Header header)
    {
        var anchor = !string.IsNullOrEmpty(header.Anchor) ? $" id=\"{header.Anchor}\"" : "";
        return $"<h{header.Level}{anchor}>{EscapeHtml(header.Content)}</h{header.Level}>";
    }
    
    public string Visit(Image image)
    {
        var alt = !string.IsNullOrEmpty(image.AltText) ? $" alt=\"{EscapeHtml(image.AltText)}\"" : "";
        var dimensions = image.Width > 0 && image.Height > 0 
            ? $" width=\"{image.Width}\" height=\"{image.Height}\"" 
            : "";
        
        return $"<img src=\"{EscapeHtml(image.Source)}\"{alt}{dimensions} />";
    }
    
    public string Visit(Table table)
    {
        var html = new StringBuilder("<table>");
        
        if (table.Headers.Any())
        {
            html.Append("<thead><tr>");
            foreach (var header in table.Headers)
            {
                html.Append($"<th>{EscapeHtml(header)}</th>");
            }
            html.Append("</tr></thead>");
        }
        
        html.Append("<tbody>");
        foreach (var row in table.Data)
        {
            html.Append("<tr>");
            foreach (var cell in row)
            {
                html.Append($"<td>{EscapeHtml(cell)}</td>");
            }
            html.Append("</tr>");
        }
        html.Append("</tbody></table>");
        
        return html.ToString();
    }
    
    public string Visit(List list)
    {
        var tag = list.Type == ListType.Ordered ? "ol" : "ul";
        var indent = new string(' ', list.IndentLevel * 4);
        
        var html = new StringBuilder($"{indent}<{tag}>\n");
        foreach (var item in list.Items)
        {
            html.AppendLine($"{indent}  <li>{EscapeHtml(item)}</li>");
        }
        html.Append($"{indent}</{tag}>");
        
        return html.ToString();
    }
    
    public string CombineResults(List<string> results)
    {
        return string.Join("\n", results);
    }
    
    private string EscapeHtml(string text)
    {
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#39;");
    }
}

public class MarkdownExportVisitor : IDocumentVisitor<string>, ICombiningVisitor<string>
{
    public string Visit(Paragraph paragraph)
    {
        return paragraph.Content + "\n";
    }
    
    public string Visit(Header header)
    {
        var prefix = new string('#', header.Level);
        return $"{prefix} {header.Content}\n";
    }
    
    public string Visit(Image image)
    {
        var alt = !string.IsNullOrEmpty(image.AltText) ? image.AltText : "Image";
        return $"![{alt}]({image.Source})\n";
    }
    
    public string Visit(Table table)
    {
        var md = new StringBuilder();
        
        if (table.Headers.Any())
        {
            md.AppendLine("| " + string.Join(" | ", table.Headers) + " |");
            md.AppendLine("| " + string.Join(" | ", table.Headers.Select(_ => "---")) + " |");
        }
        
        foreach (var row in table.Data)
        {
            md.AppendLine("| " + string.Join(" | ", row) + " |");
        }
        
        return md.ToString();
    }
    
    public string Visit(List list)
    {
        var md = new StringBuilder();
        var indent = new string(' ', list.IndentLevel * 2);
        var prefix = list.Type == ListType.Ordered ? "1." : "*";
        
        for (int i = 0; i < list.Items.Count; i++)
        {
            var itemPrefix = list.Type == ListType.Ordered ? $"{i + 1}." : "*";
            md.AppendLine($"{indent}{itemPrefix} {list.Items[i]}");
        }
        
        return md.ToString();
    }
    
    public string CombineResults(List<string> results)
    {
        return string.Join("\n", results);
    }
}

public class WordCountVisitor : IDocumentVisitor<int>, ICombiningVisitor<int>
{
    public int Visit(Paragraph paragraph)
    {
        return CountWords(paragraph.Content);
    }
    
    public int Visit(Header header)
    {
        return CountWords(header.Content);
    }
    
    public int Visit(Image image)
    {
        return CountWords(image.AltText);
    }
    
    public int Visit(Table table)
    {
        var count = table.Headers.Sum(h => CountWords(h));
        count += table.Data.SelectMany(row => row).Sum(cell => CountWords(cell));
        return count;
    }
    
    public int Visit(List list)
    {
        return list.Items.Sum(item => CountWords(item));
    }
    
    public int CombineResults(List<int> results)
    {
        return results.Sum();
    }
    
    private int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;
        
        return text.Split(new char[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
    }
}

public class ValidationVisitor : IDocumentVisitor<ValidationResult>, ICombiningVisitor<ValidationResult>
{
    private readonly ValidationOptions options;
    
    public ValidationVisitor(ValidationOptions? options = null)
    {
        options = options ?? new ValidationOptions();
    }
    
    public ValidationResult Visit(Paragraph paragraph)
    {
        var result = new ValidationResult { ElementType = "Paragraph", ElementId = paragraph.Id };
        
        if (string.IsNullOrWhiteSpace(paragraph.Content))
        {
            result.AddError("Paragraph content cannot be empty");
        }
        
        if (paragraph.Content.Length > options.MaxParagraphLength)
        {
            result.AddWarning($"Paragraph exceeds maximum length of {options.MaxParagraphLength} characters");
        }
        
        return result;
    }
    
    public ValidationResult Visit(Header header)
    {
        var result = new ValidationResult { ElementType = "Header", ElementId = header.Id };
        
        if (string.IsNullOrWhiteSpace(header.Content))
        {
            result.AddError("Header content cannot be empty");
        }
        
        if (header.Level < 1 || header.Level > 6)
        {
            result.AddError($"Header level must be between 1 and 6, got {header.Level}");
        }
        
        if (header.Content.Length > options.MaxHeaderLength)
        {
            result.AddWarning($"Header exceeds maximum length of {options.MaxHeaderLength} characters");
        }
        
        return result;
    }
    
    public ValidationResult Visit(Image image)
    {
        var result = new ValidationResult { ElementType = "Image", ElementId = image.Id };
        
        if (string.IsNullOrWhiteSpace(image.Source))
        {
            result.AddError("Image source cannot be empty");
        }
        
        if (string.IsNullOrWhiteSpace(image.AltText))
        {
            result.AddWarning("Image should have alt text for accessibility");
        }
        
        if (image.Width <= 0 || image.Height <= 0)
        {
            result.AddWarning("Image dimensions should be specified");
        }
        
        return result;
    }
    
    public ValidationResult Visit(Table table)
    {
        var result = new ValidationResult { ElementType = "Table", ElementId = table.Id };
        
        if (table.Rows <= 0 || table.Columns <= 0)
        {
            result.AddError("Table must have positive number of rows and columns");
        }
        
        if (table.Data.Any(row => row.Count != table.Columns))
        {
            result.AddError("All table rows must have the same number of columns");
        }
        
        if (table.Headers.Any() && table.Headers.Count != table.Columns)
        {
            result.AddError("Number of headers must match number of columns");
        }
        
        return result;
    }
    
    public ValidationResult Visit(List list)
    {
        var result = new ValidationResult { ElementType = "List", ElementId = list.Id };
        
        if (!list.Items.Any())
        {
            result.AddWarning("List has no items");
        }
        
        if (list.Items.Any(string.IsNullOrWhiteSpace))
        {
            result.AddError("List items cannot be empty");
        }
        
        if (list.IndentLevel < 0)
        {
            result.AddError("List indent level cannot be negative");
        }
        
        return result;
    }
    
    public ValidationResult CombineResults(List<ValidationResult> results)
    {
        var combined = new ValidationResult { ElementType = "Document", ElementId = "document" };
        
        foreach (var result in results)
        {
            combined.Errors.AddRange(result.Errors);
            combined.Warnings.AddRange(result.Warnings);
        }
        
        return combined;
    }
}

// Supporting classes
public class HtmlExportOptions
{
    public bool IncludeInlineStyles { get; set; } = true;
    public bool PrettyPrint { get; set; } = false;
}

public class ValidationOptions
{
    public int MaxParagraphLength { get; set; } = 5000;
    public int MaxHeaderLength { get; set; } = 200;
    public bool RequireAltText { get; set; } = true;
}

public class ValidationResult
{
    public string ElementType { get; set; } = "";
    public string ElementId { get; set; } = "";
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    
    public bool IsValid => !Errors.Any();
    public bool HasWarnings => Warnings.Any();
    
    public void AddError(string error) => Errors.Add($"[{ElementType} {ElementId}] {error}");
    public void AddWarning(string warning) => Warnings.Add($"[{ElementType} {ElementId}] {warning}");
    
    public override string ToString()
    {
        var result = $"{ElementType} {ElementId}: ";
        if (IsValid && !HasWarnings)
        {
            result += "OK";
        }
        else
        {
            result += $"{Errors.Count} errors, {Warnings.Count} warnings";
        }
        return result;
    }
}
```

## 3. Abstract Syntax Tree Example

```csharp
// AST Node interfaces
public interface IAstNode : IVisitable
{
    string NodeType { get; }
    List<IAstNode> Children { get; }
}

public interface IAstVisitor<TResult> : IVisitor<TResult>
{
    TResult Visit(LiteralNode node);
    TResult Visit(VariableNode node);
    TResult Visit(BinaryOperationNode node);
    TResult Visit(UnaryOperationNode node);
    TResult Visit(FunctionCallNode node);
    TResult Visit(AssignmentNode node);
    TResult Visit(BlockNode node);
}

// Abstract AST Node
public abstract class AstNode : IAstNode
{
    public abstract string NodeType { get; }
    public List<IAstNode> Children { get; } = new();
    public Dictionary<string, object> Attributes { get; } = new();
    
    public abstract TResult Accept<TResult>(IVisitor<TResult> visitor);
    
    protected void AddChild(IAstNode child)
    {
        Children.Add(child);
    }
}

// Concrete AST nodes
public class LiteralNode : AstNode
{
    public override string NodeType => "Literal";
    public object Value { get; set; } = null!;
    public Type ValueType { get; set; } = typeof(object);
    
    public LiteralNode(object value)
    {
        Value = value;
        ValueType = value?.GetType() ?? typeof(object);
    }
    
    public override TResult Accept<TResult>(IVisitor<TResult> visitor)
    {
        if (visitor is IAstVisitor<TResult> astVisitor)
        {
            return astVisitor.Visit(this);
        }
        throw new ArgumentException("Visitor must implement IAstVisitor<TResult>");
    }
    
    public override string ToString() => $"Literal({Value})";
}

public class VariableNode : AstNode
{
    public override string NodeType => "Variable";
    public string Name { get; set; } = "";
    
    public VariableNode(string name)
    {
        Name = name;
    }
    
    public override TResult Accept<TResult>(IVisitor<TResult> visitor)
    {
        if (visitor is IAstVisitor<TResult> astVisitor)
        {
            return astVisitor.Visit(this);
        }
        throw new ArgumentException("Visitor must implement IAstVisitor<TResult>");
    }
    
    public override string ToString() => $"Variable({Name})";
}

public class BinaryOperationNode : AstNode
{
    public override string NodeType => "BinaryOperation";
    public string Operator { get; set; } = "";
    public IAstNode Left { get; set; } = null!;
    public IAstNode Right { get; set; } = null!;
    
    public BinaryOperationNode(string op, IAstNode left, IAstNode right)
    {
        Operator = op;
        Left = left;
        Right = right;
        AddChild(left);
        AddChild(right);
    }
    
    public override TResult Accept<TResult>(IVisitor<TResult> visitor)
    {
        if (visitor is IAstVisitor<TResult> astVisitor)
        {
            return astVisitor.Visit(this);
        }
        throw new ArgumentException("Visitor must implement IAstVisitor<TResult>");
    }
    
    public override string ToString() => $"BinaryOp({Operator})";
}

public class UnaryOperationNode : AstNode
{
    public override string NodeType => "UnaryOperation";
    public string Operator { get; set; } = "";
    public IAstNode Operand { get; set; } = null!;
    
    public UnaryOperationNode(string op, IAstNode operand)
    {
        Operator = op;
        Operand = operand;
        AddChild(operand);
    }
    
    public override TResult Accept<TResult>(IVisitor<TResult> visitor)
    {
        if (visitor is IAstVisitor<TResult> astVisitor)
        {
            return astVisitor.Visit(this);
        }
        throw new ArgumentException("Visitor must implement IAstVisitor<TResult>");
    }
    
    public override string ToString() => $"UnaryOp({Operator})";
}

public class FunctionCallNode : AstNode
{
    public override string NodeType => "FunctionCall";
    public string FunctionName { get; set; } = "";
    public List<IAstNode> Arguments { get; set; } = new();
    
    public FunctionCallNode(string name, params IAstNode[] args)
    {
        FunctionName = name;
        Arguments.AddRange(args);
        foreach (var arg in args)
        {
            AddChild(arg);
        }
    }
    
    public override TResult Accept<TResult>(IVisitor<TResult> visitor)
    {
        if (visitor is IAstVisitor<TResult> astVisitor)
        {
            return astVisitor.Visit(this);
        }
        throw new ArgumentException("Visitor must implement IAstVisitor<TResult>");
    }
    
    public override string ToString() => $"FunctionCall({FunctionName}, {Arguments.Count} args)";
}

public class AssignmentNode : AstNode
{
    public override string NodeType => "Assignment";
    public string VariableName { get; set; } = "";
    public IAstNode Value { get; set; } = null!;
    
    public AssignmentNode(string variable, IAstNode value)
    {
        VariableName = variable;
        Value = value;
        AddChild(value);
    }
    
    public override TResult Accept<TResult>(IVisitor<TResult> visitor)
    {
        if (visitor is IAstVisitor<TResult> astVisitor)
        {
            return astVisitor.Visit(this);
        }
        throw new ArgumentException("Visitor must implement IAstVisitor<TResult>");
    }
    
    public override string ToString() => $"Assignment({VariableName} = ...)";
}

public class BlockNode : AstNode
{
    public override string NodeType => "Block";
    public List<IAstNode> Statements { get; set; } = new();
    
    public BlockNode(params IAstNode[] statements)
    {
        Statements.AddRange(statements);
        foreach (var stmt in statements)
        {
            AddChild(stmt);
        }
    }
    
    public void AddStatement(IAstNode statement)
    {
        Statements.Add(statement);
        AddChild(statement);
    }
    
    public override TResult Accept<TResult>(IVisitor<TResult> visitor)
    {
        if (visitor is IAstVisitor<TResult> astVisitor)
        {
            return astVisitor.Visit(this);
        }
        throw new ArgumentException("Visitor must implement IAstVisitor<TResult>");
    }
    
    public override string ToString() => $"Block({Statements.Count} statements)";
}

// AST Visitors
public class EvaluationVisitor : IAstVisitor<object>
{
    private readonly Dictionary<string, object> variables = new();
    private readonly Dictionary<string, Func<object[], object>> functions = new();
    
    public EvaluationVisitor()
    {
        // Built-in functions
        _functions["sin"] = args => Math.Sin(Convert.ToDouble(args[0]));
        _functions["cos"] = args => Math.Cos(Convert.ToDouble(args[0]));
        _functions["sqrt"] = args => Math.Sqrt(Convert.ToDouble(args[0]));
        _functions["abs"] = args => Math.Abs(Convert.ToDouble(args[0]));
        _functions["max"] = args => Math.Max(Convert.ToDouble(args[0]), Convert.ToDouble(args[1]));
        _functions["min"] = args => Math.Min(Convert.ToDouble(args[0]), Convert.ToDouble(args[1]));
    }
    
    public void SetVariable(string name, object value)
    {
        _variables[name] = value;
    }
    
    public void SetFunction(string name, Func<object[], object> function)
    {
        _functions[name] = function;
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
        _variables[node.VariableName] = value;
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

public class CodeGenerationVisitor : IAstVisitor<string>
{
    private int indentLevel = 0;
    private readonly string indentString = "  ";
    
    public string Visit(LiteralNode node)
    {
        return node.Value switch
        {
            string s => $"\"{s.Replace("\"", "\\\"")}\"",
            bool b => b.ToString().ToLower(),
            _ => node.Value.ToString()!
        };
    }
    
    public string Visit(VariableNode node)
    {
        return node.Name;
    }
    
    public string Visit(BinaryOperationNode node)
    {
        var left = node.Left.Accept(this);
        var right = node.Right.Accept(this);
        return $"({left} {node.Operator} {right})";
    }
    
    public string Visit(UnaryOperationNode node)
    {
        var operand = node.Operand.Accept(this);
        return $"{node.Operator}{operand}";
    }
    
    public string Visit(FunctionCallNode node)
    {
        var args = string.Join(", ", node.Arguments.Select(arg => arg.Accept(this)));
        return $"{node.FunctionName}({args})";
    }
    
    public string Visit(AssignmentNode node)
    {
        var value = node.Value.Accept(this);
        return $"{GetIndent()}{node.VariableName} = {value};";
    }
    
    public string Visit(BlockNode node)
    {
        var code = new StringBuilder();
        code.AppendLine($"{GetIndent()}{{");
        
        _indentLevel++;
        foreach (var statement in node.Statements)
        {
            code.AppendLine(statement.Accept(this));
        }
        _indentLevel--;
        
        code.Append($"{GetIndent()}}}");
        return code.ToString();
    }
    
    private string GetIndent()
    {
        return new string(' ', _indentLevel * indentString.Length);
    }
}

public class AstAnalysisVisitor : IAstVisitor<AnalysisResult>
{
    private readonly AnalysisResult result = new();
    
    public AnalysisResult GetResult() => result;
    
    public AnalysisResult Visit(LiteralNode node)
    {
        result.LiteralCount++;
        result.NodesByType.TryAdd("Literal", 0);
        result.NodesByType["Literal"]++;
        return result;
    }
    
    public AnalysisResult Visit(VariableNode node)
    {
        result.VariableCount++;
        result.Variables.Add(node.Name);
        result.NodesByType.TryAdd("Variable", 0);
        result.NodesByType["Variable"]++;
        return result;
    }
    
    public AnalysisResult Visit(BinaryOperationNode node)
    {
        result.OperationCount++;
        result.Operators.Add(node.Operator);
        result.NodesByType.TryAdd("BinaryOperation", 0);
        result.NodesByType["BinaryOperation"]++;
        
        node.Left.Accept(this);
        node.Right.Accept(this);
        return result;
    }
    
    public AnalysisResult Visit(UnaryOperationNode node)
    {
        result.OperationCount++;
        result.Operators.Add(node.Operator);
        result.NodesByType.TryAdd("UnaryOperation", 0);
        result.NodesByType["UnaryOperation"]++;
        
        node.Operand.Accept(this);
        return result;
    }
    
    public AnalysisResult Visit(FunctionCallNode node)
    {
        result.FunctionCallCount++;
        result.Functions.Add(node.FunctionName);
        result.NodesByType.TryAdd("FunctionCall", 0);
        result.NodesByType["FunctionCall"]++;
        
        foreach (var arg in node.Arguments)
        {
            arg.Accept(this);
        }
        return result;
    }
    
    public AnalysisResult Visit(AssignmentNode node)
    {
        result.AssignmentCount++;
        result.Variables.Add(node.VariableName);
        result.NodesByType.TryAdd("Assignment", 0);
        result.NodesByType["Assignment"]++;
        
        node.Value.Accept(this);
        return result;
    }
    
    public AnalysisResult Visit(BlockNode node)
    {
        result.BlockCount++;
        result.NodesByType.TryAdd("Block", 0);
        result.NodesByType["Block"]++;
        
        foreach (var statement in node.Statements)
        {
            statement.Accept(this);
        }
        return result;
    }
}

// Analysis result class
public class AnalysisResult
{
    public int LiteralCount { get; set; }
    public int VariableCount { get; set; }
    public int OperationCount { get; set; }
    public int FunctionCallCount { get; set; }
    public int AssignmentCount { get; set; }
    public int BlockCount { get; set; }
    
    public HashSet<string> Variables { get; set; } = new();
    public List<string> Operators { get; set; } = new();
    public HashSet<string> Functions { get; set; } = new();
    public Dictionary<string, int> NodesByType { get; set; } = new();
    
    public int TotalNodes => NodesByType.Values.Sum();
    
    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine("AST Analysis Results:");
        sb.AppendLine($"Total Nodes: {TotalNodes}");
        sb.AppendLine($"Literals: {LiteralCount}");
        sb.AppendLine($"Variables: {VariableCount} (unique: {Variables.Count})");
        sb.AppendLine($"Operations: {OperationCount}");
        sb.AppendLine($"Function Calls: {FunctionCallCount} (unique: {Functions.Count})");
        sb.AppendLine($"Assignments: {AssignmentCount}");
        sb.AppendLine($"Blocks: {BlockCount}");
        
        if (Variables.Any())
        {
            sb.AppendLine($"Variable Names: {string.Join(", ", Variables)}");
        }
        
        if (Functions.Any())
        {
            sb.AppendLine($"Function Names: {string.Join(", ", Functions)}");
        }
        
        return sb.ToString();
    }
}
```

**Usage**:

```csharp
// 1. Document Processing Example
var document = new Document
{
    Title = "Sample Document",
    Author = "John Doe"
};

document.AddElement(new Header { Level = 1, Content = "Introduction" });
document.AddElement(new Paragraph 
{ 
    Content = "This is a sample document demonstrating the Visitor pattern.",
    FontFamily = "Times New Roman",
    FontSize = 12
});

document.AddElement(new Image 
{ 
    Source = "diagram.png", 
    AltText = "Architecture diagram",
    Width = 400,
    Height = 300
});

var table = new Table 
{ 
    Rows = 2, 
    Columns = 3,
    Headers = new List<string> { "Name", "Age", "City" }
};
table.Data.Add(new List<string> { "Alice", "30", "New York" });
table.Data.Add(new List<string> { "Bob", "25", "Los Angeles" });
document.AddElement(table);

var list = new List 
{ 
    Type = ListType.Unordered,
    Items = new List<string> { "First item", "Second item", "Third item" }
};
document.AddElement(list);

// Export to HTML
var htmlVisitor = new HtmlExportVisitor(new HtmlExportOptions { IncludeInlineStyles = true });
var html = document.Accept(htmlVisitor);
Console.WriteLine("HTML Output:");
Console.WriteLine(html);

// Export to Markdown
var markdownVisitor = new MarkdownExportVisitor();
var markdown = document.Accept(markdownVisitor);
Console.WriteLine("\nMarkdown Output:");
Console.WriteLine(markdown);

// Count words
var wordCountVisitor = new WordCountVisitor();
var wordCount = document.Accept(wordCountVisitor);
Console.WriteLine($"\nTotal word count: {wordCount}");

// Validate document
var validationVisitor = new ValidationVisitor(new ValidationOptions { MaxParagraphLength = 1000 });
var validationResult = document.Accept(validationVisitor);
Console.WriteLine($"\nValidation: {validationResult}");
if (!validationResult.IsValid || validationResult.HasWarnings)
{
    Console.WriteLine("Issues found:");
    foreach (var error in validationResult.Errors)
    {
        Console.WriteLine($"ERROR: {error}");
    }
    foreach (var warning in validationResult.Warnings)
    {
        Console.WriteLine($"WARNING: {warning}");
    }
}

// 2. AST Example
// Build AST: x = 5 + 3 * sqrt(16)
var ast = new BlockNode(
    new AssignmentNode("x", 
        new BinaryOperationNode("+",
            new LiteralNode(5),
            new BinaryOperationNode("*",
                new LiteralNode(3),
                new FunctionCallNode("sqrt", new LiteralNode(16))
            )
        )
    ),
    new AssignmentNode("y",
        new BinaryOperationNode("*",
            new VariableNode("x"),
            new LiteralNode(2)
        )
    )
);

// Evaluate AST
var evaluator = new EvaluationVisitor();
var result = ast.Accept(evaluator);
Console.WriteLine($"\nAST Evaluation Result: {result}");

// Generate code
var codeGen = new CodeGenerationVisitor();
var code = ast.Accept(codeGen);
Console.WriteLine("\nGenerated Code:");
Console.WriteLine(code);

// Analyze AST
var analyzer = new AstAnalysisVisitor();
ast.Accept(analyzer);
var analysis = analyzer.GetResult();
Console.WriteLine($"\nAST Analysis:");
Console.WriteLine(analysis);

// Expected output demonstrates:
// - Double dispatch mechanism working with different element types
// - Multiple visitors operating on same object structure
// - Type-safe operations without instanceof checks
// - New operations added without modifying existing classes
// - Visitor pattern enabling different result types from same traversal
// - Composition of complex operations through visitor chaining
```

**Notes**:

- **Double Dispatch**: Visitor pattern uses double dispatch to determine both visitor and element types at runtime
- **Open/Closed Principle**: Easy to add new operations (visitors) without modifying existing element classes
- **Type Safety**: Compile-time checking ensures all element types are handled by each visitor
- **Separation of Concerns**: Algorithms are separated from data structures they operate on
- **Traversal Logic**: Visitors can implement different traversal strategies for composite structures
- **Result Aggregation**: Visitors can combine results from multiple elements in flexible ways
- **Performance**: Avoid runtime type checking and casting with proper visitor implementation
- **Maintenance**: Adding new element types requires updating all existing visitors

**Prerequisites**:

- .NET 6.0 or later for pattern matching and modern C# features
- Understanding of polymorphism and double dispatch
- Knowledge of the Composite pattern (often used together)
- Familiarity with tree traversal algorithms

**Related Patterns**:

- **Composite**: Visitor often operates on Composite structures
- **Interpreter**: AST nodes visited by different interpreters
- **Strategy**: Visitor can be seen as external strategy for operations
- **Command**: Visitor operations can be encapsulated as commands

