using UnityEngine;

public class CustomGravity : MonoBehaviour
{
    [SerializeField] private float jumpGravityStrength, fallGravityStrength; 
    private Rigidbody rb;
    private float gravityStrength; 
    private void Awake()
    {
        rb = GetComponent<Rigidbody>(); 
        gravityStrength = fallGravityStrength;
    }
    private void FixedUpdate()
    {
        rb.AddForce(-transform.up * gravityStrength, ForceMode.Acceleration); 
    }

    public void Falling()
    {
        gravityStrength = fallGravityStrength; 
    }

    public void Jumping()
    {
        gravityStrength = jumpGravityStrength; 
    }
}
