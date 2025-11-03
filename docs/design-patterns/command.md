# Command Pattern

**Description**: Encapsulates a request as an object, thereby allowing you to parameterize clients with different requests, queue or log requests, and support undoable operations. The pattern decouples the sender of a request from the receiver by packaging the request as a command object.

**Language/Technology**: C#

**Code**:

## 1. Basic Command Structure

```csharp
// Command interface
public interface ICommand
{
    void Execute();
    void Undo();
    string Description { get; }
}

// Receiver - performs the actual work
public class TextEditor
{
    private readonly StringBuilder content = new();
    private int cursorPosition = 0;
    
    public string Content => content.ToString();
    public int CursorPosition => cursorPosition;
    
    public void InsertText(string text, int position)
    {
        if (position < 0 || position > content.Length)
            throw new ArgumentOutOfRangeException(nameof(position));
            
        content.Insert(position, text);
        cursorPosition = position + text.Length;
        Console.WriteLine($"Inserted '{text}' at position {position}");
    }
    
    public string DeleteText(int startPosition, int length)
    {
        if (startPosition < 0 || startPosition + length > content.Length)
            throw new ArgumentOutOfRangeException();
            
        var deletedText = content.ToString(startPosition, length);
        content.Remove(startPosition, length);
        cursorPosition = startPosition;
        Console.WriteLine($"Deleted '{deletedText}' from position {startPosition}");
        return deletedText;
    }
    
    public void MoveCursor(int newPosition)
    {
        if (newPosition < 0 || newPosition > content.Length)
            throw new ArgumentOutOfRangeException(nameof(newPosition));
            
        var oldPosition = cursorPosition;
        cursorPosition = newPosition;
        Console.WriteLine($"Moved cursor from {oldPosition} to {newPosition}");
    }
    
    public void Clear()
    {
        content.Clear();
        cursorPosition = 0;
    }
    
    public override string ToString() => $"Content: '{Content}', Cursor: {CursorPosition}";
}
```

## 2. Concrete Commands

```csharp
// Insert text command
public class InsertTextCommand : ICommand
{
    private readonly TextEditor editor;
    private readonly string text;
    private readonly int position;
    
    public string Description { get; }
    
    public InsertTextCommand(TextEditor editor, string text, int position)
    {
        editor = editor;
        text = text;
        position = position;
        Description = $"Insert '{text}' at position {position}";
    }
    
    public void Execute()
    {
        editor.InsertText(text, position);
    }
    
    public void Undo()
    {
        editor.DeleteText(position, text.Length);
    }
}

// Delete text command
public class DeleteTextCommand : ICommand
{
    private readonly TextEditor editor;
    private readonly int startPosition;
    private readonly int length;
    private string deletedText = "";
    
    public string Description { get; }
    
    public DeleteTextCommand(TextEditor editor, int startPosition, int length)
    {
        editor = editor;
        startPosition = startPosition;
        length = length;
        Description = $"Delete {length} characters from position {startPosition}";
    }
    
    public void Execute()
    {
        deletedText = editor.DeleteText(startPosition, length);
    }
    
    public void Undo()
    {
        editor.InsertText(deletedText, startPosition);
    }
}

// Move cursor command
public class MoveCursorCommand : ICommand
{
    private readonly TextEditor editor;
    private readonly int newPosition;
    private int oldPosition;
    
    public string Description { get; }
    
    public MoveCursorCommand(TextEditor editor, int newPosition)
    {
        editor = editor;
        newPosition = newPosition;
        Description = $"Move cursor to position {newPosition}";
    }
    
    public void Execute()
    {
        oldPosition = editor.CursorPosition;
        editor.MoveCursor(newPosition);
    }
    
    public void Undo()
    {
        editor.MoveCursor(oldPosition);
    }
}

// Composite command for complex operations
public class CompositeCommand : ICommand
{
    private readonly List<ICommand> commands = new();
    
    public string Description { get; private set; }
    
    public CompositeCommand(string description)
    {
        Description = description;
    }
    
    public void AddCommand(ICommand command)
    {
        commands.Add(command);
    }
    
    public void Execute()
    {
        Console.WriteLine($"Executing composite command: {Description}");
        foreach (var command in commands)
        {
            command.Execute();
        }
    }
    
    public void Undo()
    {
        Console.WriteLine($"Undoing composite command: {Description}");
        // Undo in reverse order
        for (int i = commands.Count - 1; i >= 0; i--)
        {
            commands[i].Undo();
        }
    }
}
```

## 3. Command Manager (Invoker)

```csharp
public class CommandManager
{
    private readonly Stack<ICommand> undoStack = new();
    private readonly Stack<ICommand> redoStack = new();
    private readonly List<ICommand> commandHistory = new();
    
    public IReadOnlyList<ICommand> CommandHistory => commandHistory.AsReadOnly();
    public bool CanUndo => undoStack.Count > 0;
    public bool CanRedo => redoStack.Count > 0;
    
    public void ExecuteCommand(ICommand command)
    {
        try
        {
            command.Execute();
            undoStack.Push(command);
            commandHistory.Add(command);
            redoStack.Clear(); // Clear redo stack when new command is executed
            
            Console.WriteLine($"Command executed: {command.Description}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Command execution failed: {ex.Message}");
            throw;
        }
    }
    
    public void Undo()
    {
        if (!CanUndo)
        {
            Console.WriteLine("Nothing to undo");
            return;
        }
        
        var command = undoStack.Pop();
        try
        {
            command.Undo();
            redoStack.Push(command);
            Console.WriteLine($"Command undone: {command.Description}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Undo failed: {ex.Message}");
            undoStack.Push(command); // Put it back if undo fails
            throw;
        }
    }
    
    public void Redo()
    {
        if (!CanRedo)
        {
            Console.WriteLine("Nothing to redo");
            return;
        }
        
        var command = redoStack.Pop();
        try
        {
            command.Execute();
            undoStack.Push(command);
            Console.WriteLine($"Command redone: {command.Description}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Redo failed: {ex.Message}");
            redoStack.Push(command); // Put it back if redo fails
            throw;
        }
    }
    
    public void ClearHistory()
    {
        undoStack.Clear();
        redoStack.Clear();
        commandHistory.Clear();
        Console.WriteLine("Command history cleared");
    }
    
    public void PrintHistory()
    {
        Console.WriteLine("\nCommand History:");
        for (int i = 0; i < commandHistory.Count; i++)
        {
            Console.WriteLine($"{i + 1}. {commandHistory[i].Description}");
        }
        Console.WriteLine($"Can Undo: {CanUndo}, Can Redo: {CanRedo}");
    }
}
```

## 4. Advanced Command Examples

```csharp
// Macro command for recording and replaying sequences
public class MacroCommand : ICommand
{
    private readonly List<ICommand> commands = new();
    private readonly string name;
    
    public string Description => $"Macro: {name} ({commands.Count} commands)";
    
    public MacroCommand(string name)
    {
        name = name;
    }
    
    public void AddCommand(ICommand command)
    {
        commands.Add(command);
    }
    
    public void Execute()
    {
        Console.WriteLine($"Executing macro '{name}':");
        foreach (var command in commands)
        {
            command.Execute();
        }
    }
    
    public void Undo()
    {
        Console.WriteLine($"Undoing macro '{name}':");
        for (int i = commands.Count - 1; i >= 0; i--)
        {
            commands[i].Undo();
        }
    }
    
    public MacroCommand Clone()
    {
        var clone = new MacroCommand(name);
        foreach (var command in commands)
        {
            clone.AddCommand(command);
        }
        return clone;
    }
}

// Remote control example
public interface IDevice
{
    void TurnOn();
    void TurnOff();
    void SetVolume(int volume);
    void ChangeChannel(int channel);
    string Status { get; }
}

public class Television : IDevice
{
    private bool isOn = false;
    private int volume = 50;
    private int channel = 1;
    
    public string Status => $"TV: {(isOn ? "ON" : "OFF")}, Volume: {volume}, Channel: {channel}";
    
    public void TurnOn()
    {
        isOn = true;
        Console.WriteLine("TV turned on");
    }
    
    public void TurnOff()
    {
        isOn = false;
        Console.WriteLine("TV turned off");
    }
    
    public void SetVolume(int volume)
    {
        volume = Math.Clamp(volume, 0, 100);
        Console.WriteLine($"TV volume set to {volume}");
    }
    
    public void ChangeChannel(int channel)
    {
        channel = Math.Max(1, channel);
        Console.WriteLine($"TV channel changed to {channel}");
    }
}

public class Stereo : IDevice
{
    private bool isOn = false;
    private int volume = 30;
    private int channel = 101; // FM frequency
    
    public string Status => $"Stereo: {(isOn ? "ON" : "OFF")}, Volume: {volume}, Station: {channel}";
    
    public void TurnOn()
    {
        isOn = true;
        Console.WriteLine("Stereo turned on");
    }
    
    public void TurnOff()
    {
        isOn = false;
        Console.WriteLine("Stereo turned off");
    }
    
    public void SetVolume(int volume)
    {
        volume = Math.Clamp(volume, 0, 100);
        Console.WriteLine($"Stereo volume set to {volume}");
    }
    
    public void ChangeChannel(int station)
    {
        channel = station;
        Console.WriteLine($"Stereo tuned to station {station}");
    }
}

// Device commands
public class TurnOnCommand : ICommand
{
    private readonly IDevice device;
    private bool wasAlreadyOn;
    
    public string Description { get; }
    
    public TurnOnCommand(IDevice device)
    {
        device = device;
        Description = $"Turn on {device.GetType().Name}";
    }
    
    public void Execute()
    {
        wasAlreadyOn = device.Status.Contains("ON");
        if (!wasAlreadyOn)
        {
            device.TurnOn();
        }
    }
    
    public void Undo()
    {
        if (!wasAlreadyOn)
        {
            device.TurnOff();
        }
    }
}

public class SetVolumeCommand : ICommand
{
    private readonly IDevice device;
    private readonly int newVolume;
    private int previousVolume;
    
    public string Description { get; }
    
    public SetVolumeCommand(IDevice device, int volume)
    {
        device = device;
        newVolume = volume;
        Description = $"Set {device.GetType().Name} volume to {volume}";
    }
    
    public void Execute()
    {
        // Extract current volume from status (simplified)
        var status = device.Status;
        var volumeIndex = status.IndexOf("Volume: ") + 8;
        var volumeEnd = status.IndexOf(",", volumeIndex);
        if (volumeEnd == -1) volumeEnd = status.Length;
        
        if (int.TryParse(status.Substring(volumeIndex, volumeEnd - volumeIndex), out var currentVolume))
        {
            previousVolume = currentVolume;
        }
        
        device.SetVolume(newVolume);
    }
    
    public void Undo()
    {
        device.SetVolume(previousVolume);
    }
}

// Remote control with programmable buttons
public class UniversalRemote
{
    private readonly Dictionary<string, ICommand> commands = new();
    private readonly Dictionary<string, ICommand> undoCommands = new();
    private ICommand? lastCommand;
    
    public void SetCommand(string slot, ICommand command)
    {
        commands[slot] = command;
        Console.WriteLine($"Programmed slot '{slot}': {command.Description}");
    }
    
    public void PressButton(string slot)
    {
        if (commands.TryGetValue(slot, out var command))
        {
            command.Execute();
            lastCommand = command;
            Console.WriteLine($"Button '{slot}' pressed");
        }
        else
        {
            Console.WriteLine($"No command programmed for slot '{slot}'");
        }
    }
    
    public void PressUndo()
    {
        if (lastCommand != null)
        {
            lastCommand.Undo();
            Console.WriteLine("Undo button pressed");
            lastCommand = null;
        }
        else
        {
            Console.WriteLine("No command to undo");
        }
    }
    
    public void PrintRemoteStatus()
    {
        Console.WriteLine("\nRemote Control Status:");
        foreach (var kvp in commands)
        {
            Console.WriteLine($"  {kvp.Key}: {kvp.Value.Description}");
        }
    }
}

// No-op command (Null Object pattern)
public class NoOpCommand : ICommand
{
    public string Description => "No Operation";
    
    public void Execute()
    {
        // Do nothing
    }
    
    public void Undo()
    {
        // Do nothing
    }
}
```

## 5. Queued Command Processing

```csharp
public class CommandQueue
{
    private readonly Queue<ICommand> commandQueue = new();
    private readonly object lock = new();
    private readonly CancellationTokenSource cancellationTokenSource = new();
    private readonly Task processingTask;
    
    public CommandQueue()
    {
        processingTask = Task.Run(ProcessCommands, cancellationTokenSource.Token);
    }
    
    public void EnqueueCommand(ICommand command)
    {
        lock (lock)
        {
            commandQueue.Enqueue(command);
            Console.WriteLine($"Command queued: {command.Description}");
        }
    }
    
    public void EnqueueCommands(IEnumerable<ICommand> commands)
    {
        lock (lock)
        {
            foreach (var command in commands)
            {
                commandQueue.Enqueue(command);
            }
            Console.WriteLine($"Batch of {commands.Count()} commands queued");
        }
    }
    
    private async Task ProcessCommands()
    {
        while (!cancellationTokenSource.Token.IsCancellationRequested)
        {
            ICommand? command = null;
            
            lock (lock)
            {
                if (commandQueue.Count > 0)
                {
                    command = commandQueue.Dequeue();
                }
            }
            
            if (command != null)
            {
                try
                {
                    Console.WriteLine($"Processing queued command: {command.Description}");
                    command.Execute();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing command: {ex.Message}");
                }
            }
            else
            {
                await Task.Delay(100, cancellationTokenSource.Token);
            }
        }
    }
    
    public void Stop()
    {
        cancellationTokenSource.Cancel();
        processingTask.Wait(5000);
    }
    
    public int QueuedCommandsCount
    {
        get
        {
            lock (lock)
            {
                return commandQueue.Count;
            }
        }
    }
}

// Command with priority
public class PriorityCommand : ICommand
{
    private readonly ICommand innerCommand;
    public int Priority { get; }
    public string Description => $"{innerCommand.Description} (Priority: {Priority})";
    
    public PriorityCommand(ICommand command, int priority)
    {
        innerCommand = command;
        Priority = priority;
    }
    
    public void Execute() => innerCommand.Execute();
    public void Undo() => innerCommand.Undo();
}

public class PriorityCommandQueue
{
    private readonly PriorityQueue<ICommand, int> priorityQueue = new();
    private readonly object lock = new();
    
    public void EnqueueCommand(ICommand command, int priority = 0)
    {
        lock (lock)
        {
            priorityQueue.Enqueue(command, -priority); // Negative for highest priority first
            Console.WriteLine($"Priority command queued: {command.Description} (Priority: {priority})");
        }
    }
    
    public ICommand? DequeueCommand()
    {
        lock (lock)
        {
            return priorityQueue.TryDequeue(out var command, out _) ? command : null;
        }
    }
    
    public bool HasCommands => priorityQueue.Count > 0;
}
```

**Usage**:

```csharp
// 1. Basic Text Editor Commands
var editor = new TextEditor();
var commandManager = new CommandManager();

// Execute some commands
commandManager.ExecuteCommand(new InsertTextCommand(editor, "Hello", 0));
commandManager.ExecuteCommand(new InsertTextCommand(editor, " World", 5));
commandManager.ExecuteCommand(new MoveCursorCommand(editor, 6));
commandManager.ExecuteCommand(new InsertTextCommand(editor, "Beautiful ", 6));

Console.WriteLine($"Editor state: {editor}");

// Undo operations
commandManager.Undo(); // Undo "Beautiful "
commandManager.Undo(); // Undo cursor move
Console.WriteLine($"After undo: {editor}");

// Redo operations
commandManager.Redo(); // Redo cursor move
commandManager.Redo(); // Redo "Beautiful "
Console.WriteLine($"After redo: {editor}");

commandManager.PrintHistory();

// 2. Composite Command Example
var compositeCommand = new CompositeCommand("Format Text");
compositeCommand.AddCommand(new MoveCursorCommand(editor, 0));
compositeCommand.AddCommand(new InsertTextCommand(editor, "*** ", 0));
compositeCommand.AddCommand(new MoveCursorCommand(editor, editor.Content.Length));
compositeCommand.AddCommand(new InsertTextCommand(editor, " ***", editor.Content.Length));

commandManager.ExecuteCommand(compositeCommand);
Console.WriteLine($"After composite command: {editor}");

commandManager.Undo(); // Undo entire composite operation
Console.WriteLine($"After undoing composite: {editor}");

// 3. Macro Recording Example
var macro = new MacroCommand("Insert Greeting");
macro.AddCommand(new InsertTextCommand(editor, "Dear Sir/Madam,\n\n", editor.CursorPosition));
macro.AddCommand(new InsertTextCommand(editor, "Thank you for your interest.\n\n", editor.CursorPosition + 16));
macro.AddCommand(new InsertTextCommand(editor, "Best regards,\n", editor.CursorPosition + 50));

commandManager.ExecuteCommand(macro);
Console.WriteLine($"After macro: {editor}");

// 4. Remote Control Example
var tv = new Television();
var stereo = new Stereo();
var remote = new UniversalRemote();

// Program remote buttons
remote.SetCommand("Power TV", new TurnOnCommand(tv));
remote.SetCommand("Power Stereo", new TurnOnCommand(stereo));
remote.SetCommand("TV Vol+", new SetVolumeCommand(tv, 75));
remote.SetCommand("Stereo Vol+", new SetVolumeCommand(stereo, 60));

// Use remote
remote.PressButton("Power TV");
remote.PressButton("TV Vol+");
Console.WriteLine(tv.Status);

remote.PressUndo(); // Undo volume change
Console.WriteLine(tv.Status);

remote.PrintRemoteStatus();

// 5. Command Queue Example
var commandQueue = new CommandQueue();

// Queue commands for batch processing
commandQueue.EnqueueCommand(new InsertTextCommand(editor, "\n\nQueued text 1", editor.Content.Length));
commandQueue.EnqueueCommand(new InsertTextCommand(editor, "\nQueued text 2", editor.Content.Length + 15));
commandQueue.EnqueueCommand(new InsertTextCommand(editor, "\nQueued text 3", editor.Content.Length + 30));

// Wait for processing
await Task.Delay(2000);
Console.WriteLine($"After queued commands: {editor}");

commandQueue.Stop();

// Expected output demonstrates:
// - Command execution and undo/redo functionality
// - Composite commands for complex operations
// - Macro recording and replay
// - Remote control programming with different devices
// - Queued command processing for batch operations
// - Command history tracking and management
```

**Notes**:

- **Decoupling**: Separates the object that invokes the operation from the object that performs it
- **Undo/Redo**: Essential for applications requiring operation history and reversal
- **Macro Recording**: Commands can be stored and replayed as sequences
- **Queuing**: Commands can be scheduled for later execution or batch processing
- **Logging**: Command history provides audit trail of all operations
- **Threading**: Command queues enable asynchronous processing
- **Parameterization**: Different commands can be assigned to the same invoker (button, menu item)
- **Composite Commands**: Complex operations built from simpler commands
- **Null Object**: NoOpCommand provides safe default behavior for unassigned slots

**Prerequisites**:

- .NET 6.0 or later
- Understanding of interfaces and polymorphism
- Knowledge of the Strategy pattern (commands encapsulate algorithms)
- Familiarity with async/await for queued processing

**Related Patterns**:

- **Memento**: Often used together to store command state for undo operations
- **Composite**: Macro commands use Composite pattern to group commands
- **Strategy**: Commands encapsulate different strategies/algorithms
- **Observer**: Command execution can trigger notifications to observers
