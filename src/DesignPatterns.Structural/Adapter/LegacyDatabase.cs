namespace Snippets.DesignPatterns.Structural.Adapter;

public class LegacyDatabase
{
    private readonly Dictionary<string, string> files = new()
    {
        { "song1.mp3", "MP3 Data for Song 1" },
        { "video1.mp4", "MP4 Data for Video 1" },
        { "movie1.vlc", "VLC Data for Movie 1" }
    };

    public string GetFileData(string fileName)
    {
        return files.TryGetValue(fileName, out var data) ? data : "File not found";
    }
}