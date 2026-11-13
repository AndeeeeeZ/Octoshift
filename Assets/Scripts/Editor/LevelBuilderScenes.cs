using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class LevelBuilderScenes
{
    public const string TemplatePath = "Assets/Scenes/Level Template.unity";
    private const string SourceLevelPath = "Assets/Scenes/Level 1.unity";

    public static bool CreateTemplate(out SceneAsset template, out string error)
    {
        template = AssetDatabase.LoadAssetAtPath<SceneAsset>(TemplatePath);
        error = null;

        if (template != null)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                error = "Template update was cancelled.";
                return false;
            }

            Scene existingScene = EditorSceneManager.OpenScene(TemplatePath, OpenSceneMode.Single);

            if (!EnsureCenterCube(existingScene, out error))
                return false;

            EditorSceneManager.SaveScene(existingScene);
            return true;
        }

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SourceLevelPath) == null)
        {
            error = "Level 1 was not found. A source scene is needed for the shared gameplay setup.";
            return false;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            error = "Template creation was cancelled.";
            return false;
        }

        if (!AssetDatabase.CopyAsset(SourceLevelPath, TemplatePath))
        {
            error = "Unity could not copy Level 1 to the template path.";
            return false;
        }

        Scene scene = EditorSceneManager.OpenScene(TemplatePath, OpenSceneMode.Single);

        // Preserve the scene's controller references while clearing its authored puzzle.
        foreach (PlayerMovement player in LevelBuilderGraph.FindInScene<PlayerMovement>(scene))
        {
            player.transform.SetParent(null, true);
            player.gameObject.SetActive(false);
        }

        foreach (Exit exit in LevelBuilderGraph.FindInScene<Exit>(scene))
        {
            exit.transform.SetParent(null, true);
            exit.gameObject.SetActive(false);
        }

        foreach (Pole pole in LevelBuilderGraph.FindInScene<Pole>(scene))
            Object.DestroyImmediate(pole.gameObject);

        foreach (BlockNode cube in LevelBuilderGraph.FindInScene<BlockNode>(scene))
            Object.DestroyImmediate(cube.gameObject);

        // These standalone props belong to the source puzzle, not its shared setup.
        List<GameObject> props = new();

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (IsPuzzleProp(child.gameObject.name))
                    props.Add(child.gameObject);
            }
        }

        foreach (GameObject prop in props)
        {
            if (prop != null)
                Object.DestroyImmediate(prop);
        }

        foreach (SceneLoader loader in LevelBuilderGraph.FindInScene<SceneLoader>(scene))
            loader.targetSceneName = "";

        ConfigureRestart(scene, "Level Template");

        if (!EnsureCenterCube(scene, out error))
            return false;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.Refresh();

        template = AssetDatabase.LoadAssetAtPath<SceneAsset>(TemplatePath);
        return template != null;
    }

    private static bool EnsureCenterCube(Scene scene, out string error)
    {
        error = null;

        if (LevelBuilderGraph.FindInScene<BlockNode>(scene).Count > 0)
            return true;

        GameObject cubePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Prefabs/Cube.prefab");

        if (cubePrefab == null || cubePrefab.GetComponent<BlockNode>() == null)
        {
            error = "The Cube prefab is missing its BlockNode component.";
            return false;
        }

        GameObject cube = (GameObject)PrefabUtility.InstantiatePrefab(cubePrefab, scene);
        Transform puzzleRoot = LevelBuilderGraph.FindPuzzleRoot(scene);

        if (puzzleRoot != null)
            cube.transform.SetParent(puzzleRoot, false);

        cube.transform.localPosition = Vector3.zero;
        cube.transform.localRotation = Quaternion.identity;

        BlockNode node = cube.GetComponent<BlockNode>();
        node.Connections = new List<Pole>();

        PrefabUtility.RecordPrefabInstancePropertyModifications(cube.transform);
        PrefabUtility.RecordPrefabInstancePropertyModifications(node);
        EditorSceneManager.MarkSceneDirty(scene);

        return true;
    }

    public static bool CreateLevel(SceneAsset template, out string error)
    {
        error = null;

        if (template == null)
        {
            error = "Create or assign a clean level template first.";
            return false;
        }

        string templatePath = AssetDatabase.GetAssetPath(template);
        string path = EditorUtility.SaveFilePanelInProject(
            "New Octoshift Level", "New Level", "unity",
            "Choose a scene path for the new level.", "Assets/Scenes");

        if (string.IsNullOrEmpty(path))
            return false;

        if (path == templatePath)
        {
            error = "Choose a different path from the template scene.";
            return false;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return false;

        if (!AssetDatabase.CopyAsset(templatePath, path))
        {
            error = "Unity could not create the new scene. Check the selected path.";
            return false;
        }

        AssetDatabase.Refresh();
        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        ConfigureRestart(scene, System.IO.Path.GetFileNameWithoutExtension(path));
        EditorSceneManager.SaveScene(scene);
        AddToBuildSettings(path);

        return true;
    }

    public static void AddToBuildSettings(string scenePath)
    {
        List<EditorBuildSettingsScene> scenes = new(EditorBuildSettings.scenes);

        foreach (EditorBuildSettingsScene scene in scenes)
        {
            if (scene.path == scenePath)
            {
                if (!scene.enabled)
                {
                    scene.enabled = true;
                    EditorBuildSettings.scenes = scenes.ToArray();
                }

                return;
            }
        }

        scenes.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    public static void ConfigureRestart(Scene scene, string sceneName)
    {
        foreach ((Object owner, string _, string callsPath) in RestartEvents(scene))
        {
            SerializedObject serializedOwner = new(owner);
            SerializedProperty calls = serializedOwner.FindProperty(callsPath);

            if (calls == null)
                continue;

            for (int index = 0; index < calls.arraySize; index++)
            {
                SerializedProperty call = calls.GetArrayElementAtIndex(index);
                SerializedProperty method = call.FindPropertyRelative("m_MethodName");

                if (method.stringValue != "SetTargetSceneName")
                    continue;

                call.FindPropertyRelative("m_Arguments.m_StringArgument").stringValue = sceneName;
            }

            serializedOwner.ApplyModifiedProperties();
        }

        EditorSceneManager.MarkSceneDirty(scene);
    }

    public static IEnumerable<(Object Owner, string Label, string CallsPath)> RestartEvents(Scene scene)
    {
        foreach (RestartHelper restart in LevelBuilderGraph.FindInScene<RestartHelper>(scene))
        {
            yield return (restart, "R key", "ToReloadCurrScene.m_PersistentCalls.m_Calls");
        }

        foreach (Button button in LevelBuilderGraph.FindInScene<Button>(scene))
        {
            if (button.name == "Restart")
                yield return (button, "Restart button", "m_OnClick.m_PersistentCalls.m_Calls");
        }

        VoidEvent fallEvent = AssetDatabase.LoadAssetAtPath<VoidEvent>(
            "Assets/Scripts/ScriptableObject/Objects/Events/OnPlayerFellOutsideOfScreen.asset");

        if (fallEvent == null)
            yield break;

        foreach (VoidEventListener listener in LevelBuilderGraph.FindInScene<VoidEventListener>(scene))
        {
            if (listener.Event == fallEvent)
                yield return (listener, "Fall restart", "Response.m_PersistentCalls.m_Calls");
        }
    }

    private static bool IsPuzzleProp(string name)
    {
        return name == "Floor" || name.StartsWith("Floor (") ||
            name == "Water texture" || name.StartsWith("Water texture (") ||
            name == "Mural" || name.StartsWith("Mural (") ||
            name == "Oneway Platform" || name.StartsWith("Oneway Platform (") ||
            name == "OneWay Platform" || name.StartsWith("OneWay Platform (");
    }
}
