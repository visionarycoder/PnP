namespace Snippets.DesignPatterns.Behavioral.Command;

public class UniversalRemote
{
    private readonly Dictionary<string, ICommand> commands = new();
    private readonly Stack<ICommand> lastCommands = new();

    public void SetCommand(string slot, ICommand command)
    {
        commands[slot] = command;
        Console.WriteLine($"Command '{command.Description}' assigned to slot '{slot}'");
    }

    public void PressButton(string slot)
    {
        if (commands.TryGetValue(slot, out var command))
        {
            command.Execute();
            lastCommands.Push(command);
        }
        else
        {
            Console.WriteLine($"No command assigned to slot '{slot}'");
        }
    }

    public void PressUndo()
    {
        if (lastCommands.Count > 0)
        {
            var lastCommand = lastCommands.Pop();
            lastCommand.Undo();
        }
        else
        {
            Console.WriteLine("No command to undo");
        }
    }

    public void ShowConfiguration()
    {
        Console.WriteLine("\n🎮 Remote Control Configuration:");
        foreach (var kvp in commands)
        {
            Console.WriteLine($"  {kvp.Key}: {kvp.Value.Description}");
        }
    }
}