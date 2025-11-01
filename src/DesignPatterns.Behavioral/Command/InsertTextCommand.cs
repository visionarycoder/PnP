namespace Snippets.DesignPatterns.Behavioral.Command;

public class InsertTextCommand(TextEditor editor, string text, int position) : ICommand
{
    public string Description { get; } = $"Insert '{text}' at position {position}";

    public void Execute()
    {
        editor.InsertText(text, position);
    }

    public void Undo()
    {
        editor.DeleteText(position, text.Length);
    }
}