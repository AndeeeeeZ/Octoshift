using NUnit.Framework.Internal;
using TMPro;
using UnityEngine;

public class MessageController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private float displayTime;
    private bool isDisplaying;
    private float timer;

    private void Start()
    {
        isDisplaying = false; 
        text.text = ""; 
    }

    public void DisplayMessage(string message)
    {
        text.text = message;
        isDisplaying = true;
        timer = 0f;
    }

    private void Update()
    {
        if (!isDisplaying) return;

        timer += Time.deltaTime;
        if (timer >= displayTime)
        {
            text.text = "";
            isDisplaying = false; 
        }
    }
}
