using UnityEngine;
using UnityEngine.Rendering;

// Owns the temporary visuals for one cube drag. The indicator has no colliders.
public sealed class CubeDragFeedback
{
    private static readonly Color SphereColor = new(1f, 0.82f, 0.2f);
    private static readonly Color InkColor = Color.black;

    private GameObject root;
    private Transform arrow;
    private Transform ring;
    private Mesh arrowMesh;
    private Mesh ringMesh;
    private Material sphereMaterial;
    private Material arrowMaterial;
    private Material ringMaterial;
    private Camera ringCamera;
    private float ringRadiusPixels;
    private Vector3 faceRightLocal;
    private Vector3 faceUpLocal;
    private float sphereOpacity;
    private float arrowOpacity;
    private float targetSphereOpacity;
    private float targetArrowOpacity;
    private float arrowStretch;
    private float targetArrowStretch;
    private float stretchVelocity;
    private float arrowAngle;
    private float targetArrowAngle;

    public Vector3 MarkerCenterWorld { get; private set; }

    public void Show(BlockNode block, BoxCollider cubeCollider,
        Vector3 faceNormalLocal, Vector3 rightLocal, Vector3 upLocal,
        Camera camera, float cancelRadiusPixels)
    {
        MarkerCenterWorld = block.transform.position;
        if (cubeCollider == null)
            return;

        if (root == null)
            CreateVisuals(block.GetComponent<Renderer>());

        if (root == null)
            return;

        faceRightLocal = rightLocal;
        faceUpLocal = upLocal;
        ringCamera = camera;
        ringRadiusPixels = cancelRadiusPixels;

        Vector3 size = cubeCollider.size;
        Vector3 scale = block.transform.lossyScale;
        size = Vector3.Scale(size, new Vector3(
            Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z)));

        float faceSize = Mathf.Abs(faceNormalLocal.x) > 0.5f
            ? Mathf.Min(size.y, size.z)
            : Mathf.Abs(faceNormalLocal.y) > 0.5f
                ? Mathf.Min(size.x, size.z)
                : Mathf.Min(size.x, size.y);

        Vector3 localFaceCenter = cubeCollider.center +
            Vector3.Scale(faceNormalLocal, cubeCollider.size) * 0.5f;
        Vector3 worldNormal = block.transform.TransformDirection(faceNormalLocal);

        MarkerCenterWorld =
            block.transform.TransformPoint(localFaceCenter) + worldNormal * faceSize * 0.015f;
        root.transform.SetPositionAndRotation(
            MarkerCenterWorld,
            Quaternion.LookRotation(worldNormal, block.transform.TransformDirection(upLocal)));
        root.transform.localScale = Vector3.one * faceSize * 0.4f;
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
        SetOpacity(sphereMaterial, SphereColor, 0f);
        SetOpacity(arrowMaterial, InkColor, 0f);
        SetOpacity(ringMaterial, InkColor, 0f);
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
        targetArrowStretch = 0.55f + 0.75f * Mathf.Clamp01(dragStrength / 80f);
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

        float fadeStep = deltaTime / 0.2f;
        sphereOpacity = Mathf.MoveTowards(sphereOpacity, targetSphereOpacity, fadeStep);
        float arrowFadeStep = deltaTime /
            (targetArrowOpacity < arrowOpacity ? 0.35f : 0.2f);
        arrowOpacity = Mathf.MoveTowards(arrowOpacity, targetArrowOpacity, arrowFadeStep);
        arrowStretch = Mathf.SmoothDamp(arrowStretch, targetArrowStretch,
            ref stretchVelocity, 0.14f, Mathf.Infinity, deltaTime);

        SetOpacity(sphereMaterial, SphereColor, sphereOpacity);
        SetOpacity(arrowMaterial, InkColor, arrowOpacity);
        SetOpacity(ringMaterial, InkColor, sphereOpacity * 0.8f);
        arrow.localScale = new Vector3(1f, arrowStretch, 1f);
        arrowAngle = Mathf.LerpAngle(arrowAngle, targetArrowAngle,
            1f - Mathf.Exp(-18f * deltaTime));
        arrow.localRotation = Quaternion.AngleAxis(arrowAngle, Vector3.forward);
        UpdateRing();

        if (sphereOpacity <= 0f && arrowOpacity <= 0f &&
            arrowStretch <= 0.03f && targetSphereOpacity <= 0f)
            root.SetActive(false);
    }

    public void Dispose()
    {
        if (root != null)
            Object.Destroy(root);
        if (arrowMesh != null)
            Object.Destroy(arrowMesh);
        if (ringMesh != null)
            Object.Destroy(ringMesh);
        if (sphereMaterial != null)
            Object.Destroy(sphereMaterial);
        if (arrowMaterial != null)
            Object.Destroy(arrowMaterial);
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
        ringMaterial = CreateMaterial(shader);

        root = new GameObject("Cube Drag Feedback");
        root.layer = 2; // Ignore Raycast

        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "Face Center";
        sphere.layer = 2;
        sphere.transform.SetParent(root.transform, false);
        sphere.transform.localScale = Vector3.one * 0.28f;
        sphere.GetComponent<Renderer>().sharedMaterial = sphereMaterial;
        Object.Destroy(sphere.GetComponent<Collider>());
        DisableShadows(sphere.GetComponent<Renderer>());

        GameObject arrowObject = new GameObject("Drag Direction", typeof(MeshFilter), typeof(MeshRenderer));
        arrowObject.layer = 2;
        arrowObject.transform.SetParent(root.transform, false);
        arrow = arrowObject.transform;
        arrowMesh = CreateArrowMesh();
        arrowObject.GetComponent<MeshFilter>().sharedMesh = arrowMesh;
        MeshRenderer arrowRenderer = arrowObject.GetComponent<MeshRenderer>();
        arrowRenderer.sharedMaterial = arrowMaterial;
        DisableShadows(arrowRenderer);

        GameObject ringObject = new GameObject("Cancel Zone", typeof(MeshFilter), typeof(MeshRenderer));
        ringObject.layer = 2;
        ringObject.transform.SetParent(root.transform, false);
        ring = ringObject.transform;
        ringMesh = CreateDottedRingMesh();
        ringObject.GetComponent<MeshFilter>().sharedMesh = ringMesh;
        MeshRenderer ringRenderer = ringObject.GetComponent<MeshRenderer>();
        ringRenderer.sharedMaterial = ringMaterial;
        DisableShadows(ringRenderer);
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
        Color color = new Color(baseColor.r, baseColor.g, baseColor.b, opacity);
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

    private static Mesh CreateDottedRingMesh()
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
            vertices[vertex + 1] = startDirection * 0.92f;
            vertices[vertex + 2] = endDirection * 0.92f;
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
