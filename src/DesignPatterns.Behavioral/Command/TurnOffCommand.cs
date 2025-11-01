namespace Snippets.DesignPatterns.Behavioral.Command;

public class TurnOffCommand(IDevice device) : DeviceCommand(device)
{
    public override string Description => "Turn device OFF";

    public override void Execute() => Device.TurnOff();

    public override void Undo() => Device.TurnOn();
}