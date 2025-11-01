namespace Snippets.DesignPatterns.Behavioral.Command;

public class Television : IDevice
{
    private bool isOn = false;
    private int volume = 10;
    private int channel = 1;

    public void TurnOn()
    {
        isOn = true;
        Console.WriteLine("📺 TV turned ON");
    }

    public void TurnOff()
    {
        isOn = false;
        Console.WriteLine("📺 TV turned OFF");
    }

    public void VolumeUp()
    {
        if (isOn && volume < 100)
        {
            volume++;
            Console.WriteLine($"🔊 Volume: {volume}");
        }
    }

    public void VolumeDown()
    {
        if (isOn && volume > 0)
        {
            volume--;
            Console.WriteLine($"🔉 Volume: {volume}");
        }
    }

    public void ChannelUp()
    {
        if (isOn)
        {
            channel = channel >= 999 ? 1 : channel + 1;
            Console.WriteLine($"📻 Channel: {channel}");
        }
    }

    public void ChannelDown()
    {
        if (isOn)
        {
            channel = channel <= 1 ? 999 : channel - 1;
            Console.WriteLine($"📻 Channel: {channel}");
        }
    }

    public string GetStatus()
    {
        return $"TV: {(isOn ? "ON" : "OFF")}, Volume: {volume}, Channel: {channel}";
    }
}