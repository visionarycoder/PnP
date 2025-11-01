using System.Text;

namespace Snippets.DesignPatterns.Behavioral.Memento;

/// <summary>
/// Text editor that can create and restore from mementos
/// </summary>
public class MementoTextEditor : IOriginator<MementoTextEditorMemento>
{
    private StringBuilder content = new();

    public string Content => content.ToString();
    public int CursorPosition { get; private set; } = 0;

    public string FontName { get; private set; } = "Arial";

    public int FontSize { get; private set; } = 12;

    public bool IsBold { get; private set; } = false;

    public bool IsItalic { get; private set; } = false;

    public void InsertText(string text)
    {
        content.Insert(CursorPosition, text);
        CursorPosition += text.Length;
        Console.WriteLine($"Inserted '{text}' at position {CursorPosition - text.Length}");
    }

    public void DeleteText(int length)
    {
        if (CursorPosition >= length)
        {
            var deletedText = content.ToString(CursorPosition - length, length);
            content.Remove(CursorPosition - length, length);
            CursorPosition -= length;
            Console.WriteLine($"Deleted '{deletedText}'");
        }
    }

    public void MoveCursor(int position)
    {
        if (position >= 0 && position <= content.Length)
        {
            CursorPosition = position;
            Console.WriteLine($"Cursor moved to position {position}");
        }
    }

    public void SetFont(string fontName, int fontSize)
    {
        FontName = fontName;
        FontSize = fontSize;
        Console.WriteLine($"Font changed to {fontName} {fontSize}pt");
    }

    public void SetBold(bool isBold)
    {
        IsBold = isBold;
        Console.WriteLine($"Bold: {(isBold ? "ON" : "OFF")}");
    }

    public void SetItalic(bool isItalic)
    {
        IsItalic = isItalic;
        Console.WriteLine($"Italic: {(isItalic ? "ON" : "OFF")}");
    }

    public MementoTextEditorMemento CreateMemento()
    {
        var description = $"State at {DateTime.Now:HH:mm:ss} - {Content.Length} chars";
        return new MementoTextEditorMemento(
            Content,
            CursorPosition,
            FontName,
            FontSize,
            IsBold,
            IsItalic,
            description);
    }

    public void RestoreFromMemento(MementoTextEditorMemento memento)
    {
        content = new StringBuilder(memento.Content);
        CursorPosition = memento.CursorPosition;
        FontName = memento.FontName;
        FontSize = memento.FontSize;
        IsBold = memento.IsBold;
        IsItalic = memento.IsItalic;

        Console.WriteLine($"Restored state from {memento.CreatedAt:HH:mm:ss}");
        Console.WriteLine($"Content: '{Content}', Cursor: {CursorPosition}");
    }

    public void PrintStatus()
    {
        Console.WriteLine($"Content: '{Content}'");
        Console.WriteLine($"Cursor: {CursorPosition}, Font: {FontName} {FontSize}pt");
        Console.WriteLine($"Bold: {IsBold}, Italic: {IsItalic}");
    }
}