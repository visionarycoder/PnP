# Memento Pattern

**Description**: Captures and externalizes an object's internal state without violating encapsulation, so that the object can be restored to this state later. The pattern provides the ability to save and restore the state of an object without revealing the details of its implementation.

**Language/Technology**: C#

**Code**:

## 1. Basic Memento Structure

```csharp
// Memento interface
public interface IMemento
{
    DateTime CreatedAt { get; }
    string Description { get; }
}

// Originator interface
public interface IOriginator<T> where T : IMemento
{
    T CreateMemento();
    void RestoreFromMemento(T memento);
}

// Caretaker interface
public interface ICaretaker<T> where T : IMemento
{
    void SaveMemento(T memento, string? label = null);
    T? RestoreMemento(int index);
    T? RestoreLatest();
    IReadOnlyList<T> GetHistory();
    void ClearHistory();
}
```

## 2. Text Editor Example

```csharp
// Text editor memento
public class TextEditorMemento : IMemento
{
    public string Content { get; }
    public int CursorPosition { get; }
    public string FontName { get; }
    public int FontSize { get; }
    public bool IsBold { get; }
    public bool IsItalic { get; }
    public DateTime CreatedAt { get; }
    public string Description { get; }
    
    public TextEditorMemento(string content, int cursorPosition, string fontName, 
        int fontSize, bool isBold, bool isItalic, string description)
    {
        Content = content;
        CursorPosition = cursorPosition;
        FontName = fontName;
        FontSize = fontSize;
        IsBold = isBold;
        IsItalic = isItalic;
        CreatedAt = DateTime.UtcNow;
        Description = description;
    }
}

// Text editor (Originator)
public class TextEditor : IOriginator<TextEditorMemento>
{
    private StringBuilder _content = new();
    private int _cursorPosition = 0;
    private string _fontName = "Arial";
    private int _fontSize = 12;
    private bool _isBold = false;
    private bool _isItalic = false;
    
    public string Content => _content.ToString();
    public int CursorPosition => _cursorPosition;
    public string FontName => _fontName;
    public int FontSize => _fontSize;
    public bool IsBold => _isBold;
    public bool IsItalic => _isItalic;
    
    public void InsertText(string text)
    {
        _content.Insert(_cursorPosition, text);
        _cursorPosition += text.Length;
        Console.WriteLine($"Inserted '{text}' at position {_cursorPosition - text.Length}");
    }
    
    public void DeleteText(int length)
    {
        if (_cursorPosition >= length)
        {
            var deletedText = _content.ToString(_cursorPosition - length, length);
            _content.Remove(_cursorPosition - length, length);
            _cursorPosition -= length;
            Console.WriteLine($"Deleted '{deletedText}'");
        }
    }
    
    public void MoveCursor(int position)
    {
        if (position >= 0 && position <= _content.Length)
        {
            _cursorPosition = position;
            Console.WriteLine($"Cursor moved to position {position}");
        }
    }
    
    public void SetFont(string fontName, int fontSize)
    {
        _fontName = fontName;
        _fontSize = fontSize;
        Console.WriteLine($"Font changed to {fontName} {fontSize}pt");
    }
    
    public void SetBold(bool isBold)
    {
        _isBold = isBold;
        Console.WriteLine($"Bold: {(isBold ? "ON" : "OFF")}");
    }
    
    public void SetItalic(bool isItalic)
    {
        _isItalic = isItalic;
        Console.WriteLine($"Italic: {(isItalic ? "ON" : "OFF")}");
    }
    
    public TextEditorMemento CreateMemento()
    {
        var description = $"State at {DateTime.Now:HH:mm:ss} - {Content.Length} chars";
        return new TextEditorMemento(
            Content, 
            _cursorPosition, 
            _fontName, 
            _fontSize, 
            _isBold, 
            _isItalic, 
            description);
    }
    
    public void RestoreFromMemento(TextEditorMemento memento)
    {
        _content = new StringBuilder(memento.Content);
        _cursorPosition = memento.CursorPosition;
        _fontName = memento.FontName;
        _fontSize = memento.FontSize;
        _isBold = memento.IsBold;
        _isItalic = memento.IsItalic;
        
        Console.WriteLine($"Restored state from {memento.CreatedAt:HH:mm:ss}");
        Console.WriteLine($"Content: '{Content}', Cursor: {_cursorPosition}");
    }
    
    public void PrintStatus()
    {
        Console.WriteLine($"Content: '{Content}'");
        Console.WriteLine($"Cursor: {_cursorPosition}, Font: {_fontName} {_fontSize}pt");
        Console.WriteLine($"Bold: {_isBold}, Italic: {_isItalic}");
    }
}
```

## 3. Game State Example

```csharp
// Game state memento
public class GameStateMemento : IMemento
{
    public int Level { get; }
    public int Score { get; }
    public int Lives { get; }
    public PlayerPosition Position { get; }
    public List<Item> Inventory { get; }
    public Dictionary<string, object> GameData { get; }
    public DateTime CreatedAt { get; }
    public string Description { get; }
    
    public GameStateMemento(int level, int score, int lives, PlayerPosition position,
        List<Item> inventory, Dictionary<string, object> gameData, string description)
    {
        Level = level;
        Score = score;
        Lives = lives;
        Position = position;
        Inventory = new List<Item>(inventory); // Deep copy
        GameData = new Dictionary<string, object>(gameData); // Deep copy
        CreatedAt = DateTime.UtcNow;
        Description = description;
    }
}

public record PlayerPosition(int X, int Y, string Area);
public record Item(string Name, string Type, int Quantity, Dictionary<string, object> Properties);

// Game state (Originator)
public class GameState : IOriginator<GameStateMemento>
{
    private int _level = 1;
    private int _score = 0;
    private int _lives = 3;
    private PlayerPosition _position = new(0, 0, "StartArea");
    private List<Item> _inventory = new();
    private Dictionary<string, object> _gameData = new();
    
    public int Level => _level;
    public int Score => _score;
    public int Lives => _lives;
    public PlayerPosition Position => _position;
    public IReadOnlyList<Item> Inventory => _inventory.AsReadOnly();
    
    public void AddScore(int points)
    {
        _score += points;
        Console.WriteLine($"Score increased by {points}. Total: {_score}");
    }
    
    public void LoseLife()
    {
        if (_lives > 0)
        {
            _lives--;
            Console.WriteLine($"Life lost! Remaining lives: {_lives}");
        }
    }
    
    public void GainLife()
    {
        _lives++;
        Console.WriteLine($"Extra life gained! Lives: {_lives}");
    }
    
    public void LevelUp()
    {
        _level++;
        Console.WriteLine($"Level up! Now at level {_level}");
    }
    
    public void MovePlayer(int x, int y, string area)
    {
        _position = new PlayerPosition(x, y, area);
        Console.WriteLine($"Player moved to {area} ({x}, {y})");
    }
    
    public void AddItem(Item item)
    {
        var existingItem = _inventory.FirstOrDefault(i => i.Name == item.Name);
        if (existingItem != null)
        {
            var newQuantity = existingItem.Quantity + item.Quantity;
            _inventory.Remove(existingItem);
            _inventory.Add(existingItem with { Quantity = newQuantity });
        }
        else
        {
            _inventory.Add(item);
        }
        Console.WriteLine($"Added {item.Quantity}x {item.Name} to inventory");
    }
    
    public bool UseItem(string itemName, int quantity = 1)
    {
        var item = _inventory.FirstOrDefault(i => i.Name == itemName);
        if (item != null && item.Quantity >= quantity)
        {
            var newQuantity = item.Quantity - quantity;
            _inventory.Remove(item);
            
            if (newQuantity > 0)
            {
                _inventory.Add(item with { Quantity = newQuantity });
            }
            
            Console.WriteLine($"Used {quantity}x {itemName}");
            return true;
        }
        
        Console.WriteLine($"Not enough {itemName} in inventory");
        return false;
    }
    
    public void SetGameData(string key, object value)
    {
        _gameData[key] = value;
        Console.WriteLine($"Game data set: {key} = {value}");
    }
    
    public GameStateMemento CreateMemento()
    {
        var description = $"Level {_level} - Score: {_score} - Lives: {_lives}";
        return new GameStateMemento(
            _level,
            _score,
            _lives,
            _position,
            _inventory,
            _gameData,
            description);
    }
    
    public void RestoreFromMemento(GameStateMemento memento)
    {
        _level = memento.Level;
        _score = memento.Score;
        _lives = memento.Lives;
        _position = memento.Position;
        _inventory = new List<Item>(memento.Inventory);
        _gameData = new Dictionary<string, object>(memento.GameData);
        
        Console.WriteLine($"Game state restored: {memento.Description}");
    }
    
    public void PrintStatus()
    {
        Console.WriteLine($"Level: {_level}, Score: {_score}, Lives: {_lives}");
        Console.WriteLine($"Position: {_position.Area} ({_position.X}, {_position.Y})");
        Console.WriteLine($"Inventory: {_inventory.Count} items");
        foreach (var item in _inventory)
        {
            Console.WriteLine($"  - {item.Name} x{item.Quantity}");
        }
    }
}
```

## 4. Generic Caretaker Implementation

```csharp
// Generic caretaker
public class MementoCaretaker<T> : ICaretaker<T> where T : IMemento
{
    private readonly List<(T Memento, string Label, DateTime SavedAt)> _history = new();
    private readonly int _maxHistorySize;
    
    public MementoCaretaker(int maxHistorySize = 50)
    {
        _maxHistorySize = maxHistorySize;
    }
    
    public void SaveMemento(T memento, string? label = null)
    {
        var saveLabel = label ?? $"Save {_history.Count + 1}";
        _history.Add((memento, saveLabel, DateTime.UtcNow));
        
        // Maintain max history size
        while (_history.Count > _maxHistorySize)
        {
            _history.RemoveAt(0);
        }
        
        Console.WriteLine($"Memento saved: {saveLabel} ({memento.Description})");
    }
    
    public T? RestoreMemento(int index)
    {
        if (index >= 0 && index < _history.Count)
        {
            var (memento, label, savedAt) = _history[index];
            Console.WriteLine($"Restoring memento {index}: {label}");
            return memento;
        }
        
        Console.WriteLine($"Invalid memento index: {index}");
        return default(T);
    }
    
    public T? RestoreLatest()
    {
        if (_history.Count > 0)
        {
            return RestoreMemento(_history.Count - 1);
        }
        
        Console.WriteLine("No mementos available");
        return default(T);
    }
    
    public IReadOnlyList<T> GetHistory()
    {
        return _history.Select(h => h.Memento).ToList().AsReadOnly();
    }
    
    public void ClearHistory()
    {
        var count = _history.Count;
        _history.Clear();
        Console.WriteLine($"Cleared {count} mementos from history");
    }
    
    public void PrintHistory()
    {
        Console.WriteLine($"\nMemento History ({_history.Count} items):");
        for (int i = 0; i < _history.Count; i++)
        {
            var (memento, label, savedAt) = _history[i];
            Console.WriteLine($"{i}: {label} - {memento.Description} (Saved: {savedAt:HH:mm:ss})");
        }
    }
    
    public void DeleteMemento(int index)
    {
        if (index >= 0 && index < _history.Count)
        {
            var (_, label, _) = _history[index];
            _history.RemoveAt(index);
            Console.WriteLine($"Deleted memento: {label}");
        }
    }
    
    public List<T> GetMementosByTimeRange(DateTime start, DateTime end)
    {
        return _history
            .Where(h => h.SavedAt >= start && h.SavedAt <= end)
            .Select(h => h.Memento)
            .ToList();
    }
}
```

## 5. Advanced Memento Features

```csharp
// Compressed memento for large states
public class CompressedMemento : IMemento
{
    public byte[] CompressedData { get; }
    public DateTime CreatedAt { get; }
    public string Description { get; }
    public long OriginalSize { get; }
    public long CompressedSize => CompressedData.Length;
    
    public CompressedMemento(object state, string description)
    {
        CreatedAt = DateTime.UtcNow;
        Description = description;
        
        var json = System.Text.Json.JsonSerializer.Serialize(state);
        var originalBytes = System.Text.Encoding.UTF8.GetBytes(json);
        OriginalSize = originalBytes.Length;
        
        using var inputStream = new MemoryStream(originalBytes);
        using var outputStream = new MemoryStream();
        using (var compressionStream = new System.IO.Compression.GZipStream(outputStream, System.IO.Compression.CompressionMode.Compress))
        {
            inputStream.CopyTo(compressionStream);
        }
        
        CompressedData = outputStream.ToArray();
        Console.WriteLine($"Compressed {OriginalSize} bytes to {CompressedSize} bytes ({CompressionRatio:P1})");
    }
    
    public T Decompress<T>()
    {
        using var inputStream = new MemoryStream(CompressedData);
        using var compressionStream = new System.IO.Compression.GZipStream(inputStream, System.IO.Compression.CompressionMode.Decompress);
        using var outputStream = new MemoryStream();
        
        compressionStream.CopyTo(outputStream);
        var decompressedBytes = outputStream.ToArray();
        var json = System.Text.Encoding.UTF8.GetString(decompressedBytes);
        
        return System.Text.Json.JsonSerializer.Deserialize<T>(json) ?? throw new InvalidOperationException("Failed to deserialize memento");
    }
    
    public double CompressionRatio => (double)CompressedSize / OriginalSize;
}

// Incremental memento (stores only changes)
public class IncrementalMemento : IMemento
{
    public object? BaseState { get; }
    public Dictionary<string, object?> Changes { get; }
    public DateTime CreatedAt { get; }
    public string Description { get; }
    
    public IncrementalMemento(object? baseState, Dictionary<string, object?> changes, string description)
    {
        BaseState = baseState;
        Changes = changes;
        CreatedAt = DateTime.UtcNow;
        Description = description;
    }
    
    public static IncrementalMemento CreateFromChanges<T>(T currentState, T previousState, string description)
    {
        var changes = new Dictionary<string, object?>();
        
        var properties = typeof(T).GetProperties();
        foreach (var prop in properties)
        {
            var currentValue = prop.GetValue(currentState);
            var previousValue = prop.GetValue(previousState);
            
            if (!Equals(currentValue, previousValue))
            {
                changes[prop.Name] = currentValue;
            }
        }
        
        return new IncrementalMemento(previousState, changes, description);
    }
}

// Snapshot manager with branching
public class SnapshotManager<T> where T : IMemento
{
    private readonly Dictionary<string, List<T>> _branches = new();
    private string _currentBranch = "main";
    
    public void CreateBranch(string branchName, string? fromBranch = null)
    {
        var sourceBranch = fromBranch ?? _currentBranch;
        
        if (_branches.TryGetValue(sourceBranch, out var sourceSnapshots))
        {
            _branches[branchName] = new List<T>(sourceSnapshots);
        }
        else
        {
            _branches[branchName] = new List<T>();
        }
        
        Console.WriteLine($"Created branch '{branchName}' from '{sourceBranch}'");
    }
    
    public void SwitchBranch(string branchName)
    {
        if (_branches.ContainsKey(branchName))
        {
            _currentBranch = branchName;
            Console.WriteLine($"Switched to branch '{branchName}'");
        }
        else
        {
            Console.WriteLine($"Branch '{branchName}' not found");
        }
    }
    
    public void SaveSnapshot(T memento, string? label = null)
    {
        if (!_branches.ContainsKey(_currentBranch))
        {
            _branches[_currentBranch] = new List<T>();
        }
        
        _branches[_currentBranch].Add(memento);
        Console.WriteLine($"Saved snapshot to branch '{_currentBranch}': {label ?? memento.Description}");
    }
    
    public T? GetSnapshot(int index)
    {
        if (_branches.TryGetValue(_currentBranch, out var snapshots) && 
            index >= 0 && index < snapshots.Count)
        {
            return snapshots[index];
        }
        
        return default(T);
    }
    
    public List<string> GetBranches()
    {
        return _branches.Keys.ToList();
    }
    
    public void MergeBranch(string sourceBranch, string targetBranch)
    {
        if (_branches.TryGetValue(sourceBranch, out var sourceSnapshots) &&
            _branches.TryGetValue(targetBranch, out var targetSnapshots))
        {
            // Simple merge: append source snapshots to target
            targetSnapshots.AddRange(sourceSnapshots);
            Console.WriteLine($"Merged branch '{sourceBranch}' into '{targetBranch}'");
        }
    }
}

// Auto-save caretaker
public class AutoSaveCaretaker<T> : ICaretaker<T>, IDisposable where T : IMemento
{
    private readonly MementoCaretaker<T> _caretaker;
    private readonly Timer _autoSaveTimer;
    private readonly TimeSpan _autoSaveInterval;
    private Func<T>? _mementoFactory;
    
    public AutoSaveCaretaker(TimeSpan autoSaveInterval, int maxHistorySize = 50)
    {
        _caretaker = new MementoCaretaker<T>(maxHistorySize);
        _autoSaveInterval = autoSaveInterval;
        _autoSaveTimer = new Timer(AutoSave, null, Timeout.Infinite, Timeout.Infinite);
    }
    
    public void SetMementoFactory(Func<T> factory)
    {
        _mementoFactory = factory;
    }
    
    public void StartAutoSave()
    {
        _autoSaveTimer.Change(_autoSaveInterval, _autoSaveInterval);
        Console.WriteLine($"Auto-save started with interval: {_autoSaveInterval.TotalSeconds}s");
    }
    
    public void StopAutoSave()
    {
        _autoSaveTimer.Change(Timeout.Infinite, Timeout.Infinite);
        Console.WriteLine("Auto-save stopped");
    }
    
    private void AutoSave(object? state)
    {
        if (_mementoFactory != null)
        {
            var memento = _mementoFactory();
            SaveMemento(memento, "Auto-save");
        }
    }
    
    public void SaveMemento(T memento, string? label = null)
    {
        _caretaker.SaveMemento(memento, label);
    }
    
    public T? RestoreMemento(int index) => _caretaker.RestoreMemento(index);
    public T? RestoreLatest() => _caretaker.RestoreLatest();
    public IReadOnlyList<T> GetHistory() => _caretaker.GetHistory();
    public void ClearHistory() => _caretaker.ClearHistory();
    
    public void Dispose()
    {
        _autoSaveTimer?.Dispose();
    }
}
```

**Usage**:

```csharp
// 1. Text Editor Example
var editor = new TextEditor();
var caretaker = new MementoCaretaker<TextEditorMemento>();

// Make some changes and save states
editor.InsertText("Hello World");
caretaker.SaveMemento(editor.CreateMemento(), "Initial text");

editor.SetFont("Times New Roman", 14);
editor.SetBold(true);
caretaker.SaveMemento(editor.CreateMemento(), "Formatted text");

editor.InsertText("\nThis is a new line.");
editor.SetItalic(true);
caretaker.SaveMemento(editor.CreateMemento(), "Added line with italic");

editor.PrintStatus();
caretaker.PrintHistory();

// Restore previous state
var previousState = caretaker.RestoreMemento(1);
if (previousState != null)
{
    editor.RestoreFromMemento(previousState);
    editor.PrintStatus();
}

// 2. Game State Example
var gameState = new GameState();
var gameCaretaker = new MementoCaretaker<GameStateMemento>();

// Progress through game
gameState.AddScore(100);
gameState.AddItem(new Item("Health Potion", "Consumable", 3, new()));
gameCaretaker.SaveMemento(gameState.CreateMemento(), "Game Start");

gameState.LevelUp();
gameState.MovePlayer(10, 15, "Forest");
gameState.AddScore(250);
gameCaretaker.SaveMemento(gameState.CreateMemento(), "Level 2 Start");

gameState.UseItem("Health Potion");
gameState.LoseLife();
gameState.AddScore(150);
gameCaretaker.SaveMemento(gameState.CreateMemento(), "After Battle");

// Restore to checkpoint
var checkpoint = gameCaretaker.RestoreMemento(1);
if (checkpoint != null)
{
    gameState.RestoreFromMemento(checkpoint);
    gameState.PrintStatus();
}

// 3. Snapshot Manager with Branching
var snapshotManager = new SnapshotManager<GameStateMemento>();

snapshotManager.SaveSnapshot(gameState.CreateMemento(), "Main Progress");
snapshotManager.CreateBranch("experimental");
snapshotManager.SwitchBranch("experimental");

// Make experimental changes
gameState.AddScore(1000);
gameState.GainLife();
snapshotManager.SaveSnapshot(gameState.CreateMemento(), "Experimental Changes");

// Switch back to main branch
snapshotManager.SwitchBranch("main");
var mainSnapshot = snapshotManager.GetSnapshot(0);
if (mainSnapshot != null)
{
    gameState.RestoreFromMemento(mainSnapshot);
}

// 4. Auto-save Example
using var autoSaveCaretaker = new AutoSaveCaretaker<TextEditorMemento>(TimeSpan.FromSeconds(5));
autoSaveCaretaker.SetMementoFactory(() => editor.CreateMemento());
autoSaveCaretaker.StartAutoSave();

// Make changes (auto-saves will occur every 5 seconds)
editor.InsertText("Auto-saved content");
await Task.Delay(6000); // Wait for auto-save

editor.InsertText("\nMore content");
await Task.Delay(6000); // Wait for another auto-save

autoSaveCaretaker.StopAutoSave();

// Expected output demonstrates:
// - State capture and restoration for text editor
// - Game checkpoint system with inventory and progress
// - History management with labels and timestamps
// - Branching and merging capabilities for different save paths
// - Auto-save functionality with configurable intervals
// - Memory optimization through compression
// - Incremental state saving for large objects
```

**Notes**:

- **Encapsulation**: Object internal state is captured without exposing implementation details
- **Immutability**: Mementos should be immutable to prevent accidental state changes
- **Memory Usage**: Can consume significant memory if many states are stored
- **Deep vs Shallow Copy**: Consider whether references should be copied or shared
- **Serialization**: Complex objects may need custom serialization logic
- **Performance**: Creating mementos can be expensive for large objects
- **History Management**: Implement size limits and cleanup strategies
- **Branching**: Advanced scenarios may require branching and merging capabilities
- **Compression**: Use compression for large state objects to save memory

**Prerequisites**:

- .NET 6.0 or later
- Understanding of object state and serialization
- Knowledge of the Command pattern (often used together)
- Familiarity with memory management concepts

**Related Patterns**:

- **Command**: Often combined with Memento for undo/redo functionality
- **Prototype**: Similar concept of object copying, but for different purposes
- **Singleton**: Caretakers are often implemented as singletons
- **Observer**: State changes can trigger automatic memento creation

