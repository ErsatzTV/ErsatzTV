namespace ErsatzTV.Infrastructure.Search;

public class AdjGraph
{
    private readonly List<Edge> _edges = [];

    public void Clear()
    {
        _edges.Clear();
    }

    public void AddEdge(string from, string to)
    {
        _edges.Add(new Edge(from.ToLowerInvariant(), to.ToLowerInvariant()));
    }

    public bool HasCycle(string from)
    {
        var visited = new System.Collections.Generic.HashSet<string>();
        var stack = new System.Collections.Generic.HashSet<string>();
        return HasCycleImpl(from.ToLowerInvariant(), visited, stack);
    }

    private bool HasCycleImpl(string node, ISet<string> visited, ISet<string> stack)
    {
        if (stack.Contains(node))
        {
            return true;
        }

        if (!visited.Add(node))
        {
            return false;
        }

        stack.Add(node);

        foreach (Edge edge in _edges.Where(e => e.From == node))
        {
            if (HasCycleImpl(edge.To, visited, stack))
            {
                return true;
            }
        }

        stack.Remove(node);
        return false;
    }

    private record Edge(string From, string To);
}
