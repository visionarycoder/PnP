// See https://aka.ms/new-console-template for more information
namespace DesignPatterns.Adapter;

// Target interface that clients expect to work with
public interface IMediaPlayer
{
    void Play(string audioType, string fileName);
}

// Adaptee classes with incompatible interfaces
public class Mp3Player
{
    public void PlayMp3(string fileName)
    {
        Console.WriteLine($"Playing MP3 file: {fileName}");
    }
}

public class Mp4Player
{
    public void PlayMp4(string fileName)
    {
        Console.WriteLine($"Playing MP4 file: {fileName}");
    }
}

public class VlcPlayer
{
    public void PlayVlc(string fileName)
    {
        Console.WriteLine($"Playing VLC file: {fileName}");
    }
}

// Advanced media format interfaces
public interface IAdvancedMediaPlayer
{
    void PlayVlc(string fileName);
    void PlayMp4(string fileName);
}

// Adapter that implements the target interface
public class MediaAdapter : IMediaPlayer
{
    private readonly IAdvancedMediaPlayer advancedPlayer;
    
    public MediaAdapter(string audioType)
    {
        advancedPlayer = audioType.ToLower() switch
        {
            "vlc" => new VlcPlayerAdapter(),
            "mp4" => new Mp4PlayerAdapter(),
            _ => throw new ArgumentException($"Unsupported audio type: {audioType}")
        };
    }
    
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

// Adapters for specific players
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

// Main audio player that can play different formats
public class AudioPlayer : IMediaPlayer
{
    private MediaAdapter mediaAdapter = null!;
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

// Object Adapter Pattern Example (Composition)
public class DatabaseAdapter : IMediaPlayer
{
    private readonly LegacyDatabase legacyDatabase;
    
    public DatabaseAdapter(LegacyDatabase legacyDatabase)
    {
        this.legacyDatabase = legacyDatabase;
    }
    
    public void Play(string audioType, string fileName)
    {
        // Adapt the legacy database interface to media player interface
        var fileData = legacyDatabase.GetFileData(fileName);
        Console.WriteLine($"Playing {audioType} from database: {fileData}");
    }
}

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

// Class Adapter Pattern Example (Inheritance)
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

// Two-way adapter (implements both interfaces)
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
                Console.WriteLine($"Unsupported format: {audioType}");
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

// Generic adapter pattern
public class GenericAdapter<TTarget, TAdaptee> 
    where TTarget : class 
    where TAdaptee : class
{
    private readonly TAdaptee adaptee;
    private readonly Func<TAdaptee, TTarget> adapter;
    
    public GenericAdapter(TAdaptee adaptee, Func<TAdaptee, TTarget> adapter)
    {
        this.adaptee = adaptee;
        this.adapter = adapter;
    }
    
    public TTarget GetTarget()
    {
        return adapter(adaptee);
    }
}

// Adapter demonstration service
public class AdapterDemoService
{
    public void RunAdapterPatternDemo()
    {
        Console.WriteLine("==================================================");
        Console.WriteLine("              ADAPTER PATTERN DEMO");
        Console.WriteLine("==================================================");
        
        DemonstrateBasicAdapter();
        DemonstrateObjectAdapter();
        DemonstrateClassAdapter();
        DemonstrateTwoWayAdapter();
        DemonstrateGenericAdapter();
        DemonstrateMultipleAdapters();
        DemonstratePatternBenefits();
    }
    
    private void DemonstrateBasicAdapter()
    {
        Console.WriteLine("\n=== Basic Adapter Pattern ===");
        Console.WriteLine("Demonstrates adapting incompatible interfaces for media playback:");
        
        var audioPlayer = new AudioPlayer();
        
        Console.WriteLine("\nTesting supported formats:");
        audioPlayer.Play("mp3", "beyond_the_horizon.mp3");
        audioPlayer.Play("mp4", "alone.mp4");
        audioPlayer.Play("vlc", "far_far_away.vlc");
        
        Console.WriteLine("\nTesting unsupported format:");
        audioPlayer.Play("avi", "mind_me.avi");
    }
    
    private void DemonstrateObjectAdapter()
    {
        Console.WriteLine("\n=== Object Adapter with Legacy Database ===");
        Console.WriteLine("Shows how to adapt legacy systems with composition:");
        
        var legacyDb = new LegacyDatabase();
        var dbAdapter = new DatabaseAdapter(legacyDb);
        
        dbAdapter.Play("mp3", "song1.mp3");
        dbAdapter.Play("mp4", "video1.mp4");
        dbAdapter.Play("vlc", "movie1.vlc");
    }
    
    private void DemonstrateClassAdapter()
    {
        Console.WriteLine("\n=== Class Adapter (Inheritance) ===");
        Console.WriteLine("Demonstrates adapter using inheritance:");
        
        var mp3Adapter = new Mp3Adapter();
        mp3Adapter.Play("mp3", "classic_song.mp3");
        
        Console.WriteLine("\nTesting unsupported format on class adapter:");
        mp3Adapter.Play("mp4", "video.mp4");
    }
    
    private void DemonstrateTwoWayAdapter()
    {
        Console.WriteLine("\n=== Two-Way Adapter ===");
        Console.WriteLine("Shows bidirectional interface adaptation:");
        
        var twoWayAdapter = new TwoWayMediaAdapter();
        
        Console.WriteLine("\nUsing as IMediaPlayer:");
        IMediaPlayer mediaPlayer = twoWayAdapter;
        mediaPlayer.Play("mp3", "song.mp3");
        mediaPlayer.Play("vlc", "movie.vlc");
        
        Console.WriteLine("\nUsing as IAdvancedMediaPlayer:");
        IAdvancedMediaPlayer advancedPlayer = twoWayAdapter;
        advancedPlayer.PlayMp4("video.mp4");
        advancedPlayer.PlayVlc("documentary.vlc");
    }
    
    private void DemonstrateGenericAdapter()
    {
        Console.WriteLine("\n=== Generic Adapter ===");
        Console.WriteLine("Type-safe adaptation with delegates:");
        
        var mp3Player = new Mp3Player();
        var genericAdapter = new GenericAdapter<IMediaPlayer, Mp3Player>(
            mp3Player,
            player => new Mp3Adapter() // Convert Mp3Player to IMediaPlayer
        );
        
        var adaptedPlayer = genericAdapter.GetTarget();
        adaptedPlayer.Play("mp3", "adapted_song.mp3");
    }
    
    private void DemonstrateMultipleAdapters()
    {
        Console.WriteLine("\n=== Multiple Adapters Demonstration ===");
        Console.WriteLine("Testing polymorphic behavior across different adapters:");
        
        var players = new IMediaPlayer[]
        {
            new AudioPlayer(),
            new DatabaseAdapter(new LegacyDatabase()),
            new Mp3Adapter(),
            new TwoWayMediaAdapter()
        };
        
        foreach (var player in players)
        {
            Console.WriteLine($"\nUsing {player.GetType().Name}:");
            try
            {
                player.Play("mp3", "test.mp3");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
    
    private void DemonstratePatternBenefits()
    {
        Console.WriteLine("\n=== Adapter Pattern Benefits ===");
        
        var benefits = new[]
        {
            "✓ Allows incompatible interfaces to work together",
            "✓ Enables reuse of existing classes without modification",
            "✓ Separates interface conversion from business logic",
            "✓ Follows Open/Closed Principle",
            "✓ Supports both composition and inheritance approaches",
            "✓ Facilitates integration with third-party libraries",
            "✓ Provides clean abstraction for legacy system integration"
        };
        
        foreach (var benefit in benefits)
        {
            Console.WriteLine(benefit);
        }
        
        Console.WriteLine("\n=== Related Patterns ===");
        Console.WriteLine("• Bridge Pattern - Separates abstraction from implementation");
        Console.WriteLine("• Decorator Pattern - Adds behavior while maintaining interface");
        Console.WriteLine("• Facade Pattern - Provides simplified interface to complex subsystem");
        Console.WriteLine("• Proxy Pattern - Controls access to another object");
        
        Console.WriteLine("\n=== Use Cases ===");
        Console.WriteLine("• Legacy system integration");
        Console.WriteLine("• Third-party library adaptation");
        Console.WriteLine("• Interface standardization");
        Console.WriteLine("• Protocol conversion");
        Console.WriteLine("• Data format transformation");
    }
}

class Program
{
    static void Main(string[] args)
    {
        try
        {
            var demo = new AdapterDemoService();
            demo.RunAdapterPatternDemo();
            
            Console.WriteLine("\n==================================================");
            Console.WriteLine("Adapter pattern demonstration completed successfully!");
            Console.WriteLine("==================================================");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nError during adapter pattern demonstration: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
        }
        
        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
}
