using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class RestartHelper : MonoBehaviour
{
    private PlayerInput input;

    private void Awake()
    {
        input = new PlayerInput(); 
    
    }

    private void OnEnable()
    {
        input.Enable(); 
        input.Scene.Restart.performed += ReloadScene; 
    }

    private void OnDisable()
    {
        input.Scene.Restart.performed -= ReloadScene; 
        input.Disable(); 
    }

    public void ReloadScene(InputAction.CallbackContext context)
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
