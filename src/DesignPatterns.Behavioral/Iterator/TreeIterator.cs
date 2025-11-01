namespace Snippets.DesignPatterns.Behavioral.Iterator;

public class TreeIterator<T> : IIterator<T>
{
    private readonly Stack<TreeNode<T>> stack = new();
    private TreeNode<T>? current;

    public TreeIterator(TreeNode<T> root)
    {
        if (root != null)
        {
            stack.Push(root);
        }
    }

    public bool HasNext()
    {
        return stack.Count > 0;
    }

    public T Next()
    {
        if (!HasNext())
        {
            throw new InvalidOperationException("No more elements");
        }

        current = stack.Pop();

        // Add children in reverse order to maintain left-to-right traversal
        for (int i = current.Children.Count - 1; i >= 0; i--)
        {
            stack.Push(current.Children[i]);
        }

        return current.Value;
    }

    public void Reset()
    {
        throw new NotSupportedException("Tree iterator reset is not supported in this implementation");
    }

    public T Current => current != null
        ? current.Value
        : throw new InvalidOperationException("Iterator is not positioned on a valid element");
}