using UnityEngine;
using UnityEngine.Rendering;

[System.Serializable]
public sealed class QuickGraffitiEntry
{
    public string title;
    public Sprite sprite;
    public bool rotateArtwork;
    public Color sprayColor = Color.white;
    [Min(0.2f)] public float width = 1.8f;
    [Min(0.2f)] public float paintSeconds = 3.5f;

    public Vector2 Size
    {
        get
        {
            if (sprite == null) return Vector2.one * width;
            float aspect = sprite.rect.width / Mathf.Max(1f, sprite.rect.height);
            if (rotateArtwork) aspect = 1f / aspect;
            return new Vector2(width, width / aspect);
        }
    }
}

/// <summary>A wall-bound piece revealed by alternating spray passes; release pauses its progress.</summary>
public sealed class QuickGraffitiPiece : MonoBehaviour
{
    Mesh mesh;
    MeshRenderer meshRenderer;
    MaterialPropertyBlock properties;
    QuickGraffitiEntry entry;
    public float Progress { get; private set; }
    public bool IsComplete => Progress >= 1f;
    public QuickGraffitiEntry Entry => entry;
    public Collider Surface { get; private set; }
    public float SizeMultiplier { get; private set; } = 1f;
    public Vector2 Size => entry.Size * SizeMultiplier;

    public void Initialize(QuickGraffitiEntry artwork, Material material, RaycastHit hit, bool preview = false, float sizeMultiplier = 1f)
    {
        entry = artwork;
        SizeMultiplier = Mathf.Max(0.01f, sizeMultiplier);
        Surface = hit.collider;
        transform.SetPositionAndRotation(hit.point + hit.normal * (preview ? 0.024f : 0.018f),
            Quaternion.LookRotation(hit.normal, SurfaceUp(hit.normal)));
        if (!preview && hit.collider != null) transform.SetParent(hit.collider.transform, true);
        meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = material;
        meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        meshRenderer.sortingOrder = preview ? 11 : 10;
        mesh = new Mesh { name = "Quick graffiti surface quad" };
        UpdateSizeGeometry();
        mesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
        mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        properties = new MaterialPropertyBlock();
        Vector4 uv = UnityEngine.Sprites.DataUtility.GetOuterUV(entry.sprite);
        properties.SetVector("_UVRect", new Vector4(uv.x, uv.y, uv.z - uv.x, uv.w - uv.y));
        properties.SetTexture("_MainTex", entry.sprite.texture);
        properties.SetFloat("_RotateArtwork", entry.rotateArtwork ? 1f : 0f);
        SetPresentation(preview ? 1f : 0f, preview ? 0.32f : 1f, Color.white);
    }

    public void SetPreviewSize(float multiplier)
    {
        multiplier = Mathf.Max(0.01f, multiplier);
        if (Mathf.Approximately(SizeMultiplier, multiplier)) return;
        SizeMultiplier = multiplier;
        UpdateSizeGeometry();
    }

    void UpdateSizeGeometry()
    {
        Vector2 size = Size * 0.5f;
        mesh.vertices = new[] { new Vector3(-size.x, -size.y, 0f), new Vector3(size.x, -size.y, 0f),
            new Vector3(size.x, size.y, 0f), new Vector3(-size.x, size.y, 0f) };
        mesh.RecalculateBounds();
    }

    public static Vector3 SurfaceUp(Vector3 normal)
    {
        Vector3 up = Vector3.ProjectOnPlane(Vector3.up, normal);
        return up.sqrMagnitude > 0.001f ? up.normalized : Vector3.forward;
    }

    public void Advance(float seconds)
    {
        if (seconds <= 0f || IsComplete) return;
        Progress = Mathf.Clamp01(Progress + seconds / Mathf.Max(0.2f, entry.paintSeconds));
        SetPresentation(Progress, 1f, Color.white);
    }

    public void SetPreviewPose(RaycastHit hit, bool valid)
    {
        transform.SetPositionAndRotation(hit.point + hit.normal * 0.024f,
            Quaternion.LookRotation(hit.normal, SurfaceUp(hit.normal)));
        SetPresentation(1f, valid ? 0.32f : 0.18f, valid ? Color.white : new Color(1f, 0.25f, 0.35f));
    }

    public static Vector2 SweepUV(float progress)
    {
        float pass = Mathf.Clamp(progress, 0f, 0.99999f) * 8f;
        int row = Mathf.FloorToInt(pass);
        float x = pass - row;
        if ((row & 1) != 0) x = 1f - x;
        return new Vector2(x, 1f - (row + 0.5f) / 8f);
    }

    public Vector3 SweepWorldPoint()
    {
        Vector2 uv = SweepUV(Progress);
        return transform.TransformPoint(new Vector3((uv.x - 0.5f) * Size.x, (uv.y - 0.5f) * Size.y, 0f));
    }

    void SetPresentation(float progress, float opacity, Color tint)
    {
        properties.SetFloat("_Progress", progress);
        properties.SetFloat("_Opacity", opacity);
        properties.SetColor("_Tint", tint);
        meshRenderer.SetPropertyBlock(properties);
    }

    void OnDestroy()
    {
        if (mesh == null) return;
        if (Application.isPlaying) Destroy(mesh); else DestroyImmediate(mesh);
    }
}
