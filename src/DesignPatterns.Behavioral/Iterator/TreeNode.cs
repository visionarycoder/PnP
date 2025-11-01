namespace Snippets.DesignPatterns.Behavioral.Iterator;

public class TreeNode<T>(T value)
{
    public T Value { get; set; } = value;
    public List<TreeNode<T>> Children { get; } = [];

    public void AddChild(TreeNode<T> child)
    {
        Children.Add(child);
    }

    public void AddChild(T value)
    {
        Children.Add(new TreeNode<T>(value));
    }
}