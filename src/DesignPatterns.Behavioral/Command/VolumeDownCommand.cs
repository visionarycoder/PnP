namespace Snippets.DesignPatterns.Behavioral.Command;

public class VolumeDownCommand(IDevice device) : DeviceCommand(device)
{
    public override string Description => "Volume DOWN";

    public override void Execute() => Device.VolumeDown();

    public override void Undo() => Device.VolumeUp();
}