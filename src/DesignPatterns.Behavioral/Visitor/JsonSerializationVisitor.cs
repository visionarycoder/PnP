using System.Text.Json;

namespace Snippets.DesignPatterns.Behavioral.Visitor;

/// <summary>
/// Visitor for serializing objects to JSON
/// </summary>
public class JsonSerializationVisitor(SerializationOptions? options = null)
    : IDocumentVisitor<string>, IAstVisitor<string>
{
    private readonly SerializationOptions options = options ?? new SerializationOptions();

    // Document element serialization
    public string Visit(Paragraph paragraph)
    {
        var obj = new
        {
            type = "paragraph",
            id = paragraph.Id,
            content = paragraph.Content,
            fontFamily = paragraph.FontFamily,
            fontSize = paragraph.FontSize,
            isJustified = paragraph.IsJustified
        };
        return SerializeObject(obj);
    }

    public string Visit(Header header)
    {
        var obj = new
        {
            type = "header",
            id = header.Id,
            content = header.Content,
            level = header.Level,
            anchor = header.Anchor
        };
        return SerializeObject(obj);
    }

    public string Visit(Image image)
    {
        var obj = new
        {
            type = "image",
            id = image.Id,
            source = image.Source,
            altText = image.AltText,
            width = image.Width,
            height = image.Height
        };
        return SerializeObject(obj);
    }

    public string Visit(Table table)
    {
        var obj = new
        {
            type = "table",
            id = table.Id,
            rows = table.Rows,
            columns = table.Columns,
            headers = table.Headers,
            data = table.Data
        };
        return SerializeObject(obj);
    }

    public string Visit(DocumentList list)
    {
        var obj = new
        {
            type = "list",
            id = list.Id,
            listType = list.Type.ToString().ToLower(),
            items = list.Items,
            indentLevel = list.IndentLevel
        };
        return SerializeObject(obj);
    }

    // AST node serialization
    public string Visit(LiteralNode node)
    {
        var obj = new
        {
            type = "literal",
            value = node.Value,
            valueType = node.ValueType.Name
        };
        return SerializeObject(obj);
    }

    public string Visit(VariableNode node)
    {
        var obj = new
        {
            type = "variable",
            name = node.Name
        };
        return SerializeObject(obj);
    }

    public string Visit(BinaryOperationNode node)
    {
        var obj = new
        {
            type = "binaryOperation",
            @operator = node.Operator,
            left = node.Left.Accept(this),
            right = node.Right.Accept(this)
        };
        return SerializeObject(obj);
    }

    public string Visit(UnaryOperationNode node)
    {
        var obj = new
        {
            type = "unaryOperation",
            @operator = node.Operator,
            operand = node.Operand.Accept(this)
        };
        return SerializeObject(obj);
    }

    public string Visit(FunctionCallNode node)
    {
        var obj = new
        {
            type = "functionCall",
            functionName = node.FunctionName,
            arguments = node.Arguments.Select(arg => arg.Accept(this)).ToArray()
        };
        return SerializeObject(obj);
    }

    public string Visit(AssignmentNode node)
    {
        var obj = new
        {
            type = "assignment",
            variableName = node.VariableName,
            value = node.Value.Accept(this)
        };
        return SerializeObject(obj);
    }

    public string Visit(BlockNode node)
    {
        var obj = new
        {
            type = "block",
            statements = node.Statements.Select(stmt => stmt.Accept(this)).ToArray()
        };
        return SerializeObject(obj);
    }

    public string Visit(ElementA element) => throw new NotSupportedException();
    public string Visit(ElementB element) => throw new NotSupportedException();
    public string Visit(ElementC element) => throw new NotSupportedException();

    private string SerializeObject(object obj)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = this.options.PrettyPrint,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        return JsonSerializer.Serialize(obj, options);
    }
}