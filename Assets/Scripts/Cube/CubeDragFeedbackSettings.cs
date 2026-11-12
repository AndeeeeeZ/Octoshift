using UnityEngine;

[CreateAssetMenu(fileName = "Cube Drag Feedback Settings",
    menuName = "Octoshift/Cube Drag Feedback Settings")]
public class CubeDragFeedbackSettings : ScriptableObject
{
    [Header("Interaction")]
    [SerializeField, Min(1f)] private float cancelRadiusPixels = 24f;

    [Header("Colors")]
    [SerializeField] private Color sphereColor = new(1f, 0.82f, 0.2f);
    [SerializeField] private Color arrowColor = Color.black;
    [SerializeField] private Color arrowOutlineColor = Color.white;
    [SerializeField] private Color cancelRingColor = Color.black;

    [Header("Arrow Outline")]
    [SerializeField, Range(0f, 0.08f)] private float arrowOutlineThickness = 0.012f;

    [Header("Cancel Ring")]
    [SerializeField, Range(0f, 8f)] private float cancelRingThicknessPixels = 2f;

    [Header("Animation")]
    [SerializeField, Min(0.01f)] private float fadeInSeconds = 0.2f;
    [SerializeField, Min(0.01f)] private float fadeOutSeconds = 0.35f;
    [SerializeField, Min(0.01f)] private float stretchSeconds = 0.14f;
    [SerializeField, Min(0f)] private float minimumArrowLength = 0.55f;
    [SerializeField, Min(0f)] private float maximumArrowLength = 1.3f;
    [SerializeField, Min(1f)] private float fullStretchPixels = 80f;

    public float CancelRadiusPixels => cancelRadiusPixels;
    public Color SphereColor => sphereColor;
    public Color ArrowColor => arrowColor;
    public Color ArrowOutlineColor => arrowOutlineColor;
    public float ArrowOutlineThickness => arrowOutlineThickness;
    public Color CancelRingColor => cancelRingColor;
    public float CancelRingThicknessPixels => cancelRingThicknessPixels;
    public float FadeInSeconds => fadeInSeconds;
    public float FadeOutSeconds => fadeOutSeconds;
    public float StretchSeconds => stretchSeconds;
    public float MinimumArrowLength => minimumArrowLength;
    public float MaximumArrowLength => maximumArrowLength;
    public float FullStretchPixels => fullStretchPixels;
}
