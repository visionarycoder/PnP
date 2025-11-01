namespace Snippets.DesignPatterns.Structural.Adapter;

public class DatabaseAdapter(LegacyDatabase legacyDatabase) : IMediaPlayer
{
    public void Play(string audioType, string fileName)
    {
        // Adapt the legacy database interface to media player interface
        var fileData = legacyDatabase.GetFileData(fileName);
        Console.WriteLine($"Playing {audioType} from database: {fileData}");
    }
}