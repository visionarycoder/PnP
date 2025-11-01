# Adapter Pattern

**Description**: Allows incompatible interfaces to work together by wrapping an existing class with a new interface. Acts as a bridge between two incompatible interfaces.

**Language/Technology**: C# / .NET

**Code**:

```csharp
using System;
using System.Collections.Generic;

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
    private readonly IAdvancedMediaPlayer _advancedPlayer;
    
    public MediaAdapter(string audioType)
    {
        _advancedPlayer = audioType.ToLower() switch
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
                _advancedPlayer.PlayVlc(fileName);
                break;
            case "mp4":
                _advancedPlayer.PlayMp4(fileName);
                break;
            default:
                throw new ArgumentException($"Unsupported audio type: {audioType}");
        }
    }
}

// Adapters for specific players
public class VlcPlayerAdapter : IAdvancedMediaPlayer
{
    private readonly VlcPlayer _vlcPlayer = new();
    
    public void PlayVlc(string fileName)
    {
        _vlcPlayer.PlayVlc(fileName);
    }
    
    public void PlayMp4(string fileName)
    {
        // Not supported
        throw new NotSupportedException("VLC adapter cannot play MP4 files");
    }
}

public class Mp4PlayerAdapter : IAdvancedMediaPlayer
{
    private readonly Mp4Player _mp4Player = new();
    
    public void PlayVlc(string fileName)
    {
        // Not supported
        throw new NotSupportedException("MP4 adapter cannot play VLC files");
    }
    
    public void PlayMp4(string fileName)
    {
        _mp4Player.PlayMp4(fileName);
    }
}

// Main audio player that can play different formats
public class AudioPlayer : IMediaPlayer
{
    private MediaAdapter _mediaAdapter;
    private readonly Mp3Player _mp3Player = new();
    
    public void Play(string audioType, string fileName)
    {
        switch (audioType.ToLower())
        {
            case "mp3":
                _mp3Player.PlayMp3(fileName);
                break;
            case "vlc":
            case "mp4":
                _mediaAdapter = new MediaAdapter(audioType);
                _mediaAdapter.Play(audioType, fileName);
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
    private readonly LegacyDatabase _legacyDatabase;
    
    public DatabaseAdapter(LegacyDatabase legacyDatabase)
    {
        _legacyDatabase = legacyDatabase;
    }
    
    public void Play(string audioType, string fileName)
    {
        // Adapt the legacy database interface to media player interface
        var fileData = _legacyDatabase.GetFileData(fileName);
        Console.WriteLine($"Playing {audioType} from database: {fileData}");
    }
}

public class LegacyDatabase
{
    private readonly Dictionary<string, string> _files = new()
    {
        { "song1.mp3", "MP3 Data for Song 1" },
        { "video1.mp4", "MP4 Data for Video 1" },
        { "movie1.vlc", "VLC Data for Movie 1" }
    };
    
    public string GetFileData(string fileName)
    {
        return _files.TryGetValue(fileName, out var data) ? data : "File not found";
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
    private readonly Mp3Player _mp3Player = new();
    private readonly Mp4Player _mp4Player = new();
    private readonly VlcPlayer _vlcPlayer = new();
    
    // IMediaPlayer implementation
    public void Play(string audioType, string fileName)
    {
        switch (audioType.ToLower())
        {
            case "mp3":
                _mp3Player.PlayMp3(fileName);
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
        _vlcPlayer.PlayVlc(fileName);
    }
    
    public void PlayMp4(string fileName)
    {
        _mp4Player.PlayMp4(fileName);
    }
}

// Generic adapter pattern
public class GenericAdapter<TTarget, TAdaptee> 
    where TTarget : class 
    where TAdaptee : class
{
    private readonly TAdaptee _adaptee;
    private readonly Func<TAdaptee, TTarget> _adapter;
    
    public GenericAdapter(TAdaptee adaptee, Func<TAdaptee, TTarget> adapter)
    {
        _adaptee = adaptee;
        _adapter = adapter;
    }
    
    public TTarget GetTarget()
    {
        return _adapter(_adaptee);
    }
}
```

**Usage**:

```csharp
using System;

class Program
{
    static void Main()
    {
        Console.WriteLine("=== Basic Adapter Pattern ===");
        
        var audioPlayer = new AudioPlayer();
        
        // Play different audio formats
        audioPlayer.Play("mp3", "beyond_the_horizon.mp3");
        audioPlayer.Play("mp4", "alone.mp4");
        audioPlayer.Play("vlc", "far_far_away.vlc");
        audioPlayer.Play("avi", "mind_me.avi"); // Unsupported format
        
        Console.WriteLine("\n=== Object Adapter with Legacy Database ===");
        
        var legacyDb = new LegacyDatabase();
        var dbAdapter = new DatabaseAdapter(legacyDb);
        
        dbAdapter.Play("mp3", "song1.mp3");
        dbAdapter.Play("mp4", "video1.mp4");
        dbAdapter.Play("vlc", "movie1.vlc");
        
        Console.WriteLine("\n=== Class Adapter (Inheritance) ===");
        
        var mp3Adapter = new Mp3Adapter();
        mp3Adapter.Play("mp3", "classic_song.mp3");
        mp3Adapter.Play("mp4", "video.mp4"); // Not supported by this adapter
        
        Console.WriteLine("\n=== Two-Way Adapter ===");
        
        var twoWayAdapter = new TwoWayMediaAdapter();
        
        // Use as IMediaPlayer
        IMediaPlayer mediaPlayer = twoWayAdapter;
        mediaPlayer.Play("mp3", "song.mp3");
        mediaPlayer.Play("vlc", "movie.vlc");
        
        // Use as IAdvancedMediaPlayer
        IAdvancedMediaPlayer advancedPlayer = twoWayAdapter;
        advancedPlayer.PlayMp4("video.mp4");
        advancedPlayer.PlayVlc("documentary.vlc");
        
        Console.WriteLine("\n=== Generic Adapter ===");
        
        var mp3Player = new Mp3Player();
        var genericAdapter = new GenericAdapter<IMediaPlayer, Mp3Player>(
            mp3Player,
            player => new Mp3Adapter() // Convert Mp3Player to IMediaPlayer
        );
        
        var adaptedPlayer = genericAdapter.GetTarget();
        adaptedPlayer.Play("mp3", "adapted_song.mp3");
        
        Console.WriteLine("\n=== Multiple Adapters Demonstration ===");
        
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
}
```

**Notes**:

- Allows integration of classes with incompatible interfaces
- Object Adapter uses composition (recommended approach)
- Class Adapter uses inheritance (limited by single inheritance in C#)
- Two-way adapters implement multiple interfaces for bidirectional conversion
- Useful for integrating third-party libraries or legacy code
- Can add functionality while adapting (decorating behavior)
- Generic adapter provides type-safe adaptation with delegates
- Follows Open/Closed Principle - open for extension, closed for modification
- Related patterns: [Bridge](bridge.md), [Decorator](decorator.md), [Facade](facade.md)

**Prerequisites**:

- .NET Framework 2.0+ or .NET Core
- Understanding of interfaces and polymorphism

**References**:

- Gang of Four Design Patterns book
- [Microsoft Docs: Adapter Pattern](https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/adapter)

---

*Created: 2025-10-31*  
*Last Updated: 2025-10-31*  
*Tags: #adapter #structural #compatibility #integration*
