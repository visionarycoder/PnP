namespace Snippets.DesignPatterns.Behavioral.Command;

public class CommandManager
{
    private readonly Stack<ICommand> undoStack = new();
    private readonly Stack<ICommand> redoStack = new();

    public void ExecuteCommand(ICommand command)
    {
        Console.WriteLine($"Executing: {command.Description}");
        command.Execute();

        undoStack.Push(command);
        redoStack.Clear(); // Clear redo stack when new command is executed
    }

    public bool CanUndo => undoStack.Count > 0;
    public bool CanRedo => redoStack.Count > 0;

    public void Undo()
    {
        if (!CanUndo)
        {
            Console.WriteLine("Nothing to undo");
            return;
        }

        var command = undoStack.Pop();
        Console.WriteLine($"Undoing: {command.Description}");
        command.Undo();

        redoStack.Push(command);
    }

    public void Redo()
    {
        if (!CanRedo)
        {
            Console.WriteLine("Nothing to redo");
            return;
        }

        var command = redoStack.Pop();
        Console.WriteLine($"Redoing: {command.Description}");
        command.Execute();

        undoStack.Push(command);
    }

    public void ClearHistory()
    {
        undoStack.Clear();
        redoStack.Clear();
        Console.WriteLine("Command history cleared");
    }

    public void ShowHistory()
    {
        Console.WriteLine("\n📋 Command History:");
        Console.WriteLine($"  Undo Stack ({undoStack.Count} commands):");

        var undoArray = undoStack.ToArray();
        for (int i = 0; i < undoArray.Length; i++)
        {
            Console.WriteLine($"    {i + 1}. {undoArray[i].Description}");
        }

        Console.WriteLine($"  Redo Stack ({redoStack.Count} commands):");
        var redoArray = redoStack.ToArray();
        for (int i = 0; i < redoArray.Length; i++)
        {
            Console.WriteLine($"    {i + 1}. {redoArray[i].Description}");
        }
    }
}