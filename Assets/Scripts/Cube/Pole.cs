using UnityEngine;

public class Pole : MonoBehaviour
{
    [SerializeField] private BlockNode blockA;
    [SerializeField] private BlockNode blockB;

    public BlockNode GetOtherBlock(BlockNode block)
    {
        if (block == blockA)
            return blockB;
        else if (block == blockB)
            return blockA;
        else
        {
            Debug.LogError("Pole received non-connected block");
            return null;
        }
    }

    public Vector3 GetDirection()
    {
        return (blockB.transform.position - blockA.transform.position).normalized;
    }

    public Axis GetAxis()
    {
        Vector3 direction = GetDirection();

        if (Mathf.Abs(direction.x) > 0.9f)
            return Axis.X;

        if (Mathf.Abs(direction.y) > 0.9f)
            return Axis.Y;

        return Axis.Z;
    }
}
