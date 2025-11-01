using System.Text;

namespace Snippets.DesignPatterns.Structural.Composite;

public class File : FileSystemComponent
{
    public File(string name, byte[]? content = null) 
        : base(name)
    {
        Content = content ?? [];
        Extension = Path.GetExtension(name);
    }

    public File(string name, string? textContent) : base(name)
    {
        Content = Encoding.UTF8.GetBytes(textContent ?? string.Empty);
        Extension = Path.GetExtension(name);
    }

    public string Extension { get; }

    public byte[] Content { get; private set; }

    public string TextContent => Encoding.UTF8.GetString(Content);

    public override long GetSize() => Content.Length;

    public override int GetCount() => 1;

    public override void Display(int depth = 0)
    {
        var indent = GetIndentation(depth);
        var sizeStr = GetSize() < 1024 ? $"{GetSize()}B" : $"{GetSize() / 1024.0:F1}KB";
        Console.WriteLine($"{indent}📄 {Name} ({sizeStr}) [{Extension}]");
    }

    public override string GetPath()
    {
        // Files don't know their parent path in this simple implementation
        return Name;
    }

    public void WriteContent(string? content)
    {
        Content = Encoding.UTF8.GetBytes(content ?? string.Empty);
        UpdateModified();
    }

    public void WriteContent(byte[]? content)
    {
        Content = content ?? [];
        UpdateModified();
    }

    public override FileSystemComponent Clone()
    {
        return new File(Name, (byte[])Content.Clone());
    }
}