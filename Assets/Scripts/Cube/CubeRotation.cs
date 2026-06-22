using UnityEngine;

// Unused script for cube rotation 
// More like button to rotate

public class CubeRotation : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 180f;

    [SerializeField] private bool canRotateX = true;
    [SerializeField] private bool canRotateY = true;
    [SerializeField] private bool canRotateZ = true;

    private Quaternion targetRotation;
    private bool isRotating;

    private void Awake()
    {
        targetRotation = transform.rotation;
    }

    private void Update()
    {
        if (!isRotating)
            return;

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime);

        if (Quaternion.Angle(transform.rotation, targetRotation) < 0.1f)
        {
            transform.rotation = targetRotation;
            isRotating = false;
        }
    }

    public void RotateX(int direction)
    {
        if (!canRotateX || isRotating)
            return;

        targetRotation =
            Quaternion.AngleAxis(90f * direction, Vector3.right) *
            targetRotation;

        isRotating = true;
    }

    public void RotateY(int direction)
    {
        if (!canRotateY || isRotating)
            return;

        targetRotation =
            Quaternion.AngleAxis(90f * direction, Vector3.up) *
            targetRotation;

        isRotating = true;
    }

    public void RotateZ(int direction)
    {
        if (!canRotateZ || isRotating)
            return;

        targetRotation =
            Quaternion.AngleAxis(90f * direction, Vector3.forward) *
            targetRotation;

        isRotating = true;
    }
}