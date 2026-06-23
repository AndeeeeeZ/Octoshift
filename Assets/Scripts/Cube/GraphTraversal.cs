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
    BlockNode pivot,
    Axis rotationAxis)
    {
        List<BlockNode> result = new();

        if (pivot == null)
            return result;

        HashSet<Pole> cutPoles = new();

        // Only inspect poles attached to pivot.
        foreach (Pole pole in pivot.Connections)
        {
            if (pole.GetAxis() == rotationAxis)
            {
                cutPoles.Add(pole);
            }
        }

        HashSet<BlockNode> visited = new();
        Queue<BlockNode> queue = new();

        visited.Add(pivot);
        queue.Enqueue(pivot);

        while (queue.Count > 0)
        {
            BlockNode current = queue.Dequeue();
            result.Add(current);

            foreach (Pole pole in current.Connections)
            {
                if (cutPoles.Contains(pole))
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