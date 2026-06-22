using UnityEngine;

[CreateAssetMenu(menuName = "Value/Float Value")]
public class FloatValue : ScriptableObject
{
    [SerializeField] private float value;
    public float Value => value;
}