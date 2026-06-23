using System.Collections.Generic;
using Microsoft.Unity.VisualStudio.Editor;

public static class GraphTraversal
{
    public static List<BlockNode> FindConnectedRegion(
        BlockNode start,
        Pole ignoredPole = null
    )
    {
        List<BlockNode> result = new();

        if (start == null)
            return result;

        HashSet<BlockNode> visited = new();
        Queue<BlockNode> queue = new();

        visited.Add(start);
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            BlockNode current = queue.Dequeue();
            result.Add(current);

            foreach (Pole pole in current.Connections)
            {
                if (pole == ignoredPole)
                    continue;

                BlockNode neighbor = pole.GetOtherBlock(current);

                if (neighbor == null)
                    continue; 
                
                if (visited.Contains(neighbor))
                    continue; 
                
                visited.Add(neighbor); 
                queue.Enqueue(neighbor); 
            }
        }

        return result; 
    }

    public static List<BlockNode> FindMovingRegion(
        BlockNode start, 
        Axis rotationAxis
    )
    {
        List<BlockNode> result = new();

        if (start == null)
            return result;

        HashSet<BlockNode> visited = new();
        Queue<BlockNode> queue = new();

        visited.Add(start);
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            BlockNode current = queue.Dequeue();
            result.Add(current);

            foreach (Pole pole in current.Connections)
            {
                // Matching axis = hinge
                // Do not traverse through it
                if (pole.GetAxis() == rotationAxis)
                    continue;

                BlockNode neighbor = pole.GetOtherBlock(current);

                if (neighbor == null)
                    continue;

                if (visited.Contains(neighbor))
                    continue;

                visited.Add(neighbor);
                queue.Enqueue(neighbor);
            }
        }

        return result;
    }

}