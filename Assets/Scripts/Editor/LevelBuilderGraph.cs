using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LevelBuilderGraph
{
    public static Transform FindPuzzleRoot(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == "Puzzle")
                return root.transform;
        }

        return null;
    }

    public static List<T> FindInScene<T>(Scene scene) where T : Component
    {
        List<T> results = new();

        if (!scene.IsValid() || !scene.isLoaded)
            return results;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            results.AddRange(root.GetComponentsInChildren<T>(true));
        }

        return results;
    }

    public static bool AreNeighbors(BlockNode first, BlockNode second, float spacing)
    {
        if (first == null || second == null || first == second)
            return false;

        Vector3 difference = second.transform.position - first.transform.position;
        float tolerance = Mathf.Max(0.05f, spacing * 0.02f);
        int changedAxes = 0;

        if (Mathf.Abs(difference.x) > tolerance)
        {
            if (Mathf.Abs(Mathf.Abs(difference.x) - spacing) > tolerance)
                return false;

            changedAxes++;
        }

        if (Mathf.Abs(difference.y) > tolerance)
        {
            if (Mathf.Abs(Mathf.Abs(difference.y) - spacing) > tolerance)
                return false;

            changedAxes++;
        }

        if (Mathf.Abs(difference.z) > tolerance)
        {
            if (Mathf.Abs(Mathf.Abs(difference.z) - spacing) > tolerance)
                return false;

            changedAxes++;
        }

        return changedAxes == 1;
    }

    public static bool TryGetEndpoints(Pole pole, out BlockNode first, out BlockNode second)
    {
        SerializedObject serializedPole = new(pole);
        first = serializedPole.FindProperty("blockA")?.objectReferenceValue as BlockNode;
        second = serializedPole.FindProperty("blockB")?.objectReferenceValue as BlockNode;

        return first != null && second != null;
    }

    public static Pole FindConnection(Scene scene, BlockNode first, BlockNode second)
    {
        foreach (Pole pole in FindInScene<Pole>(scene))
        {
            TryGetEndpoints(pole, out BlockNode a, out BlockNode b);

            if ((a == first && b == second) || (a == second && b == first))
                return pole;
        }

        return null;
    }

    public static bool Connect(Scene scene, BlockNode first, BlockNode second,
        GameObject polePrefab, float spacing, out string error)
    {
        error = null;

        if (!AreNeighbors(first, second, spacing))
        {
            error = "Choose two neighboring cubes along one axis.";
            return false;
        }

        if (FindConnection(scene, first, second) != null)
        {
            error = "Those cubes are already connected.";
            return false;
        }

        if (polePrefab == null || polePrefab.GetComponent<Pole>() == null)
        {
            error = "Assign a prefab with a Pole component.";
            return false;
        }

        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Connect Cubes");

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(polePrefab, scene);
        Undo.RegisterCreatedObjectUndo(instance, "Connect Cubes");

        Transform puzzleRoot = FindPuzzleRoot(scene);

        if (puzzleRoot != null)
            Undo.SetTransformParent(instance.transform, puzzleRoot, "Parent Pole to Puzzle");

        Pole pole = instance.GetComponent<Pole>();
        Vector3 direction = second.transform.position - first.transform.position;

        Undo.RecordObject(instance.transform, "Position Pole");
        instance.transform.position = (first.transform.position + second.transform.position) * 0.5f;
        instance.transform.rotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);

        SerializedObject serializedPole = new(pole);
        serializedPole.FindProperty("blockA").objectReferenceValue = first;
        serializedPole.FindProperty("blockB").objectReferenceValue = second;
        serializedPole.ApplyModifiedProperties();

        AddConnection(first, pole);
        AddConnection(second, pole);

        PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
        EditorSceneManager.MarkSceneDirty(scene);
        Undo.CollapseUndoOperations(undoGroup);

        return true;
    }

    public static void DeletePole(Pole pole)
    {
        if (pole == null)
            return;

        Scene scene = pole.gameObject.scene;
        TryGetEndpoints(pole, out BlockNode first, out BlockNode second);

        RemoveConnection(first, pole);
        RemoveConnection(second, pole);

        Undo.DestroyObjectImmediate(pole.gameObject);
        EditorSceneManager.MarkSceneDirty(scene);
    }

    public static void DeleteCube(BlockNode cube)
    {
        if (cube == null)
            return;

        Scene scene = cube.gameObject.scene;
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Delete Cube and Connections");

        foreach (Pole pole in FindInScene<Pole>(scene))
        {
            TryGetEndpoints(pole, out BlockNode first, out BlockNode second);

            if (first == cube || second == cube ||
                (cube.Connections != null && cube.Connections.Contains(pole)))
                DeletePole(pole);
        }

        // Keep the player and exit when their supporting cube is erased.
        foreach (PlayerMovement player in cube.GetComponentsInChildren<PlayerMovement>(true))
            Undo.SetTransformParent(player.transform, null, "Detach Player");

        foreach (Exit exit in cube.GetComponentsInChildren<Exit>(true))
            Undo.SetTransformParent(exit.transform, null, "Detach Exit");

        Undo.DestroyObjectImmediate(cube.gameObject);
        EditorSceneManager.MarkSceneDirty(scene);
        Undo.CollapseUndoOperations(undoGroup);
    }

    private static void AddConnection(BlockNode cube, Pole pole)
    {
        Undo.RecordObject(cube, "Connect Cube");
        cube.Connections ??= new List<Pole>();

        if (!cube.Connections.Contains(pole))
            cube.Connections.Add(pole);

        PrefabUtility.RecordPrefabInstancePropertyModifications(cube);
    }

    private static void RemoveConnection(BlockNode cube, Pole pole)
    {
        if (cube == null || cube.Connections == null)
            return;

        Undo.RecordObject(cube, "Disconnect Cube");
        cube.Connections.RemoveAll(connection => connection == pole);
        PrefabUtility.RecordPrefabInstancePropertyModifications(cube);
    }
}
