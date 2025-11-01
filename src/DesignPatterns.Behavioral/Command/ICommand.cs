namespace Snippets.DesignPatterns.Behavioral.Command;

public interface ICommand
{
    void Execute();
    void Undo();
    string Description { get; }
}