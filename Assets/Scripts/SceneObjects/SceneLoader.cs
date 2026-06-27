using UnityEngine;
using UnityEngine.SceneManagement;

/*
* Helper class to load scene
*/

public class SceneLoader : MonoBehaviour
{
    public string targetSceneName; 

    public void LoadTargetScene()
    {
        LoadScene(targetSceneName);
    }

    public void LoadScene(string name)
    {
        SceneManager.LoadScene(name, LoadSceneMode.Single); 
    }

    public void SetTargetSceneName(string n)
    {
        targetSceneName = n; 
    }
}
