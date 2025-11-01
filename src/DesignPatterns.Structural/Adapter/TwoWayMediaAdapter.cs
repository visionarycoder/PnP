namespace Snippets.DesignPatterns.Structural.Adapter;

public class TwoWayMediaAdapter : IMediaPlayer, IAdvancedMediaPlayer
{
    private readonly Mp3Player mp3Player = new();
    private readonly Mp4Player mp4Player = new();
    private readonly VlcPlayer vlcPlayer = new();

    // IMediaPlayer implementation
    public void Play(string audioType, string fileName)
    {
        switch (audioType.ToLower())
        {
            case "mp3":
                mp3Player.PlayMp3(fileName);
                break;
            case "mp4":
                PlayMp4(fileName);
                break;
            case "vlc":
                PlayVlc(fileName);
                break;
            default:
                Console.WriteLine($"Unsupported audio type: {audioType}");
                break;
        }
    }

    // IAdvancedMediaPlayer implementation
    public void PlayVlc(string fileName)
    {
        vlcPlayer.PlayVlc(fileName);
    }

    public void PlayMp4(string fileName)
    {
        mp4Player.PlayMp4(fileName);
    }
}