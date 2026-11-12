using System.Collections.Generic;
using UnityEngine;

public class BlockNode : MonoBehaviour
{
    public List<Pole> Connections;

    public bool HasConnectionOnFace(Vector3 faceNormalWorld)
    {
        if (Connections == null)
            return false;

        Vector3 normal = faceNormalWorld.normalized;
        foreach (Pole pole in Connections)
        {
            if (pole == null)
                continue;

            BlockNode neighbor = pole.GetOtherBlock(this);
            if (neighbor == null)
                continue;

            Vector3 toNeighbor = neighbor.transform.position - transform.position;
            if (toNeighbor.sqrMagnitude > 0.0001f &&
                Vector3.Dot(normal, toNeighbor.normalized) > 0.9f)
                return true;
        }

        return false;
    }

    public List<BlockNode> GetNeighbors()
    {
        List<BlockNode> neighbors = new List<BlockNode>();

        foreach (Pole p in Connections)
        {
            BlockNode b = p.GetOtherBlock(this);
            if (b != null)
            {
                neighbors.Add(b); 
            }
        }

        return neighbors; 
    }
}
