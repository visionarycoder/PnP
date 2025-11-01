namespace Snippets.DesignPatterns.Behavioral.Command;

public class VolumeUpCommand(IDevice device) : DeviceCommand(device)
{
    public override string Description => "Volume UP";

    public override void Execute() => Device.VolumeUp();

    public override void Undo() => Device.VolumeDown();
}