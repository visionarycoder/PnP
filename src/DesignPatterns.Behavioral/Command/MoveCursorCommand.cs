namespace Snippets.DesignPatterns.Behavioral.Command;

public class MoveCursorCommand(TextEditor editor, int newPosition) : ICommand
{
    private int oldPosition;

    public string Description { get; } = $"Move cursor to position {newPosition}";

    public void Execute()
    {
        oldPosition = editor.CursorPosition;
        editor.MoveCursor(newPosition);
    }

    public void Undo()
    {
        editor.MoveCursor(oldPosition);
    }
}