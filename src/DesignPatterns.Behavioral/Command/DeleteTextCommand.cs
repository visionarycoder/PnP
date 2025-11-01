namespace Snippets.DesignPatterns.Behavioral.Command;

public class DeleteTextCommand(TextEditor editor, int startPosition, int length) : ICommand
{
    private string deletedText = "";

    public string Description { get; } = $"Delete {length} characters from position {startPosition}";

    public void Execute()
    {
        deletedText = editor.DeleteText(startPosition, length);
    }

    public void Undo()
    {
        editor.InsertText(deletedText, startPosition);
    }
}