namespace Algorithm.GraphAlgorithms;

/// <summary>
/// Graph implementation using adjacency list representation.
/// </summary>
public class Graph
{
    private readonly Dictionary<int, List<int>> adjacencyList = new();
    
    /// <summary>
    /// Gets all vertices in the graph.
    /// </summary>
    public IEnumerable<int> Vertices => adjacencyList.Keys;
    
    /// <summary>
    /// Adds an edge from source to destination vertex.
    /// </summary>
    public void AddEdge(int source, int destination)
    {
        if (!adjacencyList.ContainsKey(source))
            adjacencyList[source] = new List<int>();
        if (!adjacencyList.ContainsKey(destination))
            adjacencyList[destination] = new List<int>();
            
        adjacencyList[source].Add(destination);
    }
    
    /// <summary>
    /// Adds an undirected edge between two vertices.
    /// </summary>
    public void AddUndirectedEdge(int vertex1, int vertex2)
    {
        AddEdge(vertex1, vertex2);
        AddEdge(vertex2, vertex1);
    }
    
    /// <summary>
    /// Gets the neighbors of a given vertex.
    /// </summary>
    public IEnumerable<int> GetNeighbors(int vertex)
    {
        return adjacencyList.TryGetValue(vertex, out var neighbors) ? neighbors : Enumerable.Empty<int>();
    }
    
    /// <summary>
    /// Performs iterative Depth-First Search starting from the given vertex.
    /// </summary>
    public List<int> DFS(int startVertex)
    {
        var visited = new HashSet<int>();
        var stack = new Stack<int>();
        var result = new List<int>();
        
        stack.Push(startVertex);
        
        while (stack.Count > 0)
        {
            var vertex = stack.Pop();
            
            if (!visited.Contains(vertex))
            {
                visited.Add(vertex);
                result.Add(vertex);
                
                if (adjacencyList.TryGetValue(vertex, out var neighbors))
                {
                    // Add neighbors in reverse order to maintain left-to-right traversal
                    for (var i = neighbors.Count - 1; i >= 0; i--)
                    {
                        var neighbor = neighbors[i];
                        if (!visited.Contains(neighbor))
                            stack.Push(neighbor);
                    }
                }
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// Performs recursive Depth-First Search starting from the given vertex.
    /// </summary>
    public List<int> DFSRecursive(int startVertex)
    {
        var visited = new HashSet<int>();
        var result = new List<int>();
        DFSRecursiveHelper(startVertex, visited, result);
        return result;
    }
    
    private void DFSRecursiveHelper(int vertex, HashSet<int> visited, List<int> result)
    {
        visited.Add(vertex);
        result.Add(vertex);
        
        if (adjacencyList.TryGetValue(vertex, out var neighbors))
        {
            foreach (var neighbor in neighbors)
            {
                if (!visited.Contains(neighbor))
                    DFSRecursiveHelper(neighbor, visited, result);
            }
        }
    }
    
    /// <summary>
    /// Performs Breadth-First Search starting from the given vertex.
    /// </summary>
    public List<int> BFS(int startVertex)
    {
        var visited = new HashSet<int>();
        var queue = new Queue<int>();
        var result = new List<int>();
        
        visited.Add(startVertex);
        queue.Enqueue(startVertex);
        
        while (queue.Count > 0)
        {
            var vertex = queue.Dequeue();
            result.Add(vertex);
            
            if (adjacencyList.TryGetValue(vertex, out var neighbors))
            {
                foreach (var neighbor in neighbors)
                {
                    if (!visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// Finds the shortest path between two vertices using BFS.
    /// Returns null if no path exists.
    /// </summary>
    public List<int>? FindShortestPath(int start, int end)
    {
        if (start == end)
            return new List<int> { start };
        
        var visited = new HashSet<int>();
        var queue = new Queue<int>();
        var parent = new Dictionary<int, int>();
        
        visited.Add(start);
        queue.Enqueue(start);
        
        while (queue.Count > 0)
        {
            var vertex = queue.Dequeue();
            
            if (adjacencyList.TryGetValue(vertex, out var neighbors))
            {
                foreach (var neighbor in neighbors)
                {
                    if (!visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        parent[neighbor] = vertex;
                        queue.Enqueue(neighbor);
                        
                        if (neighbor == end)
                        {
                            // Reconstruct path
                            var path = new List<int>();
                            var current = end;
                            
                            while (current != start)
                            {
                                path.Add(current);
                                current = parent[current];
                            }
                            path.Add(start);
                            path.Reverse();
                            
                            return path;
                        }
                    }
                }
            }
        }
        
        return null; // No path found
    }
    
    /// <summary>
    /// Checks if the graph contains a cycle using DFS.
    /// </summary>
    public bool HasCycle()
    {
        var visited = new HashSet<int>();
        var recursionStack = new HashSet<int>();
        
        foreach (var vertex in Vertices)
        {
            if (!visited.Contains(vertex))
            {
                if (HasCycleDFS(vertex, visited, recursionStack))
                    return true;
            }
        }
        
        return false;
    }
    
    private bool HasCycleDFS(int vertex, HashSet<int> visited, HashSet<int> recursionStack)
    {
        visited.Add(vertex);
        recursionStack.Add(vertex);
        
        if (adjacencyList.TryGetValue(vertex, out var neighbors))
        {
            foreach (var neighbor in neighbors)
            {
                if (!visited.Contains(neighbor))
                {
                    if (HasCycleDFS(neighbor, visited, recursionStack))
                        return true;
                }
                else if (recursionStack.Contains(neighbor))
                {
                    return true; // Back edge found, cycle detected
                }
            }
        }
        
        recursionStack.Remove(vertex);
        return false;
    }
}