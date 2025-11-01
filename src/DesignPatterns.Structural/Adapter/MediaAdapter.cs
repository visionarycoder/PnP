namespace Snippets.DesignPatterns.Structural.Adapter;

public class MediaAdapter(string audioType) : IMediaPlayer
{
    private readonly IAdvancedMediaPlayer advancedPlayer = audioType.ToLower() switch
    {
        "vlc" => new VlcPlayerAdapter(),
        "mp4" => new Mp4PlayerAdapter(),
        _ => throw new ArgumentException($"Unsupported audio type: {audioType}")
    };

    public void Play(string audioType, string fileName)
    {
        switch (audioType.ToLower())
        {
            case "vlc":
                advancedPlayer.PlayVlc(fileName);
                break;
            case "mp4":
                advancedPlayer.PlayMp4(fileName);
                break;
            default:
                throw new ArgumentException($"Unsupported audio type: {audioType}");
        }
    }
}