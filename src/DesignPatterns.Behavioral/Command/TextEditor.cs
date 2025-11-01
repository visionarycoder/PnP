using System.Text;

namespace Snippets.DesignPatterns.Behavioral.Command;

// Command interface

// Receiver - performs the actual work
public class TextEditor
{
    private readonly StringBuilder content = new();

    public string Content => content.ToString();
    public int CursorPosition { get; private set; } = 0;

    public void InsertText(string text, int position)
    {
        if (position < 0 || position > content.Length)
            throw new ArgumentOutOfRangeException(nameof(position));

        content.Insert(position, text);
        CursorPosition = position + text.Length;
        Console.WriteLine($"Inserted '{text}' at position {position}");
    }

    public string DeleteText(int startPosition, int length)
    {
        if (startPosition < 0 || startPosition + length > content.Length)
            throw new ArgumentOutOfRangeException();

        var deletedText = content.ToString(startPosition, length);
        content.Remove(startPosition, length);
        CursorPosition = startPosition;
        Console.WriteLine($"Deleted '{deletedText}' from position {startPosition}");
        return deletedText;
    }

    public void MoveCursor(int newPosition)
    {
        if (newPosition < 0 || newPosition > content.Length)
            throw new ArgumentOutOfRangeException(nameof(newPosition));

        var oldPosition = CursorPosition;
        CursorPosition = newPosition;
        Console.WriteLine($"Moved cursor from {oldPosition} to {newPosition}");
    }

    public void Clear()
    {
        content.Clear();
        CursorPosition = 0;
    }

    public override string ToString() => $"Content: '{Content}', Cursor: {CursorPosition}";
}

// Concrete Commands

// Macro Command - executes multiple commands

// Invoker - manages command execution and undo/redo

// Advanced Command Pattern: Remote Control Example

// Device Commands

// Remote Control (Invoker)