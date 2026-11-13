using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LevelBuilderValidator
{
    public static List<string> Validate(Scene scene, float spacing, Vector3 origin)
    {
        List<string> messages = new();
        List<BlockNode> cubes = LevelBuilderGraph.FindInScene<BlockNode>(scene);
        List<Pole> poles = LevelBuilderGraph.FindInScene<Pole>(scene);
        List<PlayerMovement> players = LevelBuilderGraph.FindInScene<PlayerMovement>(scene);
        List<Exit> exits = LevelBuilderGraph.FindInScene<Exit>(scene);

        if (cubes.Count == 0)
            messages.Add("Error: Place at least one cube.");

        if (players.Count != 1 || !players[0].gameObject.activeInHierarchy)
            messages.Add("Error: Place one active player.");

        if (exits.Count != 1 || !exits[0].gameObject.activeInHierarchy)
            messages.Add("Error: Place one active exit.");

        if (exits.Count == 1)
        {
            bool exitHandled = false;

            foreach (VoidEventListener listener in LevelBuilderGraph.FindInScene<VoidEventListener>(scene))
            {
                if (listener.gameObject.activeInHierarchy &&
                    listener.Event == exits[0].OnReachingExit &&
                    listener.Response != null &&
                    listener.Response.GetPersistentEventCount() > 0)
                    exitHandled = true;
            }

            if (!exitHandled)
                messages.Add("Error: No active scene listener handles the exit event.");
        }

        HashSet<Vector3Int> occupied = new();

        foreach (BlockNode cube in cubes)
        {
            Vector3 cell = (cube.transform.position - origin) / spacing;
            Vector3Int rounded = Vector3Int.RoundToInt(cell);

            if ((cell - (Vector3)rounded).sqrMagnitude > 0.001f)
                messages.Add($"Warning: {cube.name} is off the builder grid.");

            if (!occupied.Add(rounded))
                messages.Add($"Error: Multiple cubes occupy grid cell {rounded}.");

            if (cube.Connections == null)
            {
                messages.Add($"Error: {cube.name} has no connection list.");
                continue;
            }

            HashSet<Pole> seen = new();

            foreach (Pole pole in cube.Connections)
            {
                if (pole == null)
                {
                    messages.Add($"Error: {cube.name} has a missing pole reference.");
                    continue;
                }

                if (!seen.Add(pole))
                    messages.Add($"Error: {cube.name} lists {pole.name} more than once.");

                LevelBuilderGraph.TryGetEndpoints(pole, out BlockNode first, out BlockNode second);

                if (first != cube && second != cube)
                    messages.Add($"Error: {cube.name} lists a pole that does not connect to it.");
            }
        }

        HashSet<string> pairs = new();

        foreach (Pole pole in poles)
        {
            if (!LevelBuilderGraph.TryGetEndpoints(pole, out BlockNode first, out BlockNode second))
            {
                messages.Add($"Error: {pole.name} is missing a cube endpoint.");
                continue;
            }

            if (first == second)
                messages.Add($"Error: {pole.name} connects a cube to itself.");

            if (first.Connections == null || second.Connections == null ||
                !first.Connections.Contains(pole) || !second.Connections.Contains(pole))
                messages.Add($"Error: {pole.name} is not listed by both cubes.");

            if (!LevelBuilderGraph.AreNeighbors(first, second, spacing))
                messages.Add($"Warning: {pole.name} connects cubes outside the configured spacing.");

            int low = Mathf.Min(first.GetInstanceID(), second.GetInstanceID());
            int high = Mathf.Max(first.GetInstanceID(), second.GetInstanceID());

            if (!pairs.Add($"{low}:{high}"))
                messages.Add($"Error: Duplicate poles connect {first.name} and {second.name}.");
        }

        foreach (PuzzleDragRotator rotator in LevelBuilderGraph.FindInScene<PuzzleDragRotator>(scene))
        {
            SerializedObject serializedRotator = new(rotator);
            CheckReference(serializedRotator, "playerMovement", messages, rotator.name);
            CheckReference(serializedRotator, "regionRotator", messages, rotator.name);
            CheckReference(serializedRotator, "messageController", messages, rotator.name);
        }

        foreach (Bound bound in LevelBuilderGraph.FindInScene<Bound>(scene))
        {
            CheckReference(new SerializedObject(bound), "target", messages, bound.name);
        }

        foreach (PlayerMovement player in players)
        {
            CheckReference(new SerializedObject(player), "messageController", messages, player.name);
        }

        foreach (CameraOrbit orbit in LevelBuilderGraph.FindInScene<CameraOrbit>(scene))
        {
            CheckReference(new SerializedObject(orbit), "player", messages, orbit.name);
        }

        List<SceneLoader> loaders = LevelBuilderGraph.FindInScene<SceneLoader>(scene);

        if (loaders.Count == 0)
            messages.Add("Error: The scene has no SceneLoader.");

        if (loaders.Count == 1 && string.IsNullOrEmpty(loaders[0].targetSceneName) &&
            scene.path != LevelBuilderScenes.TemplatePath)
            messages.Add("Warning: Set Next Scene before publishing this level.");

        if (loaders.Count == 1 && !string.IsNullOrEmpty(loaders[0].targetSceneName))
        {
            bool nextSceneInBuild = false;

            foreach (EditorBuildSettingsScene buildScene in EditorBuildSettings.scenes)
            {
                if (buildScene.enabled &&
                    System.IO.Path.GetFileNameWithoutExtension(buildScene.path) ==
                    loaders[0].targetSceneName)
                    nextSceneInBuild = true;
            }

            if (!nextSceneInBuild)
                messages.Add("Warning: Next Scene is not enabled in Build Settings.");
        }

        List<RestartHelper> restarts = LevelBuilderGraph.FindInScene<RestartHelper>(scene);

        if (restarts.Count == 0)
            messages.Add("Error: The scene has no RestartHelper.");

        foreach (RestartHelper restart in restarts)
        {
            SerializedProperty calls = new SerializedObject(restart).FindProperty(
                "ToReloadCurrScene.m_PersistentCalls.m_Calls");
            bool found = false;

            if (calls != null)
            {
                for (int index = 0; index < calls.arraySize; index++)
                {
                    SerializedProperty call = calls.GetArrayElementAtIndex(index);

                    if (call.FindPropertyRelative("m_MethodName").stringValue !=
                        "SetTargetSceneName")
                        continue;

                    found = true;
                    string target = call.FindPropertyRelative(
                        "m_Arguments.m_StringArgument").stringValue;
                    string ownName = System.IO.Path.GetFileNameWithoutExtension(scene.path);

                    if (target != ownName)
                        messages.Add($"Error: RestartHelper reloads {target} instead of {ownName}.");
                }
            }

            if (!found)
                messages.Add("Error: RestartHelper has no scene-name assignment.");
        }

        bool inBuild = false;

        foreach (EditorBuildSettingsScene buildScene in EditorBuildSettings.scenes)
        {
            if (buildScene.path == scene.path && buildScene.enabled)
                inBuild = true;
        }

        if (!inBuild && !string.IsNullOrEmpty(scene.path) &&
            scene.path != LevelBuilderScenes.TemplatePath)
            messages.Add("Warning: This scene is not enabled in Build Settings.");

        if (messages.Count == 0)
            messages.Add("Level validation passed.");

        return messages;
    }

    private static void CheckReference(SerializedObject owner, string propertyName,
        List<string> messages, string objectName)
    {
        SerializedProperty property = owner.FindProperty(propertyName);

        if (property == null || property.objectReferenceValue == null)
            messages.Add($"Error: {objectName} is missing its {propertyName} reference.");
    }
}
