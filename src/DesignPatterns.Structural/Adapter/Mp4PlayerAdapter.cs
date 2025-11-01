namespace Snippets.DesignPatterns.Structural.Adapter;

public class Mp4PlayerAdapter : IAdvancedMediaPlayer
{
    private readonly Mp4Player mp4Player = new();

    public void PlayVlc(string fileName)
    {
        // Not supported
        throw new NotSupportedException("MP4 adapter cannot play VLC files");
    }

    public void PlayMp4(string fileName)
    {
        mp4Player.PlayMp4(fileName);
    }
}