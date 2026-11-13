using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelBuilderWindow : EditorWindow
{
    private const int SceneControlHint = 0x4C425001;

    private enum BuilderMode
    {
        Cube,
        Connect,
        Player,
        Exit,
        Floor,
        OneWayPlatform,
        Erase
    }

    private enum LayerAxis
    {
        X,
        Y,
        Z
    }

    [SerializeField] private GameObject cubePrefab;
    [SerializeField] private GameObject polePrefab;
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject exitPrefab;
    [SerializeField] private GameObject floorPrefab;
    [SerializeField] private GameObject oneWayPlatformPrefab;
    [SerializeField] private SceneAsset templateScene;
    [SerializeField] private BuilderMode mode;
    [SerializeField] private LayerAxis layerAxis = LayerAxis.Z;
    [SerializeField] private int layer;
    [SerializeField] private float gridSpacing = 6f;
    [SerializeField] private Vector3 gridOrigin;
    [SerializeField] private bool toolActive = true;
    [SerializeField] private int settingsVersion;
    [SerializeField] private string gridScenePath;

    private BlockNode firstConnectionCube;
    private Vector3 hoverPosition;
    private bool hasHoverPosition;
    private Vector2 scrollPosition;
    private List<string> validationMessages = new();
    private string statusMessage;
    private BlockNode platformCube;
    private Vector3 platformNormal;
    private Vector3 platformFaceCenter;
    private Vector3 platformStart;
    private Vector3 platformEnd;
    private BuilderMode platformMode;

    [MenuItem("Tools/Octoshift/Level Builder")]
    public static void Open()
    {
        GetWindow<LevelBuilderWindow>("Level Builder");
    }

    private void OnEnable()
    {
        cubePrefab ??= AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Cube.prefab");
        polePrefab ??= AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Pole.prefab");
        playerPrefab ??= AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
        exitPrefab ??= AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Exit.prefab");
        floorPrefab ??= AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Floor.prefab");
        oneWayPlatformPrefab ??= AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Prefabs/Oneway Platform.prefab");
        templateScene ??= AssetDatabase.LoadAssetAtPath<SceneAsset>(LevelBuilderScenes.TemplatePath);

        if (settingsVersion == 0)
        {
            gridSpacing = 6f;
            gridScenePath = null;
            settingsVersion = 1;
        }

        RefreshGridOrigin();

        SceneView.duringSceneGui += OnSceneGUI;
        Undo.undoRedoPerformed += OnUndoRedo;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        Undo.undoRedoPerformed -= OnUndoRedo;
        platformCube = null;
        GUIUtility.hotControl = 0;
    }

    private void OnGUI()
    {
        RefreshGridOrigin();
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        EditorGUILayout.LabelField("Scene", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Current Scene", SceneManager.GetActiveScene().path);
        templateScene = (SceneAsset)EditorGUILayout.ObjectField(
            "Template", templateScene, typeof(SceneAsset), false);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Create Clean Template"))
        {
            if (LevelBuilderScenes.CreateTemplate(out SceneAsset created, out string error))
            {
                templateScene = created;
                statusMessage = "Level template is ready with a center cube.";
            }
            else
            {
                statusMessage = error;
            }
        }

        if (GUILayout.Button("New Level"))
        {
            if (LevelBuilderScenes.CreateLevel(templateScene, out string error))
            {
                firstConnectionCube = null;
                validationMessages.Clear();
                statusMessage = "New level created and added to Build Settings.";
            }
            else if (!string.IsNullOrEmpty(error))
            {
                statusMessage = error;
            }
        }

        EditorGUILayout.EndHorizontal();

        Scene activeScene = SceneManager.GetActiveScene();
        List<SceneLoader> loaders = LevelBuilderGraph.FindInScene<SceneLoader>(activeScene);

        if (loaders.Count == 1)
        {
            string nextScene = EditorGUILayout.TextField("Next Scene", loaders[0].targetSceneName);

            if (nextScene != loaders[0].targetSceneName)
            {
                Undo.RecordObject(loaders[0], "Change Next Scene");
                loaders[0].targetSceneName = nextScene;
                EditorSceneManager.MarkSceneDirty(activeScene);
            }
        }

        if (GUILayout.Button("Add Current Scene to Build Settings") &&
            !string.IsNullOrEmpty(activeScene.path))
        {
            LevelBuilderScenes.AddToBuildSettings(activeScene.path);
            statusMessage = "Current scene added to Build Settings.";
        }

        EditorGUILayout.Space(8f);

        EditorGUILayout.LabelField("Scene View Tools", EditorStyles.boldLabel);
        toolActive = EditorGUILayout.Toggle("Enable Tool", toolActive);

        if (!toolActive)
            platformCube = null;

        BuilderMode newMode = (BuilderMode)GUILayout.SelectionGrid((int)mode,
            new[] { "Cube", "Connect", "Player", "Exit", "Floor", "One-way", "Erase" }, 4);

        if (newMode != mode)
        {
            mode = newMode;
            firstConnectionCube = null;
            platformCube = null;
            SceneView.RepaintAll();
        }

        gridSpacing = Mathf.Max(0.1f, EditorGUILayout.FloatField("Cube Spacing", gridSpacing));
        gridOrigin = EditorGUILayout.Vector3Field("Grid Origin", gridOrigin);

        if (GUILayout.Button("Use Selected Cube as Grid Origin"))
        {
            BlockNode selected = Selection.activeGameObject == null ? null :
                Selection.activeGameObject.GetComponentInParent<BlockNode>();

            if (selected != null)
                gridOrigin = selected.transform.position;
            else
                statusMessage = "Select a cube in the Hierarchy first.";
        }
        layerAxis = (LayerAxis)EditorGUILayout.EnumPopup("Layer Axis", layerAxis);
        layer = EditorGUILayout.IntField("Layer", layer);

        EditorGUILayout.HelpBox(ModeInstruction(), MessageType.Info);

        if (mode == BuilderMode.Connect && firstConnectionCube != null)
            EditorGUILayout.LabelField("First Cube", firstConnectionCube.name);

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Prefabs", EditorStyles.boldLabel);
        cubePrefab = (GameObject)EditorGUILayout.ObjectField("Cube", cubePrefab, typeof(GameObject), false);
        polePrefab = (GameObject)EditorGUILayout.ObjectField("Pole", polePrefab, typeof(GameObject), false);
        playerPrefab = (GameObject)EditorGUILayout.ObjectField("Player", playerPrefab, typeof(GameObject), false);
        exitPrefab = (GameObject)EditorGUILayout.ObjectField("Exit", exitPrefab, typeof(GameObject), false);
        floorPrefab = (GameObject)EditorGUILayout.ObjectField("Floor", floorPrefab, typeof(GameObject), false);
        oneWayPlatformPrefab = (GameObject)EditorGUILayout.ObjectField(
            "One-way", oneWayPlatformPrefab, typeof(GameObject), false);

        EditorGUILayout.Space(8f);

        if (GUILayout.Button("Validate Level"))
        {
            Scene scene = SceneManager.GetActiveScene();
            validationMessages = LevelBuilderValidator.Validate(scene, gridSpacing, gridOrigin);
        }

        foreach (string message in validationMessages)
        {
            MessageType type = message.StartsWith("Error:") ? MessageType.Error :
                message.StartsWith("Warning:") ? MessageType.Warning : MessageType.Info;

            EditorGUILayout.HelpBox(message, type);
        }

        if (!string.IsNullOrEmpty(statusMessage))
            EditorGUILayout.HelpBox(statusMessage, MessageType.None);

        EditorGUILayout.EndScrollView();
    }

    private string ModeInstruction()
    {
        return mode switch
        {
            BuilderMode.Cube => "Choose a layer, then left-click a grid cell to place a cube.",
            BuilderMode.Connect => "Left-click two neighboring cubes to add a pole.",
            BuilderMode.Player => "Left-click a cube face to place the player.",
            BuilderMode.Exit => "Left-click a cube face to place the exit.",
            BuilderMode.Floor => "Drag on a cube face to draw a solid floor rectangle.",
            BuilderMode.OneWayPlatform => "Drag on a cube face to draw a one-way platform rectangle.",
            BuilderMode.Erase => "Left-click a platform, pole, or cube to erase it. Player and exit are preserved.",
            _ => ""
        };
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (!toolActive || EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        Scene scene = SceneManager.GetActiveScene();

        RefreshGridOrigin();

        if (!scene.IsValid() || !scene.isLoaded)
            return;

        if (platformCube != null && platformCube.gameObject.scene != scene)
            platformCube = null;

        Event current = Event.current;
        int controlId = GUIUtility.GetControlID(SceneControlHint, FocusType.Passive);

        if (platformCube == null && GUIUtility.hotControl == controlId)
            GUIUtility.hotControl = 0;

        if (platformCube != null && current.type == EventType.KeyDown &&
            current.keyCode == KeyCode.Escape)
        {
            platformCube = null;
            GUIUtility.hotControl = 0;
            statusMessage = "Platform placement cancelled.";
            current.Use();
            Repaint();
            sceneView.Repaint();
            return;
        }

        if (current.type == EventType.Layout)
            HandleUtility.AddDefaultControl(controlId);

        if (current.type == EventType.MouseMove || current.type == EventType.MouseDrag)
        {
            hasHoverPosition = TryGetGridPosition(current.mousePosition, out hoverPosition);

            if (platformCube != null && current.type == EventType.MouseDrag)
            {
                UpdatePlatformDrag(current.mousePosition);
                current.Use();
            }

            sceneView.Repaint();
        }

        if (current.type == EventType.Repaint)
            DrawSceneGuides();

        if (platformCube != null && current.type == EventType.MouseUp && current.button == 0)
        {
            UpdatePlatformDrag(current.mousePosition);
            FinishPlatformDrag(scene);
            GUIUtility.hotControl = 0;
            current.Use();
            Repaint();
            sceneView.Repaint();
            return;
        }

        if (current.type != EventType.MouseDown || current.button != 0 ||
            current.alt || current.control || current.command)
            return;

        validationMessages.Clear();

        switch (mode)
        {
            case BuilderMode.Cube:
                PlaceCube(scene, current.mousePosition);
                break;

            case BuilderMode.Connect:
                SelectConnectionCube(scene, current.mousePosition);
                break;

            case BuilderMode.Player:
                PlaceOnCubeFace<PlayerMovement>(scene, current.mousePosition, playerPrefab);
                break;

            case BuilderMode.Exit:
                PlaceOnCubeFace<Exit>(scene, current.mousePosition, exitPrefab);
                break;

            case BuilderMode.Floor:
            case BuilderMode.OneWayPlatform:
                BeginPlatformDrag(scene, current.mousePosition);

                if (platformCube != null)
                    GUIUtility.hotControl = controlId;

                break;

            case BuilderMode.Erase:
                Erase(scene, current.mousePosition);
                break;
        }

        current.Use();
        Repaint();
        sceneView.Repaint();
    }

    private void DrawSceneGuides()
    {
        if (mode == BuilderMode.Cube)
        {
            DrawGrid();

            if (hasHoverPosition)
            {
                Handles.color = Color.cyan;
                Handles.DrawWireCube(hoverPosition, CubePreviewSize());
            }
        }

        if (mode == BuilderMode.Connect && firstConnectionCube != null)
        {
            Handles.color = Color.yellow;
            BoxCollider box = firstConnectionCube.GetComponent<BoxCollider>();
            Vector3 size = box == null ? CubePreviewSize() : box.bounds.size;
            Handles.DrawWireCube(firstConnectionCube.transform.position, size * 1.05f);
        }

        if (platformCube != null)
            DrawPlatformPreview();
    }

    private Vector3 CubePreviewSize()
    {
        if (cubePrefab == null)
            return Vector3.one * gridSpacing;

        BoxCollider box = cubePrefab.GetComponent<BoxCollider>();

        if (box == null)
            return Vector3.one * gridSpacing;

        Vector3 scale = cubePrefab.transform.localScale;

        return new Vector3(
            Mathf.Abs(box.size.x * scale.x),
            Mathf.Abs(box.size.y * scale.y),
            Mathf.Abs(box.size.z * scale.z));
    }

    private void DrawGrid()
    {
        GetPlaneAxes(out Vector3 normal, out Vector3 horizontal, out Vector3 vertical);

        Vector3 center = hasHoverPosition ? hoverPosition : gridOrigin + normal * layer * gridSpacing;
        Handles.color = new Color(0.2f, 0.8f, 0.9f, 0.3f);

        for (int index = -6; index <= 6; index++)
        {
            Vector3 offset = horizontal * index * gridSpacing;
            Handles.DrawLine(center + offset - vertical * 6f * gridSpacing,
                center + offset + vertical * 6f * gridSpacing);

            offset = vertical * index * gridSpacing;
            Handles.DrawLine(center + offset - horizontal * 6f * gridSpacing,
                center + offset + horizontal * 6f * gridSpacing);
        }
    }

    private bool TryGetGridPosition(Vector2 mousePosition, out Vector3 position)
    {
        GetPlaneAxes(out Vector3 normal, out _, out _);
        Plane plane = new(normal, gridOrigin + normal * layer * gridSpacing);
        Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);

        if (!plane.Raycast(ray, out float distance))
        {
            position = default;
            return false;
        }

        Vector3 cell = (ray.GetPoint(distance) - gridOrigin) / gridSpacing;

        switch (layerAxis)
        {
            case LayerAxis.X:
                cell.x = layer;
                break;
            case LayerAxis.Y:
                cell.y = layer;
                break;
            case LayerAxis.Z:
                cell.z = layer;
                break;
        }

        position = gridOrigin + (Vector3)Vector3Int.RoundToInt(cell) * gridSpacing;
        return true;
    }

    private void GetPlaneAxes(out Vector3 normal, out Vector3 horizontal,
        out Vector3 vertical)
    {
        switch (layerAxis)
        {
            case LayerAxis.X:
                normal = Vector3.right;
                horizontal = Vector3.forward;
                vertical = Vector3.up;
                break;
            case LayerAxis.Y:
                normal = Vector3.up;
                horizontal = Vector3.right;
                vertical = Vector3.forward;
                break;
            default:
                normal = Vector3.forward;
                horizontal = Vector3.right;
                vertical = Vector3.up;
                break;
        }
    }

    private void PlaceCube(Scene scene, Vector2 mousePosition)
    {
        if (cubePrefab == null || cubePrefab.GetComponent<BlockNode>() == null)
        {
            statusMessage = "Assign a prefab with a BlockNode component.";
            return;
        }

        if (!TryGetGridPosition(mousePosition, out Vector3 position))
        {
            statusMessage = "The mouse ray does not reach this grid layer.";
            return;
        }

        foreach (BlockNode cube in LevelBuilderGraph.FindInScene<BlockNode>(scene))
        {
            if (Vector3.Distance(cube.transform.position, position) < gridSpacing * 0.1f)
            {
                statusMessage = "A cube already occupies that grid cell.";
                return;
            }
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(cubePrefab, scene);
        Undo.RegisterCreatedObjectUndo(instance, "Place Cube");

        Transform puzzleRoot = LevelBuilderGraph.FindPuzzleRoot(scene);

        if (puzzleRoot != null)
            Undo.SetTransformParent(instance.transform, puzzleRoot, "Parent Cube to Puzzle");

        // The current Cube prefab contains a null placeholder in Connections.
        BlockNode node = instance.GetComponent<BlockNode>();
        Undo.RecordObject(node, "Initialize Cube Connections");
        node.Connections = new List<Pole>();

        Undo.RecordObject(instance.transform, "Position Cube");
        instance.transform.position = position;
        PrefabUtility.RecordPrefabInstancePropertyModifications(node);
        PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
        EditorSceneManager.MarkSceneDirty(scene);
        statusMessage = $"Placed cube at {position}.";
    }

    private void RefreshGridOrigin()
    {
        Scene scene = SceneManager.GetActiveScene();

        if (!scene.IsValid() || !scene.isLoaded || gridScenePath == scene.path)
            return;

        Transform puzzleRoot = LevelBuilderGraph.FindPuzzleRoot(scene);
        gridOrigin = puzzleRoot == null ? Vector3.zero : puzzleRoot.position;
        gridScenePath = scene.path;
    }

    private void SelectConnectionCube(Scene scene, Vector2 mousePosition)
    {
        BlockNode cube = PickComponent<BlockNode>(mousePosition);

        if (cube == null || cube.gameObject.scene != scene)
        {
            statusMessage = "Click a cube to make a connection.";
            return;
        }

        if (firstConnectionCube != null && firstConnectionCube.gameObject.scene != scene)
            firstConnectionCube = null;

        if (firstConnectionCube == null)
        {
            firstConnectionCube = cube;
            statusMessage = "Click a neighboring cube.";
            return;
        }

        if (firstConnectionCube == cube)
        {
            firstConnectionCube = null;
            statusMessage = "Connection selection cleared.";
            return;
        }

        if (LevelBuilderGraph.Connect(scene, firstConnectionCube, cube,
            polePrefab, gridSpacing, out string error))
        {
            statusMessage = "Cubes connected.";
            firstConnectionCube = null;
        }
        else
        {
            statusMessage = error;
        }
    }

    private void PlaceOnCubeFace<T>(Scene scene, Vector2 mousePosition,
        GameObject prefab) where T : Component
    {
        if (!TryGetCubeFace(scene, mousePosition, out BlockNode cube,
            out Vector3 normal, out Vector3 facePoint, out _))
        {
            statusMessage = "Click a visible cube face.";
            return;
        }

        if (cube.HasConnectionOnFace(normal))
        {
            statusMessage = "That face is occupied by a connection.";
            return;
        }

        List<T> objects = LevelBuilderGraph.FindInScene<T>(scene);

        if (objects.Count > 1)
        {
            statusMessage = $"This scene has multiple {typeof(T).Name} objects. Resolve them before placement.";
            return;
        }

        GameObject instance;

        if (objects.Count == 1)
        {
            instance = objects[0].gameObject;
        }
        else
        {
            if (prefab == null || prefab.GetComponent<T>() == null)
            {
                statusMessage = $"Assign a prefab with a {typeof(T).Name} component.";
                return;
            }

            instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            Undo.RegisterCreatedObjectUndo(instance, $"Place {typeof(T).Name}");

            if (typeof(T) == typeof(PlayerMovement))
                WireNewPlayer(scene, instance.GetComponent<PlayerMovement>());
        }

        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName($"Place {typeof(T).Name} on Cube");

        Undo.RecordObject(instance, "Activate Placement");
        instance.SetActive(true);
        Undo.SetTransformParent(instance.transform, cube.transform, "Attach to Cube");
        Undo.RecordObject(instance.transform, "Position on Cube Face");

        instance.transform.rotation = FaceRotation(normal);
        instance.transform.position = facePoint + normal * 0.03f;

        PrefabUtility.RecordPrefabInstancePropertyModifications(instance);
        PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
        EditorSceneManager.MarkSceneDirty(scene);
        Undo.CollapseUndoOperations(undoGroup);

        statusMessage = $"Placed {typeof(T).Name} on {cube.name}.";
    }

    private void BeginPlatformDrag(Scene scene, Vector2 mousePosition)
    {
        GameObject prefab = mode == BuilderMode.Floor ? floorPrefab : oneWayPlatformPrefab;

        if (prefab == null || prefab.GetComponent<BoxCollider>() == null ||
            (mode == BuilderMode.Floor &&
             prefab.GetComponent<SurfaceAttachHelper>() == null) ||
            (mode == BuilderMode.OneWayPlatform &&
             (prefab.GetComponent<OneWayPlatform>() == null ||
              prefab.GetComponentInChildren<OneWayPlatformTrigger>() == null)))
        {
            statusMessage = "Assign the matching platform prefab with a BoxCollider.";
            return;
        }

        if (!TryGetCubeFace(scene, mousePosition, out BlockNode cube,
            out Vector3 normal, out Vector3 faceCenter, out Vector3 hitPoint))
        {
            statusMessage = "Start the drag on a visible cube face.";
            return;
        }

        if (cube.HasConnectionOnFace(normal))
        {
            statusMessage = "That face is occupied by a connection.";
            return;
        }

        platformCube = cube;
        platformNormal = normal;
        platformFaceCenter = faceCenter;
        platformStart = hitPoint;
        platformEnd = hitPoint;
        platformMode = mode;
        statusMessage = "Drag to the opposite corner, then release.";
    }

    private void UpdatePlatformDrag(Vector2 mousePosition)
    {
        if (platformCube == null)
            return;

        Plane facePlane = new(platformNormal, platformFaceCenter);
        Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);

        if (!facePlane.Raycast(ray, out float distance))
            return;

        BoxCollider box = platformCube.GetComponent<BoxCollider>();

        if (box == null)
            return;

        Vector3 localPoint = platformCube.transform.InverseTransformPoint(ray.GetPoint(distance));
        Vector3 lower = box.center - box.size * 0.5f;
        Vector3 upper = box.center + box.size * 0.5f;

        localPoint.x = Mathf.Clamp(localPoint.x, lower.x, upper.x);
        localPoint.y = Mathf.Clamp(localPoint.y, lower.y, upper.y);
        localPoint.z = Mathf.Clamp(localPoint.z, lower.z, upper.z);

        Vector3 localNormal = DominantAxis(
            platformCube.transform.InverseTransformDirection(platformNormal));

        if (Mathf.Abs(localNormal.x) > 0.5f)
            localPoint.x = box.center.x + localNormal.x * box.size.x * 0.5f;
        else if (Mathf.Abs(localNormal.y) > 0.5f)
            localPoint.y = box.center.y + localNormal.y * box.size.y * 0.5f;
        else
            localPoint.z = box.center.z + localNormal.z * box.size.z * 0.5f;

        platformEnd = platformCube.transform.TransformPoint(localPoint);
    }

    private void GetPlatformShape(out Vector3 center, out Quaternion rotation,
        out float width, out float height)
    {
        rotation = FaceRotation(platformNormal);
        Vector3 distance = Quaternion.Inverse(rotation) * (platformEnd - platformStart);

        width = Mathf.Max(Mathf.Abs(distance.x), 0.5f);
        height = Mathf.Max(Mathf.Abs(distance.y),
            platformMode == BuilderMode.Floor ? 0.6f : 0.3f);
        center = (platformStart + platformEnd) * 0.5f + platformNormal * 0.01f;
    }

    private void DrawPlatformPreview()
    {
        GetPlatformShape(out Vector3 center, out Quaternion rotation,
            out float width, out float height);

        Matrix4x4 previousMatrix = Handles.matrix;
        Handles.matrix = Matrix4x4.TRS(center, rotation, Vector3.one);
        Handles.color = platformMode == BuilderMode.Floor ? Color.green : Color.magenta;
        Handles.DrawWireCube(Vector3.zero, new Vector3(width, height, 0.08f));
        Handles.matrix = previousMatrix;
    }

    private void FinishPlatformDrag(Scene scene)
    {
        BlockNode cube = platformCube;
        platformCube = null;

        if (cube == null || cube.gameObject.scene != scene)
            return;

        GameObject prefab = platformMode == BuilderMode.Floor ? floorPrefab : oneWayPlatformPrefab;

        if (prefab == null)
            return;

        GetPlatformShape(out Vector3 center, out Quaternion rotation,
            out float width, out float height);

        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Place Platform");

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        Undo.RegisterCreatedObjectUndo(instance, "Place Platform");
        Undo.SetTransformParent(instance.transform, cube.transform, "Attach Platform to Cube");

        BoxCollider box = instance.GetComponent<BoxCollider>();
        float depth = platformMode == BuilderMode.Floor ? 0.25f : 0.2f;
        Transform parent = cube.transform;

        Undo.RecordObject(instance.transform, "Size Platform");
        instance.transform.rotation = rotation;
        instance.transform.position = center;
        instance.transform.localScale = new Vector3(
            width * parent.InverseTransformVector(rotation * Vector3.right).magnitude / box.size.x,
            height * parent.InverseTransformVector(rotation * Vector3.up).magnitude / box.size.y,
            depth * parent.InverseTransformVector(rotation * Vector3.forward).magnitude / box.size.z);

        PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
        EditorSceneManager.MarkSceneDirty(scene);
        Undo.CollapseUndoOperations(undoGroup);

        statusMessage = platformMode == BuilderMode.Floor ?
            "Floor placed on cube face." : "One-way platform placed on cube face.";
    }

    private static Quaternion FaceRotation(Vector3 normal)
    {
        Vector3 up = Vector3.ProjectOnPlane(Vector3.up, normal);

        if (up.sqrMagnitude < 0.01f)
            up = Vector3.ProjectOnPlane(Vector3.forward, normal);

        return Quaternion.LookRotation(-normal, up.normalized);
    }

    private static void WireNewPlayer(Scene scene, PlayerMovement player)
    {
        List<MessageController> messages = LevelBuilderGraph.FindInScene<MessageController>(scene);

        if (messages.Count > 0)
            SetObjectReference(player, "messageController", messages[0]);

        foreach (PuzzleDragRotator rotator in LevelBuilderGraph.FindInScene<PuzzleDragRotator>(scene))
            SetObjectReference(rotator, "playerMovement", player);

        foreach (Bound bound in LevelBuilderGraph.FindInScene<Bound>(scene))
            SetObjectReference(bound, "target", player.transform);

        foreach (CameraOrbit orbit in LevelBuilderGraph.FindInScene<CameraOrbit>(scene))
            SetObjectReference(orbit, "player", player.transform);
    }

    private static void SetObjectReference(Object owner, string name, Object value)
    {
        SerializedObject serializedOwner = new(owner);
        SerializedProperty property = serializedOwner.FindProperty(name);

        if (property == null)
            return;

        property.objectReferenceValue = value;
        serializedOwner.ApplyModifiedProperties();
        PrefabUtility.RecordPrefabInstancePropertyModifications(owner);
    }

    private bool TryGetCubeFace(Scene scene, Vector2 mousePosition,
        out BlockNode cube, out Vector3 normal, out Vector3 facePoint,
        out Vector3 hitPoint)
    {
        cube = null;
        normal = default;
        facePoint = default;
        hitPoint = default;

        Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 10000f);
        float nearest = float.PositiveInfinity;

        foreach (RaycastHit hit in hits)
        {
            BlockNode candidate = hit.collider.GetComponentInParent<BlockNode>();

            if (candidate == null || candidate.gameObject.scene != scene ||
                hit.distance >= nearest)
                continue;

            BoxCollider box = candidate.GetComponent<BoxCollider>();

            if (box == null || hit.collider != box)
                continue;

            Vector3 localNormal = candidate.transform.InverseTransformDirection(hit.normal);
            localNormal = DominantAxis(localNormal);

            cube = candidate;
            normal = candidate.transform.TransformDirection(localNormal).normalized;
            facePoint = candidate.transform.TransformPoint(
                box.center + Vector3.Scale(localNormal, box.size * 0.5f));
            hitPoint = hit.point;
            nearest = hit.distance;
        }

        return cube != null;
    }

    private static Vector3 DominantAxis(Vector3 vector)
    {
        Vector3 absolute = new(Mathf.Abs(vector.x), Mathf.Abs(vector.y), Mathf.Abs(vector.z));

        if (absolute.x >= absolute.y && absolute.x >= absolute.z)
            return vector.x >= 0f ? Vector3.right : Vector3.left;

        if (absolute.y >= absolute.z)
            return vector.y >= 0f ? Vector3.up : Vector3.down;

        return vector.z >= 0f ? Vector3.forward : Vector3.back;
    }

    private void Erase(Scene scene, Vector2 mousePosition)
    {
        GameObject picked = HandleUtility.PickGameObject(mousePosition, false);

        if (picked == null || picked.scene != scene)
            return;

        OneWayPlatform oneWay = picked.GetComponentInParent<OneWayPlatform>();

        if (oneWay != null)
        {
            Undo.DestroyObjectImmediate(oneWay.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            statusMessage = "One-way platform erased.";
            return;
        }

        SurfaceAttachHelper surface = picked.GetComponentInParent<SurfaceAttachHelper>();

        if (surface != null && surface.name.StartsWith("Floor") &&
            IsStandaloneFloor(surface.gameObject))
        {
            Undo.DestroyObjectImmediate(surface.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            statusMessage = "Floor erased.";
            return;
        }

        Pole pole = picked.GetComponentInParent<Pole>();

        if (pole != null)
        {
            LevelBuilderGraph.DeletePole(pole);
            statusMessage = "Pole erased.";
            return;
        }

        BlockNode cube = picked.GetComponentInParent<BlockNode>();

        if (cube != null)
        {
            if (firstConnectionCube == cube)
                firstConnectionCube = null;

            LevelBuilderGraph.DeleteCube(cube);
            statusMessage = "Cube and its connections erased.";
        }
    }

    private bool IsStandaloneFloor(GameObject floor)
    {
        Object source = PrefabUtility.GetCorrespondingObjectFromSource(floor);
        return source == null || source == floorPrefab;
    }

    private static T PickComponent<T>(Vector2 mousePosition) where T : Component
    {
        GameObject picked = HandleUtility.PickGameObject(mousePosition, false);
        return picked == null ? null : picked.GetComponentInParent<T>();
    }

    private void OnUndoRedo()
    {
        if (firstConnectionCube == null)
            firstConnectionCube = null;

        validationMessages.Clear();
        SceneView.RepaintAll();
        Repaint();
    }
}
