namespace Snippets.DesignPatterns.Behavioral.Memento;

/// <summary>
/// Snapshot manager with branching support for complex state management
/// </summary>
public class SnapshotManager<T> where T : IMemento
{
    private readonly Dictionary<string, List<T>> branches = new();
    private string currentBranch = "main";

    public void CreateBranch(string branchName, string? fromBranch = null)
    {
        var sourceBranch = fromBranch ?? currentBranch;

        if (branches.TryGetValue(sourceBranch, out var sourceSnapshots))
        {
            branches[branchName] = new List<T>(sourceSnapshots);
        }
        else
        {
            branches[branchName] = [];
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
            branches[currentBranch] = [];
        }

        branches[currentBranch].Add(memento);
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

    public void PrintBranches()
    {
        Console.WriteLine($"\nBranches (current: {currentBranch}):");
        foreach (var branch in branches)
        {
            var marker = branch.Key == currentBranch ? " *" : "  ";
            Console.WriteLine($"{marker} {branch.Key} ({branch.Value.Count} snapshots)");
        }
    }
}