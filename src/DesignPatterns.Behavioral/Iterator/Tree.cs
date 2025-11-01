namespace Snippets.DesignPatterns.Behavioral.Iterator;

public class Tree<T>(T rootValue) : IIterable<T>
{
    public TreeNode<T>? Root { get; private set; } = new(rootValue);

    public IIterator<T> CreateIterator()
    {
        return new TreeIterator<T>(Root!);
    }

    // Breadth-First traversal using yield
    public IEnumerable<T> BreadthFirst()
    {
        if (Root == null) yield break;

        var queue = new Queue<TreeNode<T>>();
        queue.Enqueue(Root);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            yield return current.Value;

            foreach (var child in current.Children)
            {
                queue.Enqueue(child);
            }
        }
    }

    // Depth-First traversal using yield
    public IEnumerable<T> DepthFirst()
    {
        if (Root == null) yield break;

        var stack = new Stack<TreeNode<T>>();
        stack.Push(Root);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            yield return current.Value;

            // Push children in reverse order to maintain left-to-right traversal
            for (int i = current.Children.Count - 1; i >= 0; i--)
            {
                stack.Push(current.Children[i]);
            }
        }
    }
}