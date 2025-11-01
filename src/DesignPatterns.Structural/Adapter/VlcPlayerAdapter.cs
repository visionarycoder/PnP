namespace Snippets.DesignPatterns.Structural.Adapter;

public class VlcPlayerAdapter : IAdvancedMediaPlayer
{
    private readonly VlcPlayer vlcPlayer = new();

    public void PlayVlc(string fileName)
    {
        vlcPlayer.PlayVlc(fileName);
    }

    public void PlayMp4(string fileName)
    {
        // Not supported
        throw new NotSupportedException("VLC adapter cannot play MP4 files");
    }
}