namespace Snippets.DesignPatterns.Behavioral.Command;

public class Stereo : IDevice
{
    private bool isOn = false;
    private int volume = 5;
    private string mode = "CD";

    public void TurnOn()
    {
        isOn = true;
        Console.WriteLine("🎵 Stereo turned ON");
    }

    public void TurnOff()
    {
        isOn = false;
        Console.WriteLine("🎵 Stereo turned OFF");
    }

    public void VolumeUp()
    {
        if (isOn && volume < 20)
        {
            volume++;
            Console.WriteLine($"🎶 Stereo Volume: {volume}");
        }
    }

    public void VolumeDown()
    {
        if (isOn && volume > 0)
        {
            volume--;
            Console.WriteLine($"🎶 Stereo Volume: {volume}");
        }
    }

    public void ChannelUp()
    {
        if (isOn)
        {
            mode = mode == "CD" ? "Radio" : "CD";
            Console.WriteLine($"🎼 Mode: {mode}");
        }
    }

    public void ChannelDown()
    {
        ChannelUp(); // Same behavior for this example
    }

    public string GetStatus()
    {
        return $"Stereo: {(isOn ? "ON" : "OFF")}, Volume: {volume}, Mode: {mode}";
    }
}