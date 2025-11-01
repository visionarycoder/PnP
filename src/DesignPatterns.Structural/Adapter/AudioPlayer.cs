namespace Snippets.DesignPatterns.Structural.Adapter;

public class AudioPlayer : IMediaPlayer
{
    private MediaAdapter? mediaAdapter;
    private readonly Mp3Player mp3Player = new();

    public void Play(string audioType, string fileName)
    {
        switch (audioType.ToLower())
        {
            case "mp3":
                mp3Player.PlayMp3(fileName);
                break;
            case "vlc":
            case "mp4":
                mediaAdapter = new MediaAdapter(audioType);
                mediaAdapter.Play(audioType, fileName);
                break;
            default:
                Console.WriteLine($"Invalid media. {audioType} format not supported");
                break;
        }
    }
}