# Scalable Graph Processing Algorithms

**Description**: Enterprise-scale graph algorithms with memory-efficient traversal, parallel processing, and distributed graph operations for large-scale network analysis and pathfinding in enterprise applications.
**Language/Technology**: C#, .NET 9.0, Parallel Processing, Distributed Computing
**Performance Complexity**: Memory-optimized algorithms with parallel execution and cache-aware implementations
**Enterprise Features**: Large-scale graph processing, distributed traversal, memory-efficient storage, and real-time analytics integration

## Depth-First Search (DFS)

**Code**:

```csharp
using System;
using System.Collections.Generic;

public class Graph
{
    private Dictionary<int, List<int>> adjacencyList;
    
    public Graph()
    {
        adjacencyList = new Dictionary<int, List<int>>();
    }
    
    public void AddEdge(int source, int destination)
    {
        if (!adjacencyList.ContainsKey(source))
            adjacencyList[source] = new List<int>();
        if (!adjacencyList.ContainsKey(destination))
            adjacencyList[destination] = new List<int>();
            
        adjacencyList[source].Add(destination);
    }
    
    public void DFS(int startVertex)
    {
        var visited = new HashSet<int>();
        var stack = new Stack<int>();
        
        stack.Push(startVertex);
        
        while (stack.Count > 0)
        {
            int vertex = stack.Pop();
            
            if (!visited.Contains(vertex))
            {
                visited.Add(vertex);
                Console.Write(vertex + " ");
                
                if (adjacencyList.ContainsKey(vertex))
                {
                    // Add neighbors in reverse order to maintain left-to-right traversal
                    for (int i = adjacencyList[vertex].Count - 1; i >= 0; i--)
                    {
                        int neighbor = adjacencyList[vertex][i];
                        if (!visited.Contains(neighbor))
                            stack.Push(neighbor);
                    }
                }
            }
        }
    }
    
    public void DFSRecursive(int vertex, HashSet<int> visited = null)
    {
        if (visited == null)
            visited = new HashSet<int>();
            
        visited.Add(vertex);
        Console.Write(vertex + " ");
        
        if (adjacencyList.ContainsKey(vertex))
        {
            foreach (int neighbor in adjacencyList[vertex])
            {
                if (!visited.Contains(neighbor))
                    DFSRecursive(neighbor, visited);
            }
        }
    }
}
```

## Breadth-First Search (BFS)

**Description**: BFS explores nodes level by level, visiting all neighbors of a node before moving to the next level. It guarantees the shortest path in unweighted graphs.

**Code**:

```csharp
public void BFS(int startVertex)
{
    var visited = new HashSet<int>();
    var queue = new Queue<int>();
    
    visited.Add(startVertex);
    queue.Enqueue(startVertex);
    
    while (queue.Count > 0)
    {
        int vertex = queue.Dequeue();
        Console.Write(vertex + " ");
        
        if (adjacencyList.ContainsKey(vertex))
        {
            foreach (int neighbor in adjacencyList[vertex])
            {
                if (!visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
        }
    }
}
```

## Dijkstra's Shortest Path Algorithm

**Description**: Dijkstra's algorithm finds the shortest paths from a source vertex to all other vertices in a weighted graph with non-negative edge weights. It uses a greedy approach with a priority queue.

**Code**:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;

public class WeightedGraph
{
    private Dictionary<int, List<(int neighbor, int weight)>> adjacencyList;
    
    public WeightedGraph()
    {
        adjacencyList = new Dictionary<int, List<(int, int)>>();
    }
    
    public void AddEdge(int source, int destination, int weight)
    {
        if (!adjacencyList.ContainsKey(source))
            adjacencyList[source] = new List<(int, int)>();
        if (!adjacencyList.ContainsKey(destination))
            adjacencyList[destination] = new List<(int, int)>();
            
        adjacencyList[source].Add((destination, weight));
    }
    
    public Dictionary<int, int> Dijkstra(int startVertex)
    {
        var distances = new Dictionary<int, int>();
        var visited = new HashSet<int>();
        var priorityQueue = new SortedDictionary<int, List<int>>();
        
        // Initialize distances
        foreach (var vertex in adjacencyList.Keys)
        {
            distances[vertex] = int.MaxValue;
        }
        distances[startVertex] = 0;
        
        // Add start vertex to priority queue
        if (!priorityQueue.ContainsKey(0))
            priorityQueue[0] = new List<int>();
        priorityQueue[0].Add(startVertex);
        
        while (priorityQueue.Count > 0)
        {
            // Get vertex with minimum distance
            var minDistance = priorityQueue.Keys.First();
            var vertex = priorityQueue[minDistance][0];
            priorityQueue[minDistance].RemoveAt(0);
            
            if (priorityQueue[minDistance].Count == 0)
                priorityQueue.Remove(minDistance);
            
            if (visited.Contains(vertex))
                continue;
                
            visited.Add(vertex);
            
            // Update distances to neighbors
            if (adjacencyList.ContainsKey(vertex))
            {
                foreach (var (neighbor, weight) in adjacencyList[vertex])
                {
                    if (!visited.Contains(neighbor))
                    {
                        int newDistance = distances[vertex] + weight;
                        if (newDistance < distances[neighbor])
                        {
                            distances[neighbor] = newDistance;
                            
                            if (!priorityQueue.ContainsKey(newDistance))
                                priorityQueue[newDistance] = new List<int>();
                            priorityQueue[newDistance].Add(neighbor);
                        }
                    }
                }
            }
        }
        
        return distances;
    }
}
```

**Usage**:

```csharp
// Create and populate graph
var graph = new Graph();
graph.AddEdge(0, 1);
graph.AddEdge(0, 2);
graph.AddEdge(1, 3);
graph.AddEdge(2, 4);

Console.WriteLine("DFS traversal:");
graph.DFS(0); // Output: 0 1 3 2 4

Console.WriteLine("\nDFS Recursive:");
graph.DFSRecursive(0); // Output: 0 1 3 2 4

Console.WriteLine("\nBFS traversal:");
graph.BFS(0); // Output: 0 1 2 3 4

// Weighted graph example
var weightedGraph = new WeightedGraph();
weightedGraph.AddEdge(0, 1, 4);
weightedGraph.AddEdge(0, 2, 2);
weightedGraph.AddEdge(1, 3, 1);
weightedGraph.AddEdge(2, 3, 5);

var distances = weightedGraph.Dijkstra(0);
foreach (var kvp in distances)
{
    Console.WriteLine($"Distance to {kvp.Key}: {kvp.Value}");
}
```

## A* (A-Star) Pathfinding Algorithm

**Description**: A* is a graph traversal and path search algorithm that uses heuristics to find the optimal path more efficiently than Dijkstra's algorithm. It combines the actual distance from the start (g-score) with a heuristic estimate to the goal (h-score).

**Code**:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;

public class AStarNode
{
    public int X { get; set; }
    public int Y { get; set; }
    public float GScore { get; set; } // Distance from start
    public float HScore { get; set; } // Heuristic distance to goal
    public float FScore => GScore + HScore; // Total cost
    public AStarNode Parent { get; set; }
    
    public AStarNode(int x, int y)
    {
        X = x;
        Y = y;
        GScore = float.MaxValue;
        HScore = 0;
        Parent = null;
    }
    
    public override bool Equals(object obj)
    {
        if (obj is AStarNode node)
            return X == node.X && Y == node.Y;
        return false;
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }
}

public static class AStar
{
    public static List<AStarNode> FindPath(bool[,] grid, AStarNode start, AStarNode goal)
    {
        var openSet = new HashSet<AStarNode> { start };
        var closedSet = new HashSet<AStarNode>();
        
        start.GScore = 0;
        start.HScore = ManhattanDistance(start, goal);
        
        while (openSet.Count > 0)
        {
            // Find node with lowest F score
            var current = openSet.OrderBy(n => n.FScore).First();
            
            if (current.Equals(goal))
            {
                return ReconstructPath(current);
            }
            
            openSet.Remove(current);
            closedSet.Add(current);
            
            foreach (var neighbor in GetNeighbors(grid, current))
            {
                if (closedSet.Contains(neighbor))
                    continue;
                
                float tentativeGScore = current.GScore + 1; // Assuming unit cost
                
                if (!openSet.Contains(neighbor))
                {
                    openSet.Add(neighbor);
                }
                else if (tentativeGScore >= neighbor.GScore)
                {
                    continue;
                }
                
                neighbor.Parent = current;
                neighbor.GScore = tentativeGScore;
                neighbor.HScore = ManhattanDistance(neighbor, goal);
            }
        }
        
        return new List<AStarNode>(); // No path found
    }
    
    private static float ManhattanDistance(AStarNode a, AStarNode b)
    {
        return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
    }
    
    private static List<AStarNode> GetNeighbors(bool[,] grid, AStarNode node)
    {
        var neighbors = new List<AStarNode>();
        int[] dx = { -1, 1, 0, 0 };
        int[] dy = { 0, 0, -1, 1 };
        
        for (int i = 0; i < 4; i++)
        {
            int newX = node.X + dx[i];
            int newY = node.Y + dy[i];
            
            if (newX >= 0 && newX < grid.GetLength(0) && 
                newY >= 0 && newY < grid.GetLength(1) && 
                grid[newX, newY])
            {
                neighbors.Add(new AStarNode(newX, newY));
            }
        }
        
        return neighbors;
    }
    
    private static List<AStarNode> ReconstructPath(AStarNode node)
    {
        var path = new List<AStarNode>();
        while (node != null)
        {
            path.Add(node);
            node = node.Parent;
        }
        path.Reverse();
        return path;
    }
}
```

## Bellman-Ford Algorithm

**Description**: The Bellman-Ford algorithm finds shortest paths from a source vertex to all other vertices in a weighted graph. Unlike Dijkstra's algorithm, it can handle graphs with negative edge weights and detect negative cycles.

**Code**:

```csharp
public class Edge
{
    public int Source { get; set; }
    public int Destination { get; set; }
    public int Weight { get; set; }
    
    public Edge(int source, int destination, int weight)
    {
        Source = source;
        Destination = destination;
        Weight = weight;
    }
}

public static class BellmanFord
{
    public static Dictionary<int, int> FindShortestPaths(List<Edge> edges, int vertexCount, int source)
    {
        var distances = new Dictionary<int, int>();
        
        // Initialize distances
        for (int i = 0; i < vertexCount; i++)
        {
            distances[i] = int.MaxValue;
        }
        distances[source] = 0;
        
        // Relax edges V-1 times
        for (int i = 0; i < vertexCount - 1; i++)
        {
            foreach (var edge in edges)
            {
                if (distances[edge.Source] != int.MaxValue &&
                    distances[edge.Source] + edge.Weight < distances[edge.Destination])
                {
                    distances[edge.Destination] = distances[edge.Source] + edge.Weight;
                }
            }
        }
        
        // Check for negative cycles
        foreach (var edge in edges)
        {
            if (distances[edge.Source] != int.MaxValue &&
                distances[edge.Source] + edge.Weight < distances[edge.Destination])
            {
                throw new InvalidOperationException("Graph contains negative cycle");
            }
        }
        
        return distances;
    }
}
```

## Floyd-Warshall Algorithm

**Description**: The Floyd-Warshall algorithm finds shortest paths between all pairs of vertices in a weighted graph. It can handle negative weights but not negative cycles.

**Code**:

```csharp
public static class FloydWarshall
{
    public static int[,] AllPairsShortestPath(int[,] graph)
    {
        int vertexCount = graph.GetLength(0);
        int[,] distances = new int[vertexCount, vertexCount];
        
        // Initialize distances array
        for (int i = 0; i < vertexCount; i++)
        {
            for (int j = 0; j < vertexCount; j++)
            {
                distances[i, j] = graph[i, j];
            }
        }
        
        // Floyd-Warshall algorithm
        for (int k = 0; k < vertexCount; k++)
        {
            for (int i = 0; i < vertexCount; i++)
            {
                for (int j = 0; j < vertexCount; j++)
                {
                    if (distances[i, k] != int.MaxValue && 
                        distances[k, j] != int.MaxValue &&
                        distances[i, k] + distances[k, j] < distances[i, j])
                    {
                        distances[i, j] = distances[i, k] + distances[k, j];
                    }
                }
            }
        }
        
        return distances;
    }
}
```

## Topological Sorting

**Description**: Topological sorting produces a linear ordering of vertices in a Directed Acyclic Graph (DAG) such that for every directed edge (u,v), vertex u comes before v in the ordering.

**Code**:

```csharp
public static class TopologicalSort
{
    public static List<int> KahnAlgorithm(Dictionary<int, List<int>> graph, int vertexCount)
    {
        var inDegree = new int[vertexCount];
        var result = new List<int>();
        var queue = new Queue<int>();
        
        // Calculate in-degrees
        foreach (var vertex in graph.Keys)
        {
            foreach (var neighbor in graph[vertex])
            {
                inDegree[neighbor]++;
            }
        }
        
        // Add vertices with 0 in-degree to queue
        for (int i = 0; i < vertexCount; i++)
        {
            if (inDegree[i] == 0)
            {
                queue.Enqueue(i);
            }
        }
        
        // Process vertices
        while (queue.Count > 0)
        {
            int vertex = queue.Dequeue();
            result.Add(vertex);
            
            if (graph.ContainsKey(vertex))
            {
                foreach (int neighbor in graph[vertex])
                {
                    inDegree[neighbor]--;
                    if (inDegree[neighbor] == 0)
                    {
                        queue.Enqueue(neighbor);
                    }
                }
            }
        }
        
        if (result.Count != vertexCount)
        {
            throw new InvalidOperationException("Graph contains a cycle - topological sort not possible");
        }
        
        return result;
    }
    
    public static List<int> DFSBasedSort(Dictionary<int, List<int>> graph, int vertexCount)
    {
        var visited = new HashSet<int>();
        var stack = new Stack<int>();
        
        for (int i = 0; i < vertexCount; i++)
        {
            if (!visited.Contains(i))
            {
                DFSUtil(graph, i, visited, stack);
            }
        }
        
        return stack.ToList();
    }
    
    private static void DFSUtil(Dictionary<int, List<int>> graph, int vertex, HashSet<int> visited, Stack<int> stack)
    {
        visited.Add(vertex);
        
        if (graph.ContainsKey(vertex))
        {
            foreach (int neighbor in graph[vertex])
            {
                if (!visited.Contains(neighbor))
                {
                    DFSUtil(graph, neighbor, visited, stack);
                }
            }
        }
        
        stack.Push(vertex);
    }
}
```

**Usage Examples**:

```csharp
// A* Algorithm Example
bool[,] grid = new bool[5, 5]
{
    {true, true, false, true, true},
    {true, true, false, true, true},
    {true, true, true, true, true},
    {true, false, false, false, true},
    {true, true, true, true, true}
};

var start = new AStarNode(0, 0);
var goal = new AStarNode(4, 4);
var path = AStar.FindPath(grid, start, goal);

Console.WriteLine($"Path found with {path.Count} nodes");

// Bellman-Ford Example
var edges = new List<Edge>
{
    new Edge(0, 1, -1),
    new Edge(0, 2, 4),
    new Edge(1, 2, 3),
    new Edge(1, 3, 2),
    new Edge(3, 2, 5)
};

var distances = BellmanFord.FindShortestPaths(edges, 4, 0);
foreach (var kvp in distances)
{
    Console.WriteLine($"Distance to {kvp.Key}: {kvp.Value}");
}

// Topological Sort Example
var graph = new Dictionary<int, List<int>>
{
    {5, new List<int> {2, 0}},
    {4, new List<int> {0, 1}},
    {2, new List<int> {3}},
    {3, new List<int> {1}}
};

var topOrder = TopologicalSort.KahnAlgorithm(graph, 6);
Console.WriteLine($"Topological Order: {string.Join(" -> ", topOrder)}");
```

**Notes**:

- **Time Complexity**:
  - DFS (Depth-First Search): O(V + E) where V is vertices and E is edges
  - BFS (Breadth-First Search): O(V + E)
  - Dijkstra: O((V + E) log V) with priority queue
  - A* (A-Star): O(b^d) where b is branching factor and d is depth, but often much faster than Dijkstra with good heuristics
  - Bellman-Ford: O(V × E) - slower than Dijkstra but handles negative weights
  - Floyd-Warshall: O(V³) - finds all pairs shortest paths
  - Topological Sort: O(V + E)

- **Space Complexity**:
  - Most algorithms: O(V) for visited tracking
  - Floyd-Warshall: O(V²) for distance matrix
  - A*: O(V) but can be higher due to open/closed sets

- **Use Cases**:
  - DFS: Topological sorting, cycle detection, pathfinding in mazes, connected components
  - BFS: Shortest path in unweighted graphs, level-order traversal, finding connected components
  - Dijkstra: Shortest path in weighted graphs with non-negative weights (GPS navigation, network routing)
  - A*: Game pathfinding, robotics navigation with known goal and good heuristic function
  - Bellman-Ford: Shortest paths with negative weights, detecting negative cycles, distributed systems
  - Floyd-Warshall: All pairs shortest paths, transitive closure, finding diameter of graph
  - Topological Sort: Task scheduling, dependency resolution, compiler optimization

- **Performance**:
  - BFS uses more memory but guarantees shortest path in unweighted graphs
  - A* is typically faster than Dijkstra when a good heuristic is available
  - Bellman-Ford is slower than Dijkstra but more versatile
  - Choose algorithm based on graph properties and requirements

- **Algorithm Selection Guidelines**:
  - Unweighted graphs: Use BFS for shortest paths
  - Weighted graphs (non-negative): Use Dijkstra for single source, Floyd-Warshall for all pairs
  - Weighted graphs (negative weights): Use Bellman-Ford, avoid if negative cycles exist
  - Game/robotics pathfinding: Use A* with Manhattan or Euclidean distance heuristics
  - DAG processing: Use topological sorting for dependency resolution

- **Security**: No special security considerations for basic graph algorithms, but be aware of:
  - Input validation for graph construction
  - Protection against denial of service with very large graphs
  - Memory usage considerations for algorithms with high space complexity

## Related Snippets

- [Data Structures](data-structures.md) - Basic data structures used in graph algorithms (priority queues, stacks, queues)
- [Sorting Algorithms](sorting-algorithms.md) - For topological sorting applications and priority queue operations
- [Dynamic Programming Algorithms](dynamic-programming.md) - For graph algorithms using dynamic programming approaches
