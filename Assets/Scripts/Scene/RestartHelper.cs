using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class RestartHelper : MonoBehaviour
{
    public UnityEvent ToReloadCurrScene; 
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
        ToReloadCurrScene?.Invoke(); 
    }
}
