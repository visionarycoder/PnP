namespace Snippets.DesignPatterns.Behavioral.Command;

public class MacroCommand(string description) : ICommand
{
    private readonly List<ICommand> commands = [];

    public string Description { get; } = description;

    public void AddCommand(ICommand command)
    {
        commands.Add(command);
    }

    public void Execute()
    {
        Console.WriteLine($"Executing macro: {Description}");
        foreach (var command in commands)
        {
            command.Execute();
        }
    }

    public void Undo()
    {
        Console.WriteLine($"Undoing macro: {Description}");
        // Undo commands in reverse order
        for (int i = commands.Count - 1; i >= 0; i--)
        {
            commands[i].Undo();
        }
    }
}