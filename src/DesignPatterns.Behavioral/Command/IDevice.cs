namespace Snippets.DesignPatterns.Behavioral.Command;

public interface IDevice
{
    void TurnOn();
    void TurnOff();
    void VolumeUp();
    void VolumeDown();
    void ChannelUp();
    void ChannelDown();
    string GetStatus();
}