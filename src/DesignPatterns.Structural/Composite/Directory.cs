namespace Snippets.DesignPatterns.Structural.Composite;

public class Directory(string name) 
    : FileSystemComponent(name)
{

    private readonly List<FileSystemComponent> children = [];

    public Directory? Parent { get; private set; }

    public IReadOnlyList<FileSystemComponent> Children => children.AsReadOnly();

    public override long GetSize() => children.Sum(child => child.GetSize());

    public override int GetCount() => children.Sum(child => child.GetCount());

    public override void Display(int depth = 0)
    {
        var indent = GetIndentation(depth);
        var sizeStr = GetSize() < 1024
            ? $"{GetSize()}B"
            : GetSize() < 1024 * 1024
                ? $"{GetSize() / 1024.0:F1}KB"
                : $"{GetSize() / (1024.0 * 1024.0):F1}MB";

        Console.WriteLine($"{indent}📁 {Name}/ ({GetCount()} items, {sizeStr})");

        foreach (var child in children)
        {
            child.Display(depth + 1);
        }
    }

    public override string GetPath()
    {
        return Parent == null
            ? Name
            : $"{Parent.GetPath()}/{Name}";
    }

    public override void Add(FileSystemComponent component)
    {

        ArgumentNullException.ThrowIfNull(component);

        if (component == this)
            throw new ArgumentException("Cannot add directory to itself");

        // Check for circular reference
        if (component is Directory dir && IsAncestor(dir))
            throw new ArgumentException("Cannot create circular reference");

        children.Add(component);

        if (component is Directory childDir)
            childDir.Parent = this;

        UpdateModified();
    }

    public override void Remove(FileSystemComponent component)
    {
        if (!children.Remove(component))
            return;

        if (component is Directory dir)
            dir.Parent = null;


        UpdateModified();
    }

    public override FileSystemComponent GetChild(int index)
    {
        if (index < 0 || index >= children.Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        return children[index];
    }

    public override IEnumerable<FileSystemComponent> GetChildren()
    {
        return children.AsEnumerable();
    }

    private bool IsAncestor(Directory potentialAncestor)
    {
        var current = Parent;
        while (current != null)
        {
            if (current == potentialAncestor)
                return true;
            current = current.Parent;
        }

        return false;
    }

    public FileSystemComponent? Find(string name)
    {

        if (Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            return this;

        foreach (var child in children)
        {
            if (child.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                return child;

            if (child is Directory dir)
            {
                var found = dir.Find(name);
                if (found != null)
                    return found;
            }
        }

        return null;
    }

    public IEnumerable<File> GetAllFiles()
    {
        foreach (var child in children)
        {
            switch (child)
            {
                case File file:
                    yield return file;
                    break;
                case Directory dir:
                {
                    foreach (var nestedFile in dir.GetAllFiles())
                    {
                        yield return nestedFile;
                    }
                    break;
                }
            }
        }
    }

    public override FileSystemComponent Clone()
    {
        var clonedDir = new Directory(Name);

        foreach (var child in children)
        {
            clonedDir.Add(child.Clone());
        }

        return clonedDir;
    }
}