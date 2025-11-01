namespace Snippets.DesignPatterns.Structural.Adapter;

public class Mp3Adapter : Mp3Player, IMediaPlayer
{
    public void Play(string audioType, string fileName)
    {
        if (audioType.ToLower() == "mp3")
        {
            PlayMp3(fileName);
        }
        else
        {
            Console.WriteLine($"MP3 Adapter cannot play {audioType} files");
        }
    }
}