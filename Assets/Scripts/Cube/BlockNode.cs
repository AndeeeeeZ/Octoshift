using System.Collections.Generic;
using UnityEngine;

public class BlockNode : MonoBehaviour
{
    public List<Pole> Connections;

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
