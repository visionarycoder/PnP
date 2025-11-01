namespace Snippets.DesignPatterns.Behavioral.Command;

public abstract class DeviceCommand(IDevice device) : ICommand
{
    protected readonly IDevice Device = device;

    public abstract void Execute();
    public abstract void Undo();
    public abstract string Description { get; }
}