namespace Snippets.DesignPatterns.Behavioral.Command;

public class TurnOnCommand(IDevice device) : DeviceCommand(device)
{
    public override string Description => "Turn device ON";

    public override void Execute() => Device.TurnOn();

    public override void Undo() => Device.TurnOff();
}