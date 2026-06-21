using UnityEngine;

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

    // direction: -1 = left, 1 = right
    public void RotateX(int direction)
    {
        if (!canRotateX || isRotating)
            return;

        targetRotation *= Quaternion.Euler(90f * direction, 0f, 0f);
        isRotating = true;
    }

    public void RotateY(int direction)
    {
        if (!canRotateY || isRotating)
            return;

        targetRotation *= Quaternion.Euler(0f, 90f * direction, 0f);
        isRotating = true;
    }

    public void RotateZ(int direction)
    {
        if (!canRotateZ || isRotating)
            return;

        targetRotation *= Quaternion.Euler(0f, 0f, 90f * direction);
        isRotating = true;
    }
}