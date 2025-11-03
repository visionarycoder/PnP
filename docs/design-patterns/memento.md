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
    private StringBuilder content = new();
    private int cursorPosition = 0;
    private string fontName = "Arial";
    private int fontSize = 12;
    private bool isBold = false;
    private bool isItalic = false;
    
    public string Content => content.ToString();
    public int CursorPosition => cursorPosition;
    public string FontName => fontName;
    public int FontSize => fontSize;
    public bool IsBold => isBold;
    public bool IsItalic => isItalic;
    
    public void InsertText(string text)
    {
        content.Insert(cursorPosition, text);
        cursorPosition += text.Length;
        Console.WriteLine($"Inserted '{text}' at position {cursorPosition - text.Length}");
    }
    
    public void DeleteText(int length)
    {
        if (cursorPosition >= length)
        {
            var deletedText = content.ToString(cursorPosition - length, length);
            content.Remove(cursorPosition - length, length);
            cursorPosition -= length;
            Console.WriteLine($"Deleted '{deletedText}'");
        }
    }
    
    public void MoveCursor(int position)
    {
        if (position >= 0 && position <= content.Length)
        {
            cursorPosition = position;
            Console.WriteLine($"Cursor moved to position {position}");
        }
    }
    
    public void SetFont(string fontName, int fontSize)
    {
        fontName = fontName;
        fontSize = fontSize;
        Console.WriteLine($"Font changed to {fontName} {fontSize}pt");
    }
    
    public void SetBold(bool isBold)
    {
        isBold = isBold;
        Console.WriteLine($"Bold: {(isBold ? "ON" : "OFF")}");
    }
    
    public void SetItalic(bool isItalic)
    {
        isItalic = isItalic;
        Console.WriteLine($"Italic: {(isItalic ? "ON" : "OFF")}");
    }
    
    public TextEditorMemento CreateMemento()
    {
        var description = $"State at {DateTime.Now:HH:mm:ss} - {Content.Length} chars";
        return new TextEditorMemento(
            Content, 
            cursorPosition, 
            fontName, 
            fontSize, 
            isBold, 
            isItalic, 
            description);
    }
    
    public void RestoreFromMemento(TextEditorMemento memento)
    {
        content = new StringBuilder(memento.Content);
        cursorPosition = memento.CursorPosition;
        fontName = memento.FontName;
        fontSize = memento.FontSize;
        isBold = memento.IsBold;
        isItalic = memento.IsItalic;
        
        Console.WriteLine($"Restored state from {memento.CreatedAt:HH:mm:ss}");
        Console.WriteLine($"Content: '{Content}', Cursor: {cursorPosition}");
    }
    
    public void PrintStatus()
    {
        Console.WriteLine($"Content: '{Content}'");
        Console.WriteLine($"Cursor: {cursorPosition}, Font: {fontName} {fontSize}pt");
        Console.WriteLine($"Bold: {isBold}, Italic: {isItalic}");
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
    private int level = 1;
    private int score = 0;
    private int lives = 3;
    private PlayerPosition position = new(0, 0, "StartArea");
    private List<Item> inventory = new();
    private Dictionary<string, object> gameData = new();
    
    public int Level => level;
    public int Score => score;
    public int Lives => lives;
    public PlayerPosition Position => position;
    public IReadOnlyList<Item> Inventory => inventory.AsReadOnly();
    
    public void AddScore(int points)
    {
        score += points;
        Console.WriteLine($"Score increased by {points}. Total: {score}");
    }
    
    public void LoseLife()
    {
        if (lives > 0)
        {
            _lives--;
            Console.WriteLine($"Life lost! Remaining lives: {lives}");
        }
    }
    
    public void GainLife()
    {
        _lives++;
        Console.WriteLine($"Extra life gained! Lives: {lives}");
    }
    
    public void LevelUp()
    {
        _level++;
        Console.WriteLine($"Level up! Now at level {level}");
    }
    
    public void MovePlayer(int x, int y, string area)
    {
        position = new PlayerPosition(x, y, area);
        Console.WriteLine($"Player moved to {area} ({x}, {y})");
    }
    
    public void AddItem(Item item)
    {
        var existingItem = inventory.FirstOrDefault(i => i.Name == item.Name);
        if (existingItem != null)
        {
            var newQuantity = existingItem.Quantity + item.Quantity;
            inventory.Remove(existingItem);
            inventory.Add(existingItem with { Quantity = newQuantity });
        }
        else
        {
            inventory.Add(item);
        }
        Console.WriteLine($"Added {item.Quantity}x {item.Name} to inventory");
    }
    
    public bool UseItem(string itemName, int quantity = 1)
    {
        var item = inventory.FirstOrDefault(i => i.Name == itemName);
        if (item != null && item.Quantity >= quantity)
        {
            var newQuantity = item.Quantity - quantity;
            inventory.Remove(item);
            
            if (newQuantity > 0)
            {
                inventory.Add(item with { Quantity = newQuantity });
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
        var description = $"Level {level} - Score: {score} - Lives: {lives}";
        return new GameStateMemento(
            level,
            score,
            lives,
            position,
            inventory,
            gameData,
            description);
    }
    
    public void RestoreFromMemento(GameStateMemento memento)
    {
        level = memento.Level;
        score = memento.Score;
        lives = memento.Lives;
        position = memento.Position;
        inventory = new List<Item>(memento.Inventory);
        gameData = new Dictionary<string, object>(memento.GameData);
        
        Console.WriteLine($"Game state restored: {memento.Description}");
    }
    
    public void PrintStatus()
    {
        Console.WriteLine($"Level: {level}, Score: {score}, Lives: {lives}");
        Console.WriteLine($"Position: {position.Area} ({position.X}, {position.Y})");
        Console.WriteLine($"Inventory: {inventory.Count} items");
        foreach (var item in inventory)
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
    private readonly List<(T Memento, string Label, DateTime SavedAt)> history = new();
    private readonly int maxHistorySize;
    
    public MementoCaretaker(int maxHistorySize = 50)
    {
        maxHistorySize = maxHistorySize;
    }
    
    public void SaveMemento(T memento, string? label = null)
    {
        var saveLabel = label ?? $"Save {history.Count + 1}";
        history.Add((memento, saveLabel, DateTime.UtcNow));
        
        // Maintain max history size
        while (history.Count > maxHistorySize)
        {
            history.RemoveAt(0);
        }
        
        Console.WriteLine($"Memento saved: {saveLabel} ({memento.Description})");
    }
    
    public T? RestoreMemento(int index)
    {
        if (index >= 0 && index < history.Count)
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
        if (history.Count > 0)
        {
            return RestoreMemento(history.Count - 1);
        }
        
        Console.WriteLine("No mementos available");
        return default(T);
    }
    
    public IReadOnlyList<T> GetHistory()
    {
        return history.Select(h => h.Memento).ToList().AsReadOnly();
    }
    
    public void ClearHistory()
    {
        var count = history.Count;
        history.Clear();
        Console.WriteLine($"Cleared {count} mementos from history");
    }
    
    public void PrintHistory()
    {
        Console.WriteLine($"\nMemento History ({history.Count} items):");
        for (int i = 0; i < history.Count; i++)
        {
            var (memento, label, savedAt) = _history[i];
            Console.WriteLine($"{i}: {label} - {memento.Description} (Saved: {savedAt:HH:mm:ss})");
        }
    }
    
    public void DeleteMemento(int index)
    {
        if (index >= 0 && index < history.Count)
        {
            var (_, label, _) = _history[index];
            history.RemoveAt(index);
            Console.WriteLine($"Deleted memento: {label}");
        }
    }
    
    public List<T> GetMementosByTimeRange(DateTime start, DateTime end)
    {
        return history
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
    private readonly Dictionary<string, List<T>> branches = new();
    private string currentBranch = "main";
    
    public void CreateBranch(string branchName, string? fromBranch = null)
    {
        var sourceBranch = fromBranch ?? currentBranch;
        
        if (branches.TryGetValue(sourceBranch, out var sourceSnapshots))
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
        if (branches.ContainsKey(branchName))
        {
            currentBranch = branchName;
            Console.WriteLine($"Switched to branch '{branchName}'");
        }
        else
        {
            Console.WriteLine($"Branch '{branchName}' not found");
        }
    }
    
    public void SaveSnapshot(T memento, string? label = null)
    {
        if (!branches.ContainsKey(currentBranch))
        {
            _branches[_currentBranch] = new List<T>();
        }
        
        _branches[_currentBranch].Add(memento);
        Console.WriteLine($"Saved snapshot to branch '{currentBranch}': {label ?? memento.Description}");
    }
    
    public T? GetSnapshot(int index)
    {
        if (branches.TryGetValue(currentBranch, out var snapshots) && 
            index >= 0 && index < snapshots.Count)
        {
            return snapshots[index];
        }
        
        return default(T);
    }
    
    public List<string> GetBranches()
    {
        return branches.Keys.ToList();
    }
    
    public void MergeBranch(string sourceBranch, string targetBranch)
    {
        if (branches.TryGetValue(sourceBranch, out var sourceSnapshots) &&
            branches.TryGetValue(targetBranch, out var targetSnapshots))
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
    private readonly MementoCaretaker<T> caretaker;
    private readonly Timer autoSaveTimer;
    private readonly TimeSpan autoSaveInterval;
    private Func<T>? mementoFactory;
    
    public AutoSaveCaretaker(TimeSpan autoSaveInterval, int maxHistorySize = 50)
    {
        caretaker = new MementoCaretaker<T>(maxHistorySize);
        autoSaveInterval = autoSaveInterval;
        autoSaveTimer = new Timer(AutoSave, null, Timeout.Infinite, Timeout.Infinite);
    }
    
    public void SetMementoFactory(Func<T> factory)
    {
        mementoFactory = factory;
    }
    
    public void StartAutoSave()
    {
        autoSaveTimer.Change(autoSaveInterval, autoSaveInterval);
        Console.WriteLine($"Auto-save started with interval: {autoSaveInterval.TotalSeconds}s");
    }
    
    public void StopAutoSave()
    {
        autoSaveTimer.Change(Timeout.Infinite, Timeout.Infinite);
        Console.WriteLine("Auto-save stopped");
    }
    
    private void AutoSave(object? state)
    {
        if (mementoFactory != null)
        {
            var memento = mementoFactory();
            SaveMemento(memento, "Auto-save");
        }
    }
    
    public void SaveMemento(T memento, string? label = null)
    {
        caretaker.SaveMemento(memento, label);
    }
    
    public T? RestoreMemento(int index) => caretaker.RestoreMemento(index);
    public T? RestoreLatest() => caretaker.RestoreLatest();
    public IReadOnlyList<T> GetHistory() => caretaker.GetHistory();
    public void ClearHistory() => caretaker.ClearHistory();
    
    public void Dispose()
    {
        autoSaveTimer?.Dispose();
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

