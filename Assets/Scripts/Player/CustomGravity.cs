using UnityEngine;

public class CustomGravity : MonoBehaviour
{
    [SerializeField] private float gravityStrength; 
    private Rigidbody rb;
    private void Awake()
    {
        rb = GetComponent<Rigidbody>(); 
    }
    private void FixedUpdate()
    {
        rb.AddForce(-transform.up * gravityStrength, ForceMode.Acceleration); 
    }
}
