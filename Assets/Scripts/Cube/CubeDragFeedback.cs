using UnityEngine;
using UnityEngine.Rendering;

// Owns the temporary visuals for one cube drag. The indicator has no colliders.
public class CubeDragFeedback
{
    private static readonly Vector2[] ArrowContour =
    {
        new(-0.04f, 0.16f),
        new( 0.04f, 0.16f),
        new( 0.04f, 0.58f),
        new( 0.13f, 0.58f),
        new( 0f,    0.80f),
        new(-0.13f, 0.58f),
        new(-0.04f, 0.58f)
    };

    private readonly CubeDragFeedbackSettings settings;
    private readonly Vector3[] arrowOutlineVertices = new Vector3[ArrowContour.Length * 2];

    // Render objects are created once and reused between drags.
    private GameObject root;
    private Transform arrow;
    private Transform ring;
    private Mesh arrowMesh;
    private Mesh arrowOutlineMesh;
    private Mesh ringMesh;
    private MeshFilter arrowOutlineFilter;
    private MeshRenderer arrowOutlineRenderer;
    private MeshFilter ringFilter;
    private MeshRenderer ringRenderer;
    private Material sphereMaterial;
    private Material arrowMaterial;
    private Material arrowOutlineMaterial;
    private Material ringMaterial;
    private float currentOutlineThickness = -1f;
    private float currentOutlineStretch = -1f;
    private float currentRingThicknessPixels = -1f;
    private float currentRingRadiusPixels = -1f;
    private Camera ringCamera;
    private float ringRadiusPixels;
    private Vector3 faceRightLocal;
    private Vector3 faceUpLocal;

    // Current values ease toward targets during Tick.
    private float sphereOpacity;
    private float arrowOpacity;
    private float targetSphereOpacity;
    private float targetArrowOpacity;
    private float arrowStretch;
    private float targetArrowStretch;
    private float stretchVelocity;
    private float arrowAngle;
    private float targetArrowAngle;

    public CubeDragFeedback(CubeDragFeedbackSettings settings)
    {
        this.settings = settings;
    }

    public void Show(CubeDragFace face, Camera camera,
        float cancelRadiusPixels, Renderer cubeRenderer)
    {
        ringCamera = camera;
        ringRadiusPixels = cancelRadiusPixels;

        if (root == null)
            CreateVisuals(cubeRenderer);

        if (root == null)
            return;

        // The face data also drives cursor interpretation in PuzzleDragRotator.
        faceRightLocal = face.LocalRight;
        faceUpLocal = face.LocalUp;

        root.transform.SetPositionAndRotation(
            face.IndicatorCenterWorld, face.IndicatorRotation);
        root.transform.localScale = Vector3.one * face.SizeWorld * 0.4f;

        // A fresh drag begins with a visible center and a hidden, short arrow.
        sphereOpacity = 0f;
        arrowOpacity = 0f;
        targetSphereOpacity = 1f;
        targetArrowOpacity = 0f;
        arrowStretch = 0.25f;
        targetArrowStretch = 0.25f;
        stretchVelocity = 0f;
        arrowAngle = 0f;
        targetArrowAngle = 0f;
        arrow.localRotation = Quaternion.identity;
        arrow.localScale = new Vector3(1f, arrowStretch, 1f);
        SetOpacity(sphereMaterial, settings.SphereColor, 0f);
        SetOpacity(arrowMaterial, settings.ArrowColor, 0f);
        SetOpacity(arrowOutlineMaterial, settings.ArrowOutlineColor, 0f);
        SetOpacity(ringMaterial, settings.CancelRingColor, 0f);
        UpdateOutlineMesh();
        UpdateRingMesh();
        root.SetActive(true);
        UpdateRing();
    }

    public void ShowDirection(Vector3 directionLocal, float dragStrength)
    {
        if (root == null || !root.activeSelf)
            return;

        Vector3 faceDirection = new Vector3(
            Vector3.Dot(directionLocal, faceRightLocal),
            Vector3.Dot(directionLocal, faceUpLocal), 0f);
        // Rotate only about the face normal; a 180-degree 3D flip hides the mesh.
        targetArrowAngle = Mathf.Atan2(-faceDirection.x, faceDirection.y) * Mathf.Rad2Deg;
        targetArrowOpacity = 1f;
        float stretch = Mathf.Clamp01(dragStrength /
            Mathf.Max(1f, settings.FullStretchPixels));
        targetArrowStretch = Mathf.Lerp(settings.MinimumArrowLength,
            Mathf.Max(settings.MinimumArrowLength, settings.MaximumArrowLength), stretch);
    }

    public void CancelDirection()
    {
        targetArrowOpacity = 0f;
        targetArrowStretch = 0f;
    }

    public void Hide()
    {
        targetSphereOpacity = 0f;
        CancelDirection();
    }

    public void HideImmediately()
    {
        Hide();
        if (root != null)
            root.SetActive(false);
    }

    public void Tick(float deltaTime)
    {
        if (root == null || !root.activeSelf)
            return;

        AdvanceAnimation(deltaTime);
        UpdateOutlineMesh();
        UpdateRingMesh();
        ApplyAnimation();
        UpdateRing();

        if (sphereOpacity <= 0f && arrowOpacity <= 0f &&
            arrowStretch <= 0.03f && targetSphereOpacity <= 0f)
            root.SetActive(false);
    }

    private void AdvanceAnimation(float deltaTime)
    {
        float fadeStep = deltaTime / Mathf.Max(0.01f, settings.FadeInSeconds);
        sphereOpacity = Mathf.MoveTowards(sphereOpacity, targetSphereOpacity, fadeStep);

        float arrowFadeStep = deltaTime /
            Mathf.Max(0.01f, targetArrowOpacity < arrowOpacity
                ? settings.FadeOutSeconds
                : settings.FadeInSeconds);
        arrowOpacity = Mathf.MoveTowards(arrowOpacity, targetArrowOpacity, arrowFadeStep);

        arrowStretch = Mathf.SmoothDamp(arrowStretch, targetArrowStretch,
            ref stretchVelocity, Mathf.Max(0.01f, settings.StretchSeconds),
            Mathf.Infinity, deltaTime);

        arrowAngle = Mathf.LerpAngle(arrowAngle, targetArrowAngle,
            1f - Mathf.Exp(-18f * deltaTime));
    }

    private void ApplyAnimation()
    {
        SetOpacity(sphereMaterial, settings.SphereColor, sphereOpacity);
        SetOpacity(arrowMaterial, settings.ArrowColor, arrowOpacity);
        SetOpacity(arrowOutlineMaterial, settings.ArrowOutlineColor, arrowOpacity);
        SetOpacity(ringMaterial, settings.CancelRingColor, sphereOpacity * 0.8f);

        arrow.localScale = new Vector3(1f, arrowStretch, 1f);
        arrow.localRotation = Quaternion.AngleAxis(arrowAngle, Vector3.forward);
    }

    public void Dispose()
    {
        if (root != null)
            Object.Destroy(root);
        if (arrowMesh != null)
            Object.Destroy(arrowMesh);
        if (arrowOutlineMesh != null)
            Object.Destroy(arrowOutlineMesh);
        if (ringMesh != null)
            Object.Destroy(ringMesh);
        if (sphereMaterial != null)
            Object.Destroy(sphereMaterial);
        if (arrowMaterial != null)
            Object.Destroy(arrowMaterial);
        if (arrowOutlineMaterial != null)
            Object.Destroy(arrowOutlineMaterial);
        if (ringMaterial != null)
            Object.Destroy(ringMaterial);
    }

    private void CreateVisuals(Renderer cubeRenderer)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ??
            Shader.Find("Sprites/Default") ??
            Shader.Find("Unlit/Transparent") ??
            (cubeRenderer != null && cubeRenderer.sharedMaterial != null
                ? cubeRenderer.sharedMaterial.shader
                : null);
        if (shader == null)
        {
            Debug.LogError("Cube drag feedback needs an unlit shader.");
            return;
        }

        sphereMaterial = CreateMaterial(shader);
        arrowMaterial = CreateMaterial(shader);
        arrowOutlineMaterial = CreateMaterial(shader);
        ringMaterial = CreateMaterial(shader);
        arrowOutlineMaterial.renderQueue = (int)RenderQueue.Transparent + 1;

        root = new GameObject("Cube Drag Feedback");
        root.layer = 2; // Ignore Raycast

        CreateSphere();
        CreateArrow();
        CreateRing();
    }

    private void CreateSphere()
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "Face Center";
        sphere.layer = 2;
        sphere.transform.SetParent(root.transform, false);
        sphere.transform.localScale = Vector3.one * 0.28f;

        Renderer sphereRenderer = sphere.GetComponent<Renderer>();
        sphereRenderer.sharedMaterial = sphereMaterial;
        Object.Destroy(sphere.GetComponent<Collider>());
        DisableShadows(sphereRenderer);
    }

    private void CreateArrow()
    {
        GameObject arrowObject = new GameObject("Drag Direction",
            typeof(MeshFilter), typeof(MeshRenderer));
        arrowObject.layer = 2;
        arrowObject.transform.SetParent(root.transform, false);

        arrow = arrowObject.transform;
        arrowMesh = CreateArrowMesh();
        arrowObject.GetComponent<MeshFilter>().sharedMesh = arrowMesh;

        MeshRenderer arrowRenderer = arrowObject.GetComponent<MeshRenderer>();
        arrowRenderer.sharedMaterial = arrowMaterial;
        DisableShadows(arrowRenderer);

        // The outline shares the arrow's turn and stretch, but uses its own color.
        GameObject outlineObject = new GameObject("Arrow Outline",
            typeof(MeshFilter), typeof(MeshRenderer));
        outlineObject.layer = 2;
        outlineObject.transform.SetParent(arrow, false);

        arrowOutlineFilter = outlineObject.GetComponent<MeshFilter>();
        arrowOutlineRenderer = outlineObject.GetComponent<MeshRenderer>();
        arrowOutlineMesh = CreateArrowOutlineMesh();
        arrowOutlineFilter.sharedMesh = arrowOutlineMesh;
        arrowOutlineRenderer.sharedMaterial = arrowOutlineMaterial;
        DisableShadows(arrowOutlineRenderer);
        UpdateOutlineMesh();
    }

    private void UpdateOutlineMesh()
    {
        float thickness = Mathf.Clamp(settings.ArrowOutlineThickness, 0f, 0.08f);
        arrowOutlineRenderer.enabled = thickness > 0f;
        if (thickness <= 0f ||
            (Mathf.Approximately(thickness, currentOutlineThickness) &&
             Mathf.Approximately(arrowStretch, currentOutlineStretch)))
            return;

        // Offset in stretched space, then undo the stretch for the child mesh.
        float stretch = Mathf.Max(0.01f, arrowStretch);
        float effectiveThickness = thickness * Mathf.Clamp01(
            stretch / Mathf.Max(0.01f, settings.MinimumArrowLength));
        int count = ArrowContour.Length;

        for (int i = 0; i < count; i++)
        {
            Vector2 previous = ArrowContour[(i + count - 1) % count];
            Vector2 current = ArrowContour[i];
            Vector2 next = ArrowContour[(i + 1) % count];

            Vector2 incoming = new Vector2(
                current.x - previous.x, (current.y - previous.y) * stretch).normalized;
            Vector2 outgoing = new Vector2(
                next.x - current.x, (next.y - current.y) * stretch).normalized;
            Vector2 previousNormal = new(incoming.y, -incoming.x);
            Vector2 nextNormal = new(outgoing.y, -outgoing.x);
            Vector2 miter = (previousNormal + nextNormal).normalized;
            Vector2 offset = miter * (effectiveThickness /
                Mathf.Max(0.1f, Vector2.Dot(miter, nextNormal)));

            arrowOutlineVertices[i * 2] = new Vector3(
                current.x, current.y, 0.065f);
            arrowOutlineVertices[i * 2 + 1] = new Vector3(
                current.x + offset.x, current.y + offset.y / stretch, 0.065f);
        }

        arrowOutlineMesh.vertices = arrowOutlineVertices;
        arrowOutlineMesh.RecalculateBounds();
        currentOutlineThickness = thickness;
        currentOutlineStretch = arrowStretch;
    }

    private void CreateRing()
    {
        GameObject ringObject = new GameObject("Cancel Zone",
            typeof(MeshFilter), typeof(MeshRenderer));
        ringObject.layer = 2;
        ringObject.transform.SetParent(root.transform, false);

        ring = ringObject.transform;
        ringFilter = ringObject.GetComponent<MeshFilter>();

        ringRenderer = ringObject.GetComponent<MeshRenderer>();
        ringRenderer.sharedMaterial = ringMaterial;
        DisableShadows(ringRenderer);
        UpdateRingMesh();
    }

    private void UpdateRingMesh()
    {
        float thicknessPixels = Mathf.Clamp(settings.CancelRingThicknessPixels,
            0f, Mathf.Max(1f, ringRadiusPixels));
        if (Mathf.Approximately(thicknessPixels, currentRingThicknessPixels) &&
            Mathf.Approximately(ringRadiusPixels, currentRingRadiusPixels))
            return;

        // The ring has a fixed pixel radius, so its thickness is measured in pixels too.
        ringRenderer.enabled = thicknessPixels > 0f;
        if (ringMesh != null)
            Object.Destroy(ringMesh);

        ringMesh = thicknessPixels > 0f
            ? CreateDottedRingMesh(1f - thicknessPixels / ringRadiusPixels)
            : null;
        ringFilter.sharedMesh = ringMesh;
        currentRingThicknessPixels = thicknessPixels;
        currentRingRadiusPixels = ringRadiusPixels;
    }

    private void UpdateRing()
    {
        if (ringCamera == null || ring == null)
            return;

        Vector3 screenCenter = ringCamera.WorldToScreenPoint(root.transform.position);
        if (screenCenter.z <= 0f)
        {
            ring.gameObject.SetActive(false);
            return;
        }

        ring.gameObject.SetActive(true);
        Vector3 screenEdge = screenCenter + Vector3.right * ringRadiusPixels;
        Vector3 worldEdge = ringCamera.ScreenToWorldPoint(screenEdge);
        float worldRadius = Vector3.Distance(root.transform.position, worldEdge);

        // Face the camera so the visible circle matches the screen-space cancel radius.
        ring.position = root.transform.position -
            ringCamera.transform.forward * worldRadius * 0.12f;
        ring.rotation = Quaternion.LookRotation(
            -ringCamera.transform.forward, ringCamera.transform.up);
        ring.localScale = Vector3.one *
            (worldRadius / root.transform.lossyScale.x);
    }

    private static Material CreateMaterial(Shader shader)
    {
        Material material = new Material(shader);
        if (material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }
        material.SetOverrideTag("RenderType", "Transparent");
        material.renderQueue = (int)RenderQueue.Transparent;
        if (material.HasProperty("_BaseMap"))
            material.SetTexture("_BaseMap", Texture2D.whiteTexture);
        if (material.HasProperty("_MainTex"))
            material.SetTexture("_MainTex", Texture2D.whiteTexture);
        SetOpacity(material, Color.white, 0f);
        return material;
    }

    private static void SetOpacity(Material material, Color baseColor, float opacity)
    {
        Color color = new Color(baseColor.r, baseColor.g, baseColor.b,
            baseColor.a * opacity);
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
    }

    private static void DisableShadows(Renderer renderer)
    {
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private static Mesh CreateArrowMesh()
    {
        Mesh mesh = new Mesh { name = "Cube Drag Arrow" };
        mesh.vertices = new[]
        {
            new Vector3(-0.04f, 0.16f, 0.06f),
            new Vector3( 0.04f, 0.16f, 0.06f),
            new Vector3( 0.04f, 0.60f, 0.06f),
            new Vector3( 0.13f, 0.58f, 0.06f),
            new Vector3( 0f,    0.80f, 0.06f),
            new Vector3(-0.13f, 0.58f, 0.06f),
            new Vector3(-0.04f, 0.60f, 0.06f)
        };
        mesh.triangles = new[] { 0, 1, 2, 0, 2, 6, 5, 3, 4 };
        mesh.RecalculateNormals();
        return mesh;
    }

    private static Mesh CreateArrowOutlineMesh()
    {
        int count = ArrowContour.Length;
        Vector3[] vertices = new Vector3[count * 2];
        Vector3[] normals = new Vector3[count * 2];
        int[] triangles = new int[count * 6];

        for (int i = 0; i < count; i++)
        {
            normals[i * 2] = Vector3.forward;
            normals[i * 2 + 1] = Vector3.forward;

            int following = (i + 1) % count;
            int triangle = i * 6;
            triangles[triangle] = i * 2;
            triangles[triangle + 1] = i * 2 + 1;
            triangles[triangle + 2] = following * 2 + 1;
            triangles[triangle + 3] = i * 2;
            triangles[triangle + 4] = following * 2 + 1;
            triangles[triangle + 5] = following * 2;
        }

        Mesh mesh = new Mesh { name = "Cube Drag Arrow Outline" };
        mesh.MarkDynamic();
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.triangles = triangles;
        return mesh;
    }

    private static Mesh CreateDottedRingMesh(float innerRadius)
    {
        const int dashCount = 18;
        Vector3[] vertices = new Vector3[dashCount * 4];
        int[] triangles = new int[dashCount * 6];

        for (int i = 0; i < dashCount; i++)
        {
            float start = i * Mathf.PI * 2f / dashCount;
            float end = start + Mathf.PI * 2f / dashCount * 0.42f;
            Vector3 startDirection = new(Mathf.Cos(start), Mathf.Sin(start), 0f);
            Vector3 endDirection = new(Mathf.Cos(end), Mathf.Sin(end), 0f);
            int vertex = i * 4;
            vertices[vertex] = startDirection;
            vertices[vertex + 1] = startDirection * innerRadius;
            vertices[vertex + 2] = endDirection * innerRadius;
            vertices[vertex + 3] = endDirection;

            int triangle = i * 6;
            triangles[triangle] = vertex;
            triangles[triangle + 1] = vertex + 2;
            triangles[triangle + 2] = vertex + 1;
            triangles[triangle + 3] = vertex;
            triangles[triangle + 4] = vertex + 3;
            triangles[triangle + 5] = vertex + 2;
        }

        Mesh mesh = new Mesh { name = "Cube Drag Cancel Ring" };
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        return mesh;
    }
}
