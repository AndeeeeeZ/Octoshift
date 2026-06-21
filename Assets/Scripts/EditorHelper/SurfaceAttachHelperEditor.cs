using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SurfaceAttachHelper))]
public class SurfaceAttachHelperEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        SurfaceAttachHelper helper =
            (SurfaceAttachHelper)target;

        GUILayout.Space(10);

        if (GUILayout.Button("Snap To Surface"))
        {
            Undo.RecordObject(
                helper.transform,
                "Snap To Surface");

            helper.SnapToSurface();

            EditorUtility.SetDirty(
                helper.transform);
        }
    }
}