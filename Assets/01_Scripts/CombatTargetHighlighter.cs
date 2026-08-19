using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public class CombatTargetHighlighter : MonoBehaviour
{
    [Header("Estilo")]
    public Color lockColor = Color.white;
    public float outlineWidth = 0.12f;
    public float outerShadowWidth = 0.032f;
    public bool showLockRings = false;
    public float ringWidth = 0.018f;
    public float fadeSpeed = 8f;
    public float pulseSpeed = 3.2f;
    public float orbitalSpinSpeed = 28f;
    public int ringSegments = 96;

    CombatTarget target;
    readonly List<GameObject> outlineShells = new List<GameObject>();
    readonly List<GameObject> shadowShells = new List<GameObject>();
    readonly List<LineRenderer> rings = new List<LineRenderer>();
    Material outlineMaterial;
    Material shadowMaterial;
    Material ringMaterial;
    Transform ringRoot;
    bool locked;
    bool built;
    float visualAmount;

    void Awake()
    {
        target = GetComponent<CombatTarget>();
    }

    void OnDisable()
    {
        SetVisualsActive(false);
    }

    void LateUpdate()
    {
        if (target == null)
            target = GetComponent<CombatTarget>();

        float goal = locked ? 1f : 0f;
        visualAmount = Mathf.MoveTowards(visualAmount, goal, fadeSpeed * Time.deltaTime);

        bool visible = visualAmount > 0.01f;
        SetVisualsActive(visible);

        if (!visible || target == null)
            return;

        EnsureBuilt();
        UpdateMaterials();
        UpdateRings();
    }

    public void SetLocked(bool value)
    {
        locked = value;

        if (locked)
        {
            EnsureBuilt();
            SetVisualsActive(true);
        }
    }

    void EnsureBuilt()
    {
        if (built)
            return;

        built = true;
        target = target != null ? target : GetComponent<CombatTarget>();
        outlineMaterial = BuildOutlineMaterial();
        shadowMaterial = BuildOutlineMaterial();

        BuildOutlineShells();

        if (showLockRings)
        {
            ringMaterial = BuildRingMaterial();
            BuildRings();
        }

        SetVisualsActive(false);
    }

    Material BuildOutlineMaterial()
    {
        Shader shader = Shader.Find("FLOWSTATE/Combat/WhiteTargetOutline");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        Material material = new Material(shader);
        material.name = "Runtime Combat White Outline";
        return material;
    }

    Material BuildRingMaterial()
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        Material material = new Material(shader);
        material.name = "Runtime Combat Lock Ring";
        return material;
    }

    void BuildOutlineShells()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);

        foreach (Renderer sourceRenderer in renderers)
        {
            if (ShouldIgnoreSourceRenderer(sourceRenderer))
                continue;

            SkinnedMeshRenderer skinned = sourceRenderer as SkinnedMeshRenderer;
            if (skinned != null)
            {
                CreateSkinnedOutlineShell(skinned);
                continue;
            }

            MeshFilter sourceFilter = sourceRenderer.GetComponent<MeshFilter>();
            if (sourceFilter == null || sourceFilter.sharedMesh == null)
                continue;

            CreateMeshOutlineShell(sourceRenderer, sourceFilter.sharedMesh, "Combat Outline Shadow - ", shadowMaterial, shadowShells);
            CreateMeshOutlineShell(sourceRenderer, sourceFilter.sharedMesh, "Combat Outline Shell - ", outlineMaterial, outlineShells);
        }
    }

    void CreateMeshOutlineShell(Renderer sourceRenderer, Mesh mesh, string namePrefix, Material material, List<GameObject> targetList)
    {
        GameObject shell = new GameObject(namePrefix + sourceRenderer.name);
        shell.transform.SetParent(sourceRenderer.transform, false);
        shell.transform.localPosition = Vector3.zero;
        shell.transform.localRotation = Quaternion.identity;
        shell.transform.localScale = Vector3.one;

        MeshFilter shellFilter = shell.AddComponent<MeshFilter>();
        shellFilter.sharedMesh = mesh;

        MeshRenderer shellRenderer = shell.AddComponent<MeshRenderer>();
        ConfigureOutlineRenderer(shellRenderer, material, sourceRenderer, mesh);
        targetList.Add(shell);
    }

    void CreateSkinnedOutlineShell(SkinnedMeshRenderer sourceRenderer)
    {
        if (sourceRenderer.sharedMesh == null)
            return;

        CreateSkinnedOutlineShell(sourceRenderer, "Combat Outline Shadow - ", shadowMaterial, shadowShells);
        CreateSkinnedOutlineShell(sourceRenderer, "Combat Outline Shell - ", outlineMaterial, outlineShells);
    }

    void CreateSkinnedOutlineShell(SkinnedMeshRenderer sourceRenderer, string namePrefix, Material material, List<GameObject> targetList)
    {
        GameObject shell = new GameObject(namePrefix + sourceRenderer.name);
        shell.transform.SetParent(sourceRenderer.transform, false);
        shell.transform.localPosition = Vector3.zero;
        shell.transform.localRotation = Quaternion.identity;
        shell.transform.localScale = Vector3.one;

        SkinnedMeshRenderer shellRenderer = shell.AddComponent<SkinnedMeshRenderer>();
        shellRenderer.sharedMesh = sourceRenderer.sharedMesh;
        shellRenderer.bones = sourceRenderer.bones;
        shellRenderer.rootBone = sourceRenderer.rootBone;
        shellRenderer.localBounds = sourceRenderer.localBounds;
        shellRenderer.updateWhenOffscreen = true;
        ConfigureOutlineRenderer(shellRenderer, material, sourceRenderer, sourceRenderer.sharedMesh);
        targetList.Add(shell);
    }

    void ConfigureOutlineRenderer(Renderer renderer, Material material, Renderer sourceRenderer, Mesh mesh)
    {
        int sourceMaterialCount = sourceRenderer != null ? sourceRenderer.sharedMaterials.Length : 0;
        int subMeshCount = mesh != null ? mesh.subMeshCount : 0;
        int materialCount = Mathf.Max(1, sourceMaterialCount, subMeshCount);
        Material[] materials = new Material[materialCount];

        for (int i = 0; i < materials.Length; i++)
            materials[i] = material;

        renderer.sharedMaterials = materials;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.allowOcclusionWhenDynamic = false;
    }

    void BuildRings()
    {
        GameObject root = new GameObject("Combat Lock Ring Root");
        root.transform.SetParent(transform, false);
        ringRoot = root.transform;

        rings.Add(CreateRing("Combat Lock Ring - Ground"));
        rings.Add(CreateRing("Combat Lock Ring - Body"));
        rings.Add(CreateRing("Combat Lock Ring - Crown"));
    }

    LineRenderer CreateRing(string ringName)
    {
        GameObject ring = new GameObject(ringName);
        ring.transform.SetParent(ringRoot, false);

        LineRenderer line = ring.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.loop = true;
        line.positionCount = Mathf.Max(12, ringSegments);
        line.material = ringMaterial;
        line.widthMultiplier = ringWidth;
        line.numCornerVertices = 6;
        line.numCapVertices = 6;
        line.alignment = LineAlignment.View;
        line.textureMode = LineTextureMode.Stretch;
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;
        return line;
    }

    void UpdateMaterials()
    {
        float pulse = 0.5f + Mathf.Sin(Time.time * pulseSpeed) * 0.5f;
        Color color = lockColor;
        color.a = Mathf.Lerp(0.42f, 0.95f, pulse) * visualAmount;

        if (outlineMaterial != null)
            ApplyOutlineMaterial(outlineMaterial, color, outlineWidth * Mathf.Lerp(0.92f, 1.08f, pulse));

        if (shadowMaterial != null)
        {
            Color shadowColor = new Color(0f, 0f, 0f, 0.55f * visualAmount);
            ApplyOutlineMaterial(shadowMaterial, shadowColor, (outlineWidth + outerShadowWidth) * Mathf.Lerp(0.96f, 1.04f, pulse));
        }

        if (!showLockRings)
            return;

        foreach (LineRenderer ring in rings)
        {
            if (ring == null)
                continue;

            Color ringColor = color;
            ringColor.a *= 0.9f;
            ring.startColor = ringColor;
            ring.endColor = ringColor;
            ring.widthMultiplier = ringWidth * Mathf.Lerp(0.8f, 1.25f, pulse);
        }
    }

    void UpdateRings()
    {
        if (!showLockRings)
            return;

        Bounds bounds;
        if (!target.TryGetWorldBounds(out bounds))
            return;

        Vector3 center = bounds.center;
        float radius = Mathf.Max(0.55f, Mathf.Max(bounds.extents.x, bounds.extents.z) * 1.28f);
        float height = Mathf.Max(0.9f, bounds.size.y);
        float timeSpin = Time.time * orbitalSpinSpeed;

        if (rings.Count > 0)
            SetHorizontalRing(rings[0], new Vector3(center.x, bounds.min.y + 0.045f, center.z), radius, radius * 0.72f, timeSpin);

        if (rings.Count > 1)
        {
            Camera cam = Camera.main;
            Vector3 right = cam != null ? cam.transform.right : transform.right;
            Vector3 up = Vector3.up;
            SetBillboardEllipse(rings[1], center + Vector3.up * (height * 0.04f), right, up, radius * 0.82f, height * 0.58f, -timeSpin * 0.35f);
        }

        if (rings.Count > 2)
            SetHorizontalRing(rings[2], new Vector3(center.x, bounds.max.y + 0.08f, center.z), radius * 0.62f, radius * 0.34f, -timeSpin * 1.4f);
    }

    void SetHorizontalRing(LineRenderer ring, Vector3 center, float radiusX, float radiusZ, float degreesOffset)
    {
        if (ring == null)
            return;

        int count = ring.positionCount;
        float angleOffset = degreesOffset * Mathf.Deg2Rad;

        for (int i = 0; i < count; i++)
        {
            float t = (i / (float)count) * Mathf.PI * 2f + angleOffset;
            Vector3 point = center + new Vector3(Mathf.Cos(t) * radiusX, 0f, Mathf.Sin(t) * radiusZ);
            ring.SetPosition(i, point);
        }
    }

    void SetBillboardEllipse(LineRenderer ring, Vector3 center, Vector3 right, Vector3 up, float radiusX, float radiusY, float degreesOffset)
    {
        if (ring == null)
            return;

        int count = ring.positionCount;
        float angleOffset = degreesOffset * Mathf.Deg2Rad;

        for (int i = 0; i < count; i++)
        {
            float t = (i / (float)count) * Mathf.PI * 2f + angleOffset;
            Vector3 point = center + right.normalized * (Mathf.Cos(t) * radiusX) + up.normalized * (Mathf.Sin(t) * radiusY);
            ring.SetPosition(i, point);
        }
    }

    void SetVisualsActive(bool visible)
    {
        foreach (GameObject shell in shadowShells)
        {
            if (shell != null && shell.activeSelf != visible)
                shell.SetActive(visible);
        }

        foreach (GameObject shell in outlineShells)
        {
            if (shell != null && shell.activeSelf != visible)
                shell.SetActive(visible);
        }

        if (ringRoot != null && ringRoot.gameObject.activeSelf != visible)
            ringRoot.gameObject.SetActive(visible);
    }

    void ApplyOutlineMaterial(Material material, Color color, float width)
    {
        if (material.HasProperty("_OutlineColor"))
            material.SetColor("_OutlineColor", color);
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
        if (material.HasProperty("_OutlineWidth"))
            material.SetFloat("_OutlineWidth", GetObjectSpaceOutlineWidth(width));
    }

    float GetObjectSpaceOutlineWidth(float worldWidth)
    {
        float maxScale = 1f;

        foreach (GameObject shell in outlineShells)
        {
            if (shell == null)
                continue;

            Vector3 scale = shell.transform.lossyScale;
            maxScale = Mathf.Max(maxScale, Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        }

        return worldWidth / Mathf.Max(0.0001f, maxScale);
    }

    bool ShouldIgnoreSourceRenderer(Renderer sourceRenderer)
    {
        if (sourceRenderer == null || sourceRenderer is LineRenderer)
            return true;

        string objectName = sourceRenderer.gameObject.name;
        if (objectName.Contains("Combat Outline Shell") || objectName.Contains("Combat Outline Shadow") || objectName.Contains("Combat Lock Ring"))
            return true;

        Material material = sourceRenderer.sharedMaterial;
        return material != null && material.shader != null && material.shader.name.Contains("WhiteTargetOutline");
    }
}
