using Algorithm.GraphAlgorithms;

namespace Algorithm.GraphAlgorithms;

/// <summary>
/// Demonstrates graph algorithms including DFS, BFS, and shortest path finding.
/// </summary>
internal class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine("=== Graph Algorithms Demo ===\n");
        
        DemonstrateTraversalAlgorithms();
        Console.WriteLine();
        
        DemonstrateShortestPath();
        Console.WriteLine();
        
        DemonstrateCycleDetection();
    }
    
    private static void DemonstrateTraversalAlgorithms()
    {
        Console.WriteLine("--- Graph Traversal Algorithms Demo ---");
        
        var graph = new Graph();
        
        // Create a sample graph
        //     1
        //   / | \
        //  2  3  4
        //  |     |
        //  5     6
        graph.AddEdge(1, 2);
        graph.AddEdge(1, 3);
        graph.AddEdge(1, 4);
        graph.AddEdge(2, 5);
        graph.AddEdge(4, 6);
        
        Console.WriteLine("Graph structure:");
        Console.WriteLine("    1");
        Console.WriteLine("  / | \\");
        Console.WriteLine(" 2  3  4");
        Console.WriteLine(" |     |");
        Console.WriteLine(" 5     6");
        Console.WriteLine();
        
        var dfsResult = graph.DFS(1);
        var dfsRecursiveResult = graph.DFSRecursive(1);
        var bfsResult = graph.BFS(1);
        
        Console.WriteLine($"DFS (Iterative) from vertex 1: {string.Join(" -> ", dfsResult)}");
        Console.WriteLine($"DFS (Recursive) from vertex 1: {string.Join(" -> ", dfsRecursiveResult)}");
        Console.WriteLine($"BFS from vertex 1: {string.Join(" -> ", bfsResult)}");
    }
    
    private static void DemonstrateShortestPath()
    {
        Console.WriteLine("--- Shortest Path Demo ---");
        
        var graph = new Graph();
        
        // Create a more complex undirected graph
        //   1 --- 2
        //   |     |
        //   3 --- 4 --- 5
        //         |
        //         6
        graph.AddUndirectedEdge(1, 2);
        graph.AddUndirectedEdge(1, 3);
        graph.AddUndirectedEdge(2, 4);
        graph.AddUndirectedEdge(3, 4);
        graph.AddUndirectedEdge(4, 5);
        graph.AddUndirectedEdge(4, 6);
        
        Console.WriteLine("Graph structure (undirected):");
        Console.WriteLine("  1 --- 2");
        Console.WriteLine("  |     |");
        Console.WriteLine("  3 --- 4 --- 5");
        Console.WriteLine("        |");
        Console.WriteLine("        6");
        Console.WriteLine();
        
        // Find shortest paths
        var path1to5 = graph.FindShortestPath(1, 5);
        var path1to6 = graph.FindShortestPath(1, 6);
        var path2to3 = graph.FindShortestPath(2, 3);
        
        if (path1to5 != null)
            Console.WriteLine($"Shortest path from 1 to 5: {string.Join(" -> ", path1to5)}");
        else
            Console.WriteLine("No path found from 1 to 5");
            
        if (path1to6 != null)
            Console.WriteLine($"Shortest path from 1 to 6: {string.Join(" -> ", path1to6)}");
        else
            Console.WriteLine("No path found from 1 to 6");
            
        if (path2to3 != null)
            Console.WriteLine($"Shortest path from 2 to 3: {string.Join(" -> ", path2to3)}");
        else
            Console.WriteLine("No path found from 2 to 3");
    }
    
    private static void DemonstrateCycleDetection()
    {
        Console.WriteLine("--- Cycle Detection Demo ---");
        
        // Graph without cycle
        var acyclicGraph = new Graph();
        acyclicGraph.AddEdge(1, 2);
        acyclicGraph.AddEdge(1, 3);
        acyclicGraph.AddEdge(2, 4);
        acyclicGraph.AddEdge(3, 4);
        
        Console.WriteLine("Acyclic graph structure:");
        Console.WriteLine("  1");
        Console.WriteLine(" / \\");
        Console.WriteLine("2   3");
        Console.WriteLine(" \\ /");
        Console.WriteLine("  4");
        Console.WriteLine($"Has cycle: {acyclicGraph.HasCycle()}");
        Console.WriteLine();
        
        // Graph with cycle
        var cyclicGraph = new Graph();
        cyclicGraph.AddEdge(1, 2);
        cyclicGraph.AddEdge(2, 3);
        cyclicGraph.AddEdge(3, 4);
        cyclicGraph.AddEdge(4, 2); // Creates a cycle: 2 -> 3 -> 4 -> 2
        
        Console.WriteLine("Cyclic graph structure:");
        Console.WriteLine("1 -> 2 -> 3");
        Console.WriteLine("     ^    |");
        Console.WriteLine("     |    v");
        Console.WriteLine("     4 <--");
        Console.WriteLine($"Has cycle: {cyclicGraph.HasCycle()}");
    }
}