namespace Snippets.DesignPatterns.Structural.Composite;


// Component - declares interface for objects in the composition
public abstract class FileSystemComponent(string name)
{

    public string Name { get; } = name ?? throw new ArgumentNullException(nameof(name));
    public DateTime Created { get; } = DateTime.Now;
    public DateTime Modified { get; protected set; } = DateTime.Now;


    public abstract long GetSize();
    public abstract int GetCount();
    public abstract void Display(int depth = 0);
    public abstract string GetPath();

    public abstract FileSystemComponent Clone();

    // Default implementations for operations that may not apply to all components
    public virtual void Add(FileSystemComponent component)
    {
        throw new NotSupportedException($"Cannot add components to {GetType().Name}");
    }

    public virtual void Remove(FileSystemComponent component)
    {
        throw new NotSupportedException($"Cannot remove components from {GetType().Name}");
    }

    public virtual FileSystemComponent? GetChild(int index)
    {
        throw new NotSupportedException($"Cannot get children from {GetType().Name}");
    }

    public virtual IEnumerable<FileSystemComponent> GetChildren()
    {
        return [];
    }

    protected void UpdateModified()
    {
        Modified = DateTime.Now;
    }

    protected string GetIndentation(int depth)
    {
        return new string(' ', depth * 2);
    }

}