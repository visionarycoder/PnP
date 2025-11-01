using Algorithm.DataStructures;

namespace Algorithm.DataStructures;

/// <summary>
/// Demonstrates usage of custom data structure implementations.
/// </summary>
internal class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine("=== Custom Data Structures Demo ===\n");
        
        DemonstrateStack();
        Console.WriteLine();
        
        DemonstrateQueue();
        Console.WriteLine();
        
        DemonstrateLinkedList();
    }
    
    private static void DemonstrateStack()
    {
        Console.WriteLine("--- Custom Stack Demo ---");
        
        var stack = new CustomStack<int>();
        
        // Push elements
        stack.Push(1);
        stack.Push(2);
        stack.Push(3);
        
        Console.WriteLine($"Peek: {stack.Peek()}"); // Output: 3
        Console.WriteLine($"Pop: {stack.Pop()}");   // Output: 3
        Console.WriteLine($"Count: {stack.Count}"); // Output: 2
        
        // Demonstrate enumeration (LIFO order)
        Console.Write("Stack contents: ");
        foreach (var item in stack)
            Console.Write($"{item} ");
        Console.WriteLine();
        
        // Demonstrate TryPop
        if (stack.TryPop(out var value))
            Console.WriteLine($"TryPop successful: {value}");
    }
    
    private static void DemonstrateQueue()
    {
        Console.WriteLine("--- Custom Queue Demo ---");
        
        var queue = new CustomQueue<string>();
        
        // Enqueue elements
        queue.Enqueue("first");
        queue.Enqueue("second");
        queue.Enqueue("third");
        
        Console.WriteLine($"Peek: {queue.Peek()}");       // Output: first
        Console.WriteLine($"Dequeue: {queue.Dequeue()}"); // Output: first
        Console.WriteLine($"Count: {queue.Count}");       // Output: 2
        
        // Demonstrate enumeration (FIFO order)
        Console.Write("Queue contents: ");
        foreach (var item in queue)
            Console.Write($"{item} ");
        Console.WriteLine();
        
        // Demonstrate TryDequeue
        if (queue.TryDequeue(out var value))
            Console.WriteLine($"TryDequeue successful: {value}");
    }
    
    private static void DemonstrateLinkedList()
    {
        Console.WriteLine("--- Custom Linked List Demo ---");
        
        var list = new CustomLinkedList<int>();
        
        // Add elements
        list.AddFirst(2);
        list.AddFirst(1);
        list.AddLast(3);
        
        Console.Write("List contents: ");
        foreach (var value in list)
            Console.Write($"{value} "); // Output: 1 2 3
        Console.WriteLine();
        
        Console.WriteLine($"Contains 2: {list.Contains(2)}"); // Output: True
        Console.WriteLine($"Contains 5: {list.Contains(5)}"); // Output: False
        
        Console.WriteLine($"Count: {list.Count}");
        
        // Remove first element
        list.RemoveFirst();
        Console.Write("After removing first: ");
        foreach (var value in list)
            Console.Write($"{value} "); // Output: 2 3
        Console.WriteLine();
    }
}